using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
    internal class RBMBehaviorArcherSkirmish : BehaviorComponent
    {
        private BehaviorState _behaviorState = BehaviorState.PullingBack;

        private readonly Timer _cantShootTimer;
        public int cooldown = 0;

        private bool firstTime = true;

        //private int flankCooldownMax = 40;
        public Timer flankingTimer = null;
        public bool nudgeFormation;

        public Timer refreshPositionTimer;

        //public float customWidth = 110f;
        public Timer repositionTimer;
        public int side = MBRandom.RandomInt(2);

        public bool wasShootingBefore;

        public RBMBehaviorArcherSkirmish(Formation formation)
            : base(formation)
        {
            BehaviorCoherence = 0.5f;
            _cantShootTimer = new Timer(0f, 0f);
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            var medianPosition = Formation.QuerySystem.MedianPosition;
            var flag = false;
            Vec2 vec;
            Vec2 vec2;
            if (Formation.CountOfUnits <= 1)
            {
/*
                vec = Formation.Direction;
*/
                medianPosition.SetVec2(Formation.QuerySystem.AveragePosition);
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                return;
            }

            if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
            {
/*
                vec = Formation.Direction;
*/
                medianPosition.SetVec2(Formation.QuerySystem.AveragePosition);
            }
            else
            {
                Formation significantEnemy = null;
                if (Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                    significantEnemy = Utilities.FindSignificantEnemy(Formation, true, false, false, false, false);
                if (significantEnemy == null)
                    significantEnemy = Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                Formation significantAlly = null;
                significantAlly = Utilities.FindSignificantAlly(Formation, true, false, false, false, false, true);

                //vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 - base.Formation.QuerySystem.MedianPosition.AsVec2;
                if (significantEnemy == null)
                    return;

                vec = significantEnemy.SmoothedAverageUnitPosition - Formation.SmoothedAverageUnitPosition;
                var distance = vec.Normalize();

                var isFormationShooting = Utilities.IsFormationShooting(Formation);
                var effectiveShootingRange = Formation.Depth / 2f + Formation.QuerySystem.MissileRange / 1.7f;
                var _currentTacticField =
                    typeof(TeamAIComponent).GetField("_currentTactic",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                var _ = _currentTacticField?.DeclaringType?.GetField("_currentTactic");
                if (Formation?.Team?.TeamAI != null)
                    if (_currentTacticField?.GetValue(Formation?.Team?.TeamAI) != null && _currentTacticField
                            .GetValue(Formation?.Team?.TeamAI).ToString().Contains("SplitArchers"))
                        if (Formation?.Team?.Formations.Count(f => f.QuerySystem.IsRangedFormation) > 1)
                            effectiveShootingRange += significantEnemy.Width / 3.5f;

                effectiveShootingRange += significantEnemy.Depth / 2f;

                if (significantAlly != null &&
                    (significantAlly == Formation || !significantAlly.QuerySystem.IsInfantryFormation))
                    effectiveShootingRange *= 1.9f;
                var rollPullBackAngle = 0f;
                var previousBehavior = _behaviorState;
                switch (_behaviorState)
                {
                    case BehaviorState.Shooting:
                        {
                            if (distance > effectiveShootingRange * 1.1f)
                            {
                                _behaviorState = BehaviorState.Approaching;
                                //_cantShootDistance = MathF.Min(_cantShootDistance, effectiveShootingRange);
                                break;
                            }

                            if (isFormationShooting)
                            {
                                if (distance > effectiveShootingRange)
                                {
                                    _behaviorState = BehaviorState.Approaching;
                                    //_cantShootDistance = MathF.Min(_cantShootDistance, effectiveShootingRange);
                                    break;
                                }

                                //_cantShoot = false;
                                if (Formation != null && Formation.QuerySystem.IsRangedFormation && distance < effectiveShootingRange * 0.5f)
                                {
                                    var meleeFormation =
                                        Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                                    if (meleeFormation != null && meleeFormation.QuerySystem.IsInfantryFormation)
                                    {
/*
                                        rollPullBackAngle = MBRandom.RandomFloat;
*/
                                        _behaviorState = BehaviorState.PullingBack;
                                    }
                                }
                            }
                            else
                            {
                                //_cantShootDistance = distance;
                                if (Formation != null && Formation.QuerySystem.IsRangedFormation && distance < effectiveShootingRange * 0.4f)
                                {
                                    var meleeFormation =
                                        Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                                    if (meleeFormation != null && meleeFormation.QuerySystem.IsInfantryFormation &&
                                        meleeFormation.QuerySystem.MedianPosition.AsVec2.Distance(Formation.QuerySystem
                                            .MedianPosition.AsVec2) <= Formation.QuerySystem.MissileRange)
                                    {
/*
                                        rollPullBackAngle = MBRandom.RandomFloat;
*/
                                        _behaviorState = BehaviorState.PullingBack;
                                    }
                                }
                                else
                                {
                                    if (refreshPositionTimer == null)
                                    {
                                        refreshPositionTimer = new Timer(Mission.Current.CurrentTime, 30f);
                                        _behaviorState = BehaviorState.Approaching;
                                    }
                                    else
                                    {
                                        if (refreshPositionTimer.Check(Mission.Current.CurrentTime))
                                            refreshPositionTimer = null;
                                    }
                                }
                            }

                            break;
                        }
                    case BehaviorState.Approaching:
                        {
                            if (distance < effectiveShootingRange * 0.4f)
                            {
/*
                                rollPullBackAngle = MBRandom.RandomFloat;
*/
                                _behaviorState = BehaviorState.PullingBack;
                                flag = true;
                            }
                            else if (distance < effectiveShootingRange * 0.9f)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }
                            else if (Utilities.IsFormationShooting(Formation, 0.2f) &&
                                     distance < effectiveShootingRange * 0.9f)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }
                            else if (distance < effectiveShootingRange * 0.9f && !wasShootingBefore)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                                wasShootingBefore = true;
                            }

                            break;
                        }
                    case BehaviorState.PullingBack:
                        {
                            var meleeFormationPull =
                                Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                            if (meleeFormationPull != null &&
                                meleeFormationPull.QuerySystem.MedianPosition.AsVec2.Distance(Formation.QuerySystem
                                    .MedianPosition.AsVec2) > Formation.QuerySystem.MissileRange)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }

                            if (meleeFormationPull == null || !meleeFormationPull.QuerySystem.IsInfantryFormation)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }

                            if (distance > effectiveShootingRange * 0.9f)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }

                            if (isFormationShooting && distance > effectiveShootingRange * 0.5f)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }

                            break;
                        }
                }

                var isOnlyCavReamining = Utilities.CheckIfOnlyCavRemaining(Formation);
                if (isOnlyCavReamining) _behaviorState = BehaviorState.Shooting;

                var shouldReposition = false;
                if (_behaviorState == BehaviorState.PullingBack)
                {
                    if (repositionTimer == null)
                    {
                        repositionTimer = new Timer(Mission.Current.CurrentTime, 5f);
                    }
                    else
                    {
                        if (repositionTimer.Check(Mission.Current.CurrentTime))
                        {
                            shouldReposition = true;
                            repositionTimer = null;
                        }
                    }
                }

                if (!firstTime && previousBehavior == _behaviorState && !shouldReposition)
                    return;

                Vec2 sigEnemyVec, sigEnemySide;

                switch (_behaviorState)
                {
                    case BehaviorState.Shooting:
                        medianPosition.SetVec2(Formation.QuerySystem.AveragePosition);
                        break;
                    case BehaviorState.Approaching:

                        rollPullBackAngle = MBRandom.RandomFloat;
                        sigEnemyVec = significantEnemy.QuerySystem.MedianPosition.AsVec2;
                        sigEnemySide = side%2 == 0 ? sigEnemyVec.LeftVec() : sigEnemyVec.RightVec();
                        medianPosition.SetVec2(significantEnemy.QuerySystem.AveragePosition + sigEnemySide.Normalized() * rollPullBackAngle * 70f);

                        break;

                    case BehaviorState.PullingBack:
                        medianPosition = significantEnemy.QuerySystem.MedianPosition;
                        rollPullBackAngle = MBRandom.RandomFloat;
                        sigEnemyVec = significantEnemy.QuerySystem.MedianPosition.AsVec2;
                        sigEnemySide = side%2 == 0 ? sigEnemyVec.LeftVec() : sigEnemyVec.RightVec();
                        medianPosition.SetVec2(medianPosition.AsVec2 - vec * (effectiveShootingRange - Formation.Depth * 0.5f) + sigEnemySide.Normalized() * rollPullBackAngle * 70f);
                        break;
                }

                if (!CurrentOrder.GetPosition(Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag) 
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);

                if (!CurrentFacingOrder.GetDirection(Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag)
                {
                    var averageAllyFormationPosition = Formation.QuerySystem.Team.AveragePosition;
                    var medianTargetFormationPosition = Formation.QuerySystem.Team.MedianTargetFormationPosition;
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(
                        (medianTargetFormationPosition.AsVec2 - Formation.QuerySystem.AveragePosition)
                        .Normalized());
                }

                firstTime = false;
            }
        }
        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }

        protected override void OnBehaviorActivatedAux()
        {
            _behaviorState = BehaviorState.PullingBack;
            _cantShootTimer.Reset(Mission.Current.CurrentTime,
                MBMath.Lerp(5f, 10f, (MBMath.ClampFloat(Formation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWide;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        protected override float GetAiWeight()
        {
            var fqs = Formation.QuerySystem;
            return MBMath.Lerp(0.1f, 1f,
                MBMath.ClampFloat(fqs.RangedUnitRatio + fqs.RangedCavalryUnitRatio, 0f, 0.5f) * 2f);
        }

        private enum BehaviorState
        {
            Approaching,
            Shooting,
            PullingBack,
            Flanking
        }
    }
}