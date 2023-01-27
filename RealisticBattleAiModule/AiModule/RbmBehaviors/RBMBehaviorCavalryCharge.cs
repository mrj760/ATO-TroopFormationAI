using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
    public class RBMBehaviorCavalryCharge : BehaviorComponent
    {
        private Vec2 _bracePosition = Vec2.Invalid;

        private ChargeState _chargeState;

        private Timer _chargeTimer;

        private Timer _chargingPastTimer;

        private float _desiredChargeStopDistance;

        private Vec2 _initialChargeDirection;

        private WorldPosition _lastReformDestination;

        private FormationQuerySystem _lastTarget;

        private Timer _reformTimer;

        public bool ChargeArchers = true;
        public bool ChargeCavalry = false;
        public bool ChargeHorseArchers = false;
        public bool ChargeInfantry = true;
        public bool isFirstCharge = true;

        public bool newTarget;

        public RBMBehaviorCavalryCharge(Formation formation)
            : base(formation)
        {
            _lastTarget = null;
            CurrentOrder = MovementOrder.MovementOrderCharge;
            CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            _chargeState = ChargeState.Charging;
            BehaviorCoherence = 0.5f;
            _desiredChargeStopDistance = 110f;
        }

        public override float NavmeshlessTargetPositionPenalty => 1f;

        public override void TickOccasionally()
        {
            base.TickOccasionally();
            if (!Equals(Formation.AI.ActiveBehavior, this)) return;
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }

        private ChargeState CheckAndChangeState()
        {
            var _currentTacticField = typeof(TeamAIComponent)
                .GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
            var _ = _currentTacticField?.DeclaringType?.GetField("_currentTactic");

            if (_currentTacticField != null && _currentTacticField.GetValue(Formation?.Team?.TeamAI).ToString().Contains("Embolon"))
            {
                _desiredChargeStopDistance = 50f;
                BehaviorCoherence = 0.85f;
            }

            var result = _chargeState;

            var fqs = Formation?.QuerySystem;

            if (fqs?.ClosestEnemyFormation == null)
            {
                return ChargeState.Undetermined;
            }

            var avgpos = fqs.AveragePosition;

            switch (_chargeState)
            {
                case ChargeState.Undetermined:
                    if (fqs.ClosestEnemyFormation != null 
                        && ((!fqs.IsCavalryFormation && !fqs.IsRangedCavalryFormation) 
                            || avgpos.Distance(fqs.ClosestEnemyFormation.MedianPosition.AsVec2) / fqs.MovementSpeedMaximum <= 5f))
                    {
                        result = ChargeState.Charging;
                    }

                    break;

                case ChargeState.Charging:

                    if (_lastTarget == null || _lastTarget.Formation.CountOfUnits == 0)
                    {
                        Formation correctEnemy;

                        if (ChargeInfantry)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, true, false, false, false, false);
                        else if (ChargeArchers)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, false, true, false, false, false);
                        else if (ChargeCavalry)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, false, false, true, true, true);
                        else
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, true, true, false, false, false);

                        _lastTarget =
                            correctEnemy != null
                                ? correctEnemy.QuerySystem
                                : fqs.ClosestEnemyFormation;

                        newTarget = true;
                        _initialChargeDirection = _lastTarget.MedianPosition.AsVec2 - avgpos;

                    }
                    else if (_initialChargeDirection.DotProduct(_lastTarget.MedianPosition.AsVec2 - avgpos) <= 0f)
                    {
                        if (_chargeTimer == null)
                        {
                            _chargeTimer = new Timer(Mission.Current.CurrentTime, 3f);
                        }
                    }

                    if (fqs.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents < 5f
                        || (_chargeTimer != null && _chargeTimer.Check(Mission.Current.CurrentTime)))
                    {
                        result = ChargeState.ChargingPast;
                        _chargeTimer = null;
                    }

                    break;

                case ChargeState.ChargingPast:
                    if (avgpos.Distance(_lastTarget.MedianPosition.AsVec2)
                        >= _desiredChargeStopDistance + _lastTarget.Formation.Depth)
                    {
                        if (_chargingPastTimer.Check(Mission.Current.CurrentTime))
                        {
                            _lastReformDestination = fqs.MedianPosition;
                        }

                        return ChargeState.Reforming;
                    }

                    break;

                case ChargeState.Reforming:
                    if (_reformTimer.Check(Mission.Current.CurrentTime)
                        || fqs.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents < 12f
                        || fqs.UnderRangedAttackRatio > 0.2f)
                    {
                        CheckForNewChargeTarget();
                        if (_lastTarget?.Formation != null)
                        {
                            Formation.FormOrder = FormOrder.FormOrderCustom(_lastTarget.Formation.Width);
                        }

                        return ChargeState.Charging;
                    }

                    break;

                case ChargeState.Bracing:
                    _bracePosition = Vec2.Invalid;
                    _chargeState = ChargeState.Charging;

                    break;
            }

            return result;
        }

        public void CheckForNewChargeTarget()
        {
            var correctEnemy = Utilities.FindSignificantEnemy(Formation, true, true, false, false, false);
            _lastTarget =
                correctEnemy != null ?
                    correctEnemy.QuerySystem :
                    Formation.QuerySystem.ClosestEnemyFormation;
            newTarget = true;
            _initialChargeDirection = _lastTarget.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition;
        }

        protected override void CalculateCurrentOrder()
        {
            if (Formation.QuerySystem.ClosestEnemyFormation == null)
            {
                CurrentOrder = MovementOrder.MovementOrderCharge;
                return;
            }

            var chargeState = CheckAndChangeState();

            var fqs = Formation.QuerySystem;

            if (isFirstCharge)
            {
                var vec4 = (_lastTarget.MedianPosition.AsVec2 - fqs.AveragePosition).Normalized();
                var medianPosition3 = _lastTarget.MedianPosition;
                var vec5 = medianPosition3.AsVec2 + vec4 * (_desiredChargeStopDistance + _lastTarget.Formation.Depth);
                medianPosition3.SetVec2(vec5);
                _lastReformDestination = medianPosition3;
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition3);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec4);
                isFirstCharge = false;
            }

            if (chargeState == _chargeState && !newTarget) 
                return;

            _chargeState = chargeState;

            switch (_chargeState)
            {
                case ChargeState.Undetermined:
                    CurrentOrder = MovementOrder.MovementOrderCharge;
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;

                case ChargeState.Charging:
                    CheckForNewChargeTarget();
                    var vec4 = (_lastTarget.MedianPosition.AsVec2 - fqs.AveragePosition).Normalized();
                    var medianPosition3 = _lastTarget.MedianPosition;
                    var vec5 = medianPosition3.AsVec2 +
                               vec4 * (_desiredChargeStopDistance + _lastTarget.Formation.Depth);
                    medianPosition3.SetVec2(vec5);
                    _lastReformDestination = medianPosition3;
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition3);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec4);
                    break;

                case ChargeState.ChargingPast:
                    _chargingPastTimer = new Timer(Mission.Current.CurrentTime, 19f);
                    CurrentOrder = MovementOrder.MovementOrderMove(_lastReformDestination);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;

                case ChargeState.Reforming:
                    _reformTimer = new Timer(Mission.Current.CurrentTime, 10f);
                    CurrentOrder = MovementOrder.MovementOrderMove(_lastReformDestination);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;

                case ChargeState.Bracing:
                    var medianPosition = fqs.MedianPosition;
                    medianPosition.SetVec2(_bracePosition);
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                    break;
            }

            newTarget = false;
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWide;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        public override TextObject GetBehaviorString()
        {
            var behaviorString = base.GetBehaviorString();

            var fqs = Formation.QuerySystem;

            if (fqs.ClosestEnemyFormation == null) 
                return behaviorString;

            behaviorString.SetTextVariable("AI_SIDE",
                GameTexts.FindText("str_formation_ai_side_strings",
                    fqs.ClosestEnemyFormation.Formation.AI.Side.ToString()));
            behaviorString.SetTextVariable("CLASS",
                GameTexts.FindText("str_formation_class_string",
                    fqs.ClosestEnemyFormation.Formation.PrimaryClass.GetName()));

            return behaviorString;
        }

        protected override float GetAiWeight()
        {
            var fqs = Formation.QuerySystem;
            if (fqs.ClosestEnemyFormation == null) return 0f;
            var num = fqs.AveragePosition.Distance(fqs.ClosestEnemyFormation.MedianPosition.AsVec2) /
                      fqs.MovementSpeedMaximum;
            float num3;
            if (!fqs.IsCavalryFormation && !fqs.IsRangedCavalryFormation)
            {
                var num2 = MBMath.ClampFloat(num, 4f, 10f);
                num3 = MBMath.Lerp(0.8f, 1f, 1f - (num2 - 4f) / 6f);
            }
            else if (num <= 4f)
            {
                var num4 = MBMath.ClampFloat(num, 0f, 4f);
                num3 = MBMath.Lerp(0.8f, 1.2f, num4 / 4f);
            }
            else
            {
                var num5 = MBMath.ClampFloat(num, 4f, 10f);
                num3 = MBMath.Lerp(0.8f, 1.2f, 1f - (num5 - 4f) / 6f);
            }

            var medianPosition = fqs.MedianPosition;
            medianPosition.SetVec2(fqs.AveragePosition);
            var num6 = medianPosition.GetNavMeshZ() - fqs.ClosestEnemyFormation.MedianPosition.GetNavMeshZ();
            var num7 = 1f;
            if (num <= 4f)
            {
                var value = num6 / (fqs.AveragePosition - fqs.ClosestEnemyFormation.MedianPosition.AsVec2)
                    .Length;
                num7 = MBMath.Lerp(0.9f, 1.1f, (MBMath.ClampFloat(value, -0.58f, 0.58f) + 0.58f) / 1.16f);
            }

            var num8 = 1f;
            if (num <= 4f && num >= 1.5f) num8 = 1.2f;
            var num9 = 1f;
            if (num <= 4f && fqs.ClosestEnemyFormation.ClosestEnemyFormation != fqs) num9 = 1.2f;
            var num10 = fqs.GetClassWeightedFactor(1f, 1f, 1.5f, 1.5f) *
                        fqs.ClosestEnemyFormation.GetClassWeightedFactor(1f, 1f, 0.5f, 0.5f);
            return num3 * num7 * num8 * num9 * num10 * 2f;
        }

        private enum ChargeState
        {
            Undetermined,
            Charging,
            ChargingPast,
            Reforming,
            Bracing
        }
    }
}