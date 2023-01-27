using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
    internal class RBMBehaviorInfantryAttackFlank : BehaviorComponent
    {
        private bool _isEnemyReachable = true;
        private FlankMode flankMode;
        

        public FormationAI.BehaviorSide FlankSide = FormationAI.BehaviorSide.Middle;

        public RBMBehaviorInfantryAttackFlank(Formation formation)
            : base(formation)
        {
            flankMode = FlankMode.Flank;
            _behaviorSide = formation.AI.Side;
            CalculateCurrentOrder();
            BehaviorCoherence = 0.5f;
        }

        protected override float GetAiWeight()
        {
            return 2f;
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
            var fqsSigEnemy = fqs.ClosestSignificantlyLargeEnemyFormation;
            var position = fqs.MedianPosition;
            _isEnemyReachable = fqsSigEnemy != null 
                                && (!(Formation.Team.TeamAI is TeamAISiegeComponent) || !TeamAISiegeComponent.IsFormationInsideCastle(fqsSigEnemy.Formation, false));
            var averagePosition = fqs.AveragePosition;

            if (!_isEnemyReachable)
            {
                position.SetVec2(averagePosition);
                return;
            }

            const float flankRange = 45f;
            var enemyFormation = fqsSigEnemy?.Formation;

            if (Formation != null && fqs.IsInfantryFormation)
                enemyFormation = Utilities.FindSignificantEnemyToPosition(Formation, position, true, true, false, false, false, false);

            if (enemyFormation == null)
            {
                CurrentOrder = MovementOrder.MovementOrderStop;
                return;
            }

            var averageAllyFormationPosition = fqs.Team.AveragePosition;
            var medianTargetFormationPosition = fqs.Team.MedianTargetFormationPosition;
            var enemyDirection = (medianTargetFormationPosition.AsVec2 - averageAllyFormationPosition).Normalized();

            switch (flankMode)
            {
                case FlankMode.Flank:

                    if (averagePosition.Distance(enemyFormation.QuerySystem.AveragePosition) < flankRange)
                        flankMode = FlankMode.Attack;

                    Vec2 calcPosition;

                    if (_behaviorSide == FormationAI.BehaviorSide.Right || FlankSide == FormationAI.BehaviorSide.Right)
                    {
                        calcPosition = enemyFormation.CurrentPosition 
                                       + enemyDirection.RightVec().Normalized() 
                                       * (enemyFormation.Width * 0.5f + flankRange);
                        position.SetVec2(calcPosition);
                    }
                    else if (_behaviorSide == FormationAI.BehaviorSide.Left || FlankSide == FormationAI.BehaviorSide.Left)
                    {
                        calcPosition = enemyFormation.CurrentPosition 
                                       + enemyDirection.LeftVec().Normalized() 
                                       * (enemyFormation.Width * 0.5f + flankRange);
                        position.SetVec2(calcPosition);
                    }
                    else
                    {
                        position = enemyFormation.QuerySystem.MedianPosition;
                    }

                    break;

                case FlankMode.Attack:

                    CurrentOrder = MovementOrder.MovementOrderChargeToTarget(enemyFormation);
                    return;
            }

            CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);

            CurrentOrder = MovementOrder.MovementOrderMove(position);
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }

        public override TextObject GetBehaviorString()
        {
            var name = GetType().Name;
            return GameTexts.FindText("str_formation_ai_sergeant_instruction_behavior_text", name);
        }

        private enum FlankMode
        {
            Flank,
            Feint,
            Attack
        }
    }
}