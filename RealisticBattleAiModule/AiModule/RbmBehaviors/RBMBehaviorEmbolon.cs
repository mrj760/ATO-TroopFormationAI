using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
    internal class RBMBehaviorEmbolon : BehaviorComponent
    {
        private Formation _mainFormation;

        public RBMBehaviorEmbolon(Formation formation)
            : base(formation)
        {
            _behaviorSide = formation.AI.Side;
            _mainFormation = formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);
            CalculateCurrentOrder();
        }

        protected sealed override void CalculateCurrentOrder()
        {
            Vec2 direction;
            WorldPosition medianPosition;
            if (_mainFormation != null)
            {
                direction = _mainFormation.Direction;
                var vec = (Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 -
                           _mainFormation.QuerySystem.MedianPosition.AsVec2).Normalized();
                medianPosition = _mainFormation.QuerySystem.MedianPosition;
                medianPosition.SetVec2(_mainFormation.CurrentPosition +
                                       vec * ((_mainFormation.Depth + Formation.Depth) * 0.5f + 20f));
            }
            else
            {
                direction = Formation.Direction;
                medianPosition = Formation.QuerySystem.MedianPosition;
                medianPosition.SetVec2(Formation.QuerySystem.AveragePosition);
            }

            CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
            CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
        }

        public override void OnValidBehaviorSideSet()
        {
            base.OnValidBehaviorSideSet();
            _mainFormation = Formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        public override TextObject GetBehaviorString()
        {
            var behaviorString = base.GetBehaviorString();
            var variable = GameTexts.FindText("str_formation_ai_side_strings", Formation.AI.Side.ToString());
            behaviorString.SetTextVariable("SIDE_STRING", variable);

            if (_mainFormation == null) return behaviorString;

            variable = GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString());
            behaviorString.SetTextVariable("AI_SIDE", variable);

            variable = GameTexts.FindText("str_formation_class_string", _mainFormation.PrimaryClass.GetName());
            behaviorString.SetTextVariable("CLASS", variable);

            return behaviorString;
        }

        protected override float GetAiWeight()
        {
            if (_mainFormation != null && _mainFormation.AI.IsMainFormation) 
                return 0f;

            _mainFormation = Formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);
            return 1.2f;

        }
    }
}