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

        protected sealed override void CalculateCurrentOrder()
        {
            var fqs = Formation.QuerySystem;
            var medianPosition = fqs.MedianPosition;
            var flag = false;
            if (Formation.CountOfUnits <= 1)
            {
                medianPosition.SetVec2(fqs.AveragePosition);
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                return;
            }

            if (fqs.ClosestSignificantlyLargeEnemyFormation == null)
            {
                medianPosition.SetVec2(fqs.AveragePosition);
            }
            else
            {
                Formation significantEnemy = null;

                if (Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    significantEnemy = Utilities.FindSignificantEnemy(Formation, true, false, false, false, false);
                }

                if (significantEnemy == null)
                {
                    significantEnemy = Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation?.Formation;
                }

                var significantAlly = Utilities.FindSignificantAlly(Formation, true, false, false, false, false, true);

                if (significantEnemy == null)
                {
                    return;
                }

                var vec = significantEnemy.SmoothedAverageUnitPosition - Formation.SmoothedAverageUnitPosition;
                var distance = vec.Normalize();

                var isFormationShooting = Utilities.IsFormationShooting(Formation);
                var effectiveShootingRange = Formation.Depth * .5f + fqs.MissileRange * 0.5883f/* .5883~=~(1/1.7) */;

                var _currentTacticField = typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
                if (_currentTacticField != null)
                {
                    var _ = _currentTacticField.DeclaringType?.GetField("_currentTactic");

                    if ((Formation?.Team?.TeamAI != null)
                        && (_currentTacticField.GetValue(Formation?.Team?.TeamAI) != null
                            && _currentTacticField.GetValue(Formation?.Team?.TeamAI).ToString().Contains("SplitArchers"))
                        && (Formation?.Team?.Formations.Count(f => f.QuerySystem.IsRangedFormation) > 1))
                    {
                        effectiveShootingRange += significantEnemy.Width / 3.5f;
                    }
                }

                effectiveShootingRange += significantEnemy.Depth / 2f;

                if (significantAlly != null && (significantAlly == Formation || !significantAlly.QuerySystem.IsInfantryFormation))
                {
                    effectiveShootingRange *= 1.9f;
                }

                var previousBehavior = _behaviorState;



                switch (_behaviorState)
                {
                    case BehaviorState.Shooting:
                        {
                            if (distance > effectiveShootingRange * 1.1f)
                            {
                                _behaviorState = BehaviorState.Approaching;
                                break;
                            }

                            if (isFormationShooting)
                            {
                                if (distance > effectiveShootingRange)
                                {
                                    _behaviorState = BehaviorState.Approaching;
                                }

                                else if (Formation != null && fqs.IsRangedFormation && distance < effectiveShootingRange * 0.5f)
                                {
                                    var meleeFormation = Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                                    if (meleeFormation != null && meleeFormation.QuerySystem.IsInfantryFormation)
                                    {
                                        _behaviorState = BehaviorState.PullingBack;
                                    }
                                }

                                break;
                            }

                            if (Formation == null || !fqs.IsRangedFormation || distance >= effectiveShootingRange * 0.4f)
                            {
                                if (refreshPositionTimer == null)
                                {
                                    refreshPositionTimer = new Timer(Mission.Current.CurrentTime, 30f);
                                    _behaviorState = BehaviorState.Approaching;
                                }
                                else if (refreshPositionTimer.Check(Mission.Current.CurrentTime))
                                {
                                    refreshPositionTimer = null;
                                }
                            }
                            else
                            {
                                var meleeFormation = Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                                if (meleeFormation == null) break;
                                var mfqs = meleeFormation.QuerySystem;

                                if (mfqs.IsInfantryFormation
                                    && mfqs.MedianPosition.AsVec2.Distance(fqs.MedianPosition.AsVec2) <= fqs.MissileRange)
                                {
                                    _behaviorState = BehaviorState.PullingBack;
                                }
                            }

                            break;
                        }

                    case BehaviorState.Approaching:
                        {
                            if (distance >= effectiveShootingRange * 0.9f)
                            {
                                break;
                            }

                            if (distance < effectiveShootingRange * 0.4f)
                            {
                                _behaviorState = BehaviorState.PullingBack;
                                flag = true;
                            }
                            else if (!wasShootingBefore)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                                wasShootingBefore = true;
                            }
                            else if (Utilities.IsFormationShooting(Formation, 0.2f))
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }
                            else
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }


                            break;
                        }

                    case BehaviorState.PullingBack:
                        {
                            var meleeFormationPull = Utilities.FindSignificantAlly(Formation, true, false, false, false, false);
                            if (meleeFormationPull == null)
                                break;
                            var mfpqs = meleeFormationPull.QuerySystem;

                            if (
                                (distance > effectiveShootingRange * 0.9f)
                                || (isFormationShooting && distance > effectiveShootingRange * 0.5f)
                                || (!mfpqs.IsInfantryFormation)
                                || (Formation != null && mfpqs.MedianPosition.AsVec2.Distance(fqs.MedianPosition.AsVec2) > fqs.MissileRange)
                            )
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }

                            break;
                        }
                }

                if (Utilities.CheckIfOnlyCavRemaining(Formation))
                {
                    _behaviorState = BehaviorState.Shooting;
                }

                var shouldReposition = false;

                if (_behaviorState == BehaviorState.PullingBack)
                {
                    var curtime = Mission.Current.CurrentTime;
                    if (repositionTimer == null)
                    {
                        repositionTimer = new Timer(curtime, 5f);
                    }
                    else if (repositionTimer.Check(curtime))
                    {
                        shouldReposition = true;
                        repositionTimer = null;
                    }
                }

                if (firstTime || shouldReposition || previousBehavior != _behaviorState)
                    return;

                firstTime = false;

                Vec2 dirvecnormed;

                var seqs = significantEnemy.QuerySystem;

                switch (_behaviorState)
                {
                    case BehaviorState.Shooting:
                        medianPosition.SetVec2(fqs.AveragePosition);
                        break;

                    case BehaviorState.Approaching:

                        dirvecnormed =
                            side == 0
                                ? seqs.MedianPosition.AsVec2.LeftVec().Normalized()
                                : seqs.MedianPosition.AsVec2.RightVec().Normalized();

                        medianPosition.SetVec2(seqs.AveragePosition + dirvecnormed * MBRandom.RandomFloat * 70f);

                        break;

                    case BehaviorState.PullingBack:

                        medianPosition = seqs.MedianPosition;

                        dirvecnormed =
                            side == 0
                                ? seqs.MedianPosition.AsVec2.LeftVec().Normalized()
                                : seqs.MedianPosition.AsVec2.RightVec().Normalized();

                        medianPosition.SetVec2(medianPosition.AsVec2 - vec * (effectiveShootingRange - Formation.Depth * 0.5f) + dirvecnormed * MBRandom.RandomFloat * 70f);

                        break;
                }

                if (flag || _behaviorState != BehaviorState.Shooting || !CurrentOrder.GetPosition(Formation).IsValid)
                {
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                }

                if (!CurrentFacingOrder.GetDirection(Formation).IsValid)
                    return;

                var averageAllyFormationPosition = fqs.Team.AveragePosition;
                var medianTargetFormationPosition = fqs.Team.MedianTargetFormationPosition.AsVec2;
                //CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection((medianTargetFormationPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized());
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection((medianTargetFormationPosition - averageAllyFormationPosition).Normalized());
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