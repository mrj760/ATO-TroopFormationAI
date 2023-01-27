using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.FormationAI;

namespace RBMAI.AiModule.RbmBehaviors
{
    internal class RBMBehaviorForwardSkirmish : BehaviorComponent
    {
        private Timer _attackTimer;

        private bool _isEnemyReachable = true;
        private Timer _reformTimer;

        private Timer _returnTimer;
        private SkirmishMode _skirmishMode;

        public BehaviorSide FlankSide = BehaviorSide.Middle;

        private float mobilityModifier = 1f;

        public RBMBehaviorForwardSkirmish(Formation formation)
            : base(formation)
        {
            _skirmishMode = SkirmishMode.Reform;
            _behaviorSide = formation.AI.Side;
            CalculateCurrentOrder();
            BehaviorCoherence = 0.5f;
        }

        protected override float GetAiWeight()
        {
            var fqs = Formation.QuerySystem;

            if (!_isEnemyReachable)
                return 0f;

            if (Formation != null && fqs.IsCavalryFormation && Utilities.CheckIfMountedSkirmishFormation(Formation, 0.6f))
                return 5f;

            if (Formation == null || !fqs.IsInfantryFormation)
                return 0f;

            var countOfSkirmishers = 0f;
            Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
            {
                if (Utilities.CheckIfSkirmisherAgent(agent, 1)) countOfSkirmishers++;
            });

