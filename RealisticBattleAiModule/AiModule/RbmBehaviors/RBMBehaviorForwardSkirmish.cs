using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI
{
    internal class RBMBehaviorForwardSkirmish : BehaviorComponent
    {
        private Timer _attackTimer;

        private bool _isEnemyReachable = true;
        private Timer _reformTimer;

        private Timer _returnTimer;
        private SkirmishMode _skirmishMode;

        public FormationAI.BehaviorSide FlankSide = FormationAI.BehaviorSide.Middle;

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
            if (!_isEnemyReachable) return 0f;
            if (Formation != null && Formation.QuerySystem.IsCavalryFormation)
                if (Utilities.CheckIfMountedSkirmishFormation(Formation, 0.6f))
                    return 5f;
            if (Formation != null && Formation.QuerySystem.IsInfantryFormation)
            {
                var countOfSkirmishers = 0;
                Formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                {
                    if (Utilities.CheckIfSkirmisherAgent(agent, 1)) countOfSkirmishers++;
                });
                if (countOfSkirmishers / Formation.CountOfUnits > 0.6f)
                    return 5f;
                return 0f;
            }

            return 0f;
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

        protected override void CalculateCurrentOrder()
        {
            if (Formation.QuerySystem.IsInfantryFormation)
                mobilityModifier = 1.25f;
            else
                mobilityModifier = 1f;
            var position = Formation.QuerySystem.MedianPosition;
            _isEnemyReachable = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null &&
                                (!(Formation.Team.TeamAI is TeamAISiegeComponent) ||
                                 !TeamAISiegeComponent.IsFormationInsideCastle(
                                     Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation, false));
            var averagePosition = Formation.QuerySystem.AveragePosition;

            if (!_isEnemyReachable)
            {
                position.SetVec2(averagePosition);
            }
            else
            {
                var skirmishRange = 45f / mobilityModifier;
                var flankRange = 25f;

                var enemyFormation = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                var allyFormation = Utilities.FindSignificantAlly(Formation, true, true, false, false, false);

                if (Formation != null && Formation.QuerySystem.IsInfantryFormation)
                    enemyFormation = Utilities.FindSignificantEnemyToPosition(Formation, position, true, true, false,
                        false, false, false);

                var averageAllyFormationPosition = Formation.QuerySystem.Team.AveragePosition;
                var medianTargetFormationPosition = Formation.QuerySystem.Team.MedianTargetFormationPosition;
                var enemyDirection = (medianTargetFormationPosition.AsVec2 - averageAllyFormationPosition).Normalized();

                switch (_skirmishMode)
                {
                    case SkirmishMode.Reform:
                    {
                        _returnTimer = null;
                        if (enemyFormation != null)
                        {
                            if (averagePosition.Distance(enemyFormation.QuerySystem.AveragePosition) > skirmishRange)
                                if (_reformTimer == null)
                                    _reformTimer = new Timer(Mission.Current.CurrentTime, 4f / mobilityModifier);
                            if (_reformTimer != null && _reformTimer.Check(Mission.Current.CurrentTime))
                                _skirmishMode = SkirmishMode.Attack;

                            if (_behaviorSide == FormationAI.BehaviorSide.Right ||
                                FlankSide == FormationAI.BehaviorSide.Right)
                            {
                                if (allyFormation != null)
                                {
                                    var calcPosition = allyFormation.CurrentPosition +
                                                       enemyDirection.RightVec().Normalized() *
                                                       (allyFormation.Width + Formation.Width + flankRange);
                                    position.SetVec2(calcPosition);
                                }
                                else
                                {
                                    position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                     enemyDirection.Normalized() * 150f);
                                }
                            }
                            else if (_behaviorSide == FormationAI.BehaviorSide.Left ||
                                     FlankSide == FormationAI.BehaviorSide.Left)
                            {
                                if (allyFormation != null)
                                {
                                    var calcPosition = allyFormation.CurrentPosition +
                                                       enemyDirection.LeftVec().Normalized() *
                                                       (allyFormation.Width + Formation.Width + flankRange);
                                    position.SetVec2(calcPosition);
                                }
                                else
                                {
                                    position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                     enemyDirection.Normalized() * 150f);
                                }
                            }
                            else
                            {
                                if (allyFormation != null)
                                    position = allyFormation.QuerySystem.MedianPosition;
                                else
                                    position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                     enemyDirection.Normalized() * 150f);
                            }
                        }
                        else
                        {
                            position = WorldPosition.Invalid;
                        }

                        break;
                    }
                    case SkirmishMode.Returning:
                    {
                        _attackTimer = null;
                        if (_returnTimer == null)
                            _returnTimer = new Timer(Mission.Current.CurrentTime, 10f / mobilityModifier);
                        if (_returnTimer != null && _returnTimer.Check(Mission.Current.CurrentTime))
                            _skirmishMode = SkirmishMode.Reform;

                        if (_behaviorSide == FormationAI.BehaviorSide.Right ||
                            FlankSide == FormationAI.BehaviorSide.Right)
                        {
                            if (allyFormation != null)
                            {
                                var calcPosition = allyFormation.CurrentPosition +
                                                   enemyDirection.RightVec().Normalized() *
                                                   (allyFormation.Width + Formation.Width + flankRange);
                                position.SetVec2(calcPosition);
                            }
                            else
                            {
                                position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                 enemyDirection.Normalized() * 150f);
                            }
                        }
                        else if (_behaviorSide == FormationAI.BehaviorSide.Left ||
                                 FlankSide == FormationAI.BehaviorSide.Left)
                        {
                            if (allyFormation != null)
                            {
                                var calcPosition = allyFormation.CurrentPosition +
                                                   enemyDirection.LeftVec().Normalized() *
                                                   (allyFormation.Width + Formation.Width + flankRange);
                                position.SetVec2(calcPosition);
                            }
                            else
                            {
                                position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                 enemyDirection.Normalized() * 150f);
                            }
                        }
                        else
                        {
                            if (allyFormation != null)
                                position = allyFormation.QuerySystem.MedianPosition;
                            else
                                position.SetVec2(medianTargetFormationPosition.AsVec2 +
                                                 enemyDirection.Normalized() * 150f);
                        }

                        break;
                    }
                    case SkirmishMode.Attack:
                    {
                        if (enemyFormation != null)
                        {
                            _reformTimer = null;
                            if (averagePosition.Distance(enemyFormation.QuerySystem.AveragePosition) < skirmishRange ||
                                Formation.QuerySystem.MakingRangedAttackRatio > 0.1f)
                                if (_attackTimer == null)
                                    _attackTimer = new Timer(Mission.Current.CurrentTime, 3f * mobilityModifier);
                            if (_attackTimer != null && _attackTimer.Check(Mission.Current.CurrentTime))
                                _skirmishMode = SkirmishMode.Returning;

                            if (_behaviorSide == FormationAI.BehaviorSide.Right ||
                                FlankSide == FormationAI.BehaviorSide.Right)
                            {
                                position = medianTargetFormationPosition;
                                var calcPosition = position.AsVec2 -
                                                   enemyDirection * (skirmishRange - (10f + Formation.Depth * 0.5f));
                                calcPosition = calcPosition + enemyFormation.Direction.LeftVec().Normalized() *
                                    (enemyFormation.Width / 2f) * mobilityModifier;
                                position.SetVec2(calcPosition);
                            }
                            else if (_behaviorSide == FormationAI.BehaviorSide.Left ||
                                     FlankSide == FormationAI.BehaviorSide.Left)
                            {
                                position = medianTargetFormationPosition;
                                var calcPosition = position.AsVec2 -
                                                   enemyDirection * (skirmishRange - (10f + Formation.Depth * 0.5f));
                                calcPosition = calcPosition + enemyFormation.Direction.RightVec().Normalized() *
                                    (enemyFormation.Width / 2f) * mobilityModifier;
                                position.SetVec2(calcPosition);
                            }
                            else
                            {
                                position = medianTargetFormationPosition;
                                var calcPosition = position.AsVec2 -
                                                   enemyDirection * (skirmishRange - (10f + Formation.Depth * 0.5f));
                                position.SetVec2(calcPosition);
                            }
                        }
                        else
                        {
                            position = WorldPosition.Invalid;
                        }

                        break;
                    }
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