using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI
{
    internal class RBMBehaviorArcherFlank : BehaviorComponent
    {
        private bool _isEnemyReachable = true;
        //private Timer returnTimer = null;
        //private Timer feintTimer = null;
        //private Timer attackTimer = null;

        public FormationAI.BehaviorSide FlankSide = FormationAI.BehaviorSide.Middle;

        public RBMBehaviorArcherFlank(Formation formation)
            : base(formation)
        {
            _behaviorSide = formation.AI.Side;
            CalculateCurrentOrder();
            BehaviorCoherence = 0.5f;
            base.NavmeshlessTargetPositionPenalty = 1f;
        }

        protected override float GetAiWeight()
        {
            return 1000f;
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWide;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        protected override void CalculateCurrentOrder()
        {
            var position = Formation.QuerySystem.MedianPosition;
            var averagePosition = Formation.QuerySystem.AveragePosition;

            var flankRange = 20f;

            var allyFormation = Utilities.FindSignificantAlly(Formation, true, false, false, false, false);

            var averageAllyFormationPosition = Formation.QuerySystem.Team.AveragePosition;
            var medianTargetFormationPosition = Formation.QuerySystem.Team.MedianTargetFormationPosition;
            var enemyDirection = (medianTargetFormationPosition.AsVec2 - averagePosition).Normalized();

            if (_behaviorSide == FormationAI.BehaviorSide.Right || FlankSide == FormationAI.BehaviorSide.Right)
            {
                if (allyFormation != null)
                {
                    var calcPosition = allyFormation.CurrentPosition + allyFormation.Direction.RightVec().Normalized() *
                        (allyFormation.Width * 0.5f + Formation.Width * 0.5f + flankRange);

                    position.SetVec2(calcPosition);
                    if (!Mission.Current.IsPositionInsideBoundaries(calcPosition) ||
                        position.GetNavMesh() == UIntPtr.Zero)
                    {
                        calcPosition = allyFormation.CurrentPosition;
                        calcPosition -= allyFormation.Direction * 5f;
                        position.SetVec2(calcPosition);
                    }
                }
                else
                {
                    position.SetVec2(medianTargetFormationPosition.AsVec2 + enemyDirection.Normalized() * 150f);
                }
            }
            else if (_behaviorSide == FormationAI.BehaviorSide.Left || FlankSide == FormationAI.BehaviorSide.Left)
            {
                if (allyFormation != null)
                {
                    var calcPosition = allyFormation.CurrentPosition + allyFormation.Direction.LeftVec().Normalized() *
                        (allyFormation.Width * 0.5f + Formation.Width * 0.5f + flankRange);
                    position.SetVec2(calcPosition);
                    if (!Mission.Current.IsPositionInsideBoundaries(calcPosition) ||
                        position.GetNavMesh() == UIntPtr.Zero)
                    {
                        calcPosition = allyFormation.CurrentPosition;
                        calcPosition -= allyFormation.Direction * 10f;
                        position.SetVec2(calcPosition);
                    }
                }
                else
                {
                    position.SetVec2(medianTargetFormationPosition.AsVec2 + enemyDirection.Normalized() * 150f);
                }
            }
            else
            {
                if (allyFormation != null)
                    position = allyFormation.QuerySystem.MedianPosition;
                else
                    position.SetVec2(medianTargetFormationPosition.AsVec2 + enemyDirection.Normalized() * 150f);
            }

            CurrentOrder = MovementOrder.MovementOrderMove(position);
            var angle = CurrentFacingOrder.GetDirection(Formation).AngleBetween(enemyDirection);
            if (angle > 0.2f || angle < -0.2f)
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
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
    }
}