            return (countOfSkirmishers / Formation.CountOfUnits) > 0.6f ? 5f : 0f;
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        protected sealed override void CalculateCurrentOrder()
        {
            var fqs = Formation.QuerySystem;

            mobilityModifier = fqs.IsInfantryFormation ? 1.25f : 1f;

            var position = fqs.MedianPosition;
            _isEnemyReachable = fqs.ClosestSignificantlyLargeEnemyFormation != null
                                && (!(Formation.Team.TeamAI is TeamAISiegeComponent)
                                    || !TeamAISiegeComponent.IsFormationInsideCastle(
                                        fqs.ClosestSignificantlyLargeEnemyFormation.Formation, false));

            var averagePosition = fqs.AveragePosition;

            if (!_isEnemyReachable)
            {
                position.SetVec2(averagePosition);
            }
            else
            {
                var skirmishRange = 45f / mobilityModifier;
                const float flankRange = 25f;

                var enemyFormation = fqs.ClosestSignificantlyLargeEnemyFormation?.Formation;
                var allyFormation = Utilities.FindSignificantAlly(Formation, true, true, false, false, false);

                if (Formation != null && fqs.IsInfantryFormation)
                    enemyFormation = Utilities.FindSignificantEnemyToPosition(Formation, position, true, true, false, false, false, false);

                var averageAllyFormationPosition = fqs.Team.AveragePosition;
                var medianTargetFormationPosition = fqs.Team.MedianTargetFormationPosition;
                var enemyDirection = (medianTargetFormationPosition.AsVec2 - averageAllyFormationPosition).Normalized();

                Vec2 calcPosition;

                switch (_skirmishMode)
                {
                    case SkirmishMode.Reform:

                        _returnTimer = null;

                        if (enemyFormation == null)
                        {
                            position = WorldPosition.Invalid;
                            break;
                        }

                        if (averagePosition.Distance(enemyFormation.QuerySystem.AveragePosition) > skirmishRange)
                        {
                            if (_reformTimer == null)
                            {
                                _reformTimer = new Timer(Mission.Current.CurrentTime, 4f / mobilityModifier);
                            }
                        }

                        if (_reformTimer != null && _reformTimer.Check(Mission.Current.CurrentTime))
                        {
                            _skirmishMode = SkirmishMode.Attack;
                        }

                        if (allyFormation == null)
                        {
                            position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                             enemyDirection.Normalized() * 150f);
                            break;
                        }

                        if (_behaviorSide == BehaviorSide.Right || FlankSide == BehaviorSide.Right)
                        {
                            calcPosition = allyFormation.CurrentPosition
                                           + enemyDirection.RightVec().Normalized()
                                           * (allyFormation.Width + Formation.Width + flankRange);
                            position.SetVec2(calcPosition);
                        }
                        else if (_behaviorSide == BehaviorSide.Left || FlankSide == BehaviorSide.Left)
                        {
                            calcPosition = allyFormation.CurrentPosition
                                           + enemyDirection.LeftVec().Normalized()
                                           * (allyFormation.Width + Formation.Width + flankRange);
                            position.SetVec2(calcPosition);
                        }
                        else
                        {
                            position = allyFormation.QuerySystem.MedianPosition;
                        }

                        break;

                    case SkirmishMode.Returning:
                        _attackTimer = null;

                        if (_returnTimer == null)
                        {
                            _returnTimer = new Timer(Mission.Current.CurrentTime, 10f / mobilityModifier);
                        }

                        if (_returnTimer != null && _returnTimer.Check(Mission.Current.CurrentTime))
                        {
                            _skirmishMode = SkirmishMode.Reform;
                        }

                        if (allyFormation == null)
                        {
                            position.SetVec2(medianTargetFormationPosition.AsVec2 + enemyDirection.Normalized() * 150f);
                            break;
                        }



                        if (_behaviorSide == BehaviorSide.Right || FlankSide == BehaviorSide.Right)
                        {
                            calcPosition = allyFormation.CurrentPosition
                                           + enemyDirection.RightVec().Normalized()
                                           * (allyFormation.Width + Formation.Width + flankRange);
                            position.SetVec2(calcPosition);
                        }
                        else if (_behaviorSide == BehaviorSide.Left || FlankSide == BehaviorSide.Left)
                        {
                            calcPosition = allyFormation.CurrentPosition 
                                           + enemyDirection.LeftVec().Normalized() 
                                           * (allyFormation.Width + Formation.Width + flankRange);
                            position.SetVec2(calcPosition);
                        }
                        else
                        {
                            position = allyFormation.QuerySystem.MedianPosition;
                        }

                        break;

                    case SkirmishMode.Attack:

                        if (enemyFormation == null)
                        {
                            position = WorldPosition.Invalid;
                            break;
                        }

                        _reformTimer = null;

                        if ((averagePosition.Distance(enemyFormation.QuerySystem.AveragePosition) < skirmishRange
                             || fqs.MakingRangedAttackRatio > 0.1f) && _attackTimer == null)
                        {
                            _attackTimer = new Timer(Mission.Current.CurrentTime, 3f * mobilityModifier);
                        }

                        if (_attackTimer != null && _attackTimer.Check(Mission.Current.CurrentTime))
                        {
                            _skirmishMode = SkirmishMode.Returning;
                        }

                        position = medianTargetFormationPosition;
                        calcPosition = position.AsVec2 - enemyDirection * (skirmishRange - (10f + Formation.Depth * 0.5f));
                        var dir = new Vec2(0,0);

                        if (_behaviorSide == BehaviorSide.Right || FlankSide == BehaviorSide.Right)
                        {
                            dir = enemyFormation.Direction.LeftVec().Normalized();
                        }
                        else if (_behaviorSide == BehaviorSide.Left || FlankSide == BehaviorSide.Left)
                        {
                            dir = enemyFormation.Direction.RightVec().Normalized();
                        }

                        calcPosition += dir * (enemyFormation.Width / 2f) * mobilityModifier;
                        position.SetVec2(calcPosition);

                        break;
                }
            }

            CurrentOrder = MovementOrder.MovementOrderMove(position);
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
        }

        public override TextObject GetBehaviorString()
        {
            var name = GetType().Name;
            return GameTexts.FindText("str_formation_ai_sergeant_instruction_behavior_text", name);
        }

        private enum SkirmishMode
        {
            Reform,
            Returning,
            Attack
        }
    }
}