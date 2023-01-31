using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
	class RBMBehaviorEmbolon : BehaviorComponent
	{
		private Formation _mainFormation;

		public FormationAI.BehaviorSide FlankSide = FormationAI.BehaviorSide.Middle;

		public RBMBehaviorEmbolon(Formation formation)
			: base(formation)
		{
            _behaviorSide = formation.AI.Side;
			_mainFormation = formation.Team.Formations.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
            CalculateCurrentOrder();
		}

		protected override void CalculateCurrentOrder()
		{
			Vec2 direction;
			WorldPosition medianPosition;
			if (_mainFormation != null)
			{
				direction = _mainFormation.Direction;
				Vec2 vec = (base.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 - _mainFormation.QuerySystem.MedianPosition.AsVec2).Normalized();
				medianPosition = _mainFormation.QuerySystem.MedianPosition;
				medianPosition.SetVec2(_mainFormation.CurrentPosition + vec * ((_mainFormation.Depth + base.Formation.Depth) * 0.5f + 20f));
			}
			else
			{
				direction = base.Formation.Direction;
				medianPosition = base.Formation.QuerySystem.MedianPosition;
				medianPosition.SetVec2(base.Formation.QuerySystem.AveragePosition);
			}
			base.CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
		}

		public override void OnValidBehaviorSideSet()
		{
            base.OnValidBehaviorSideSet();
            _mainFormation = base.Formation.Team.Formations.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
        }

		public override void TickOccasionally()
		{
			CalculateCurrentOrder();
			base.Formation.SetMovementOrder(base.CurrentOrder);
			base.Formation.FacingOrder = CurrentFacingOrder;
			//if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && base.Formation.QuerySystem.AveragePosition.DistanceSquared(base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2) > 1600f && base.Formation.QuerySystem.UnderRangedAttackRatio > 0.2f - ((base.Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose) ? 0.1f : 0f))
			//{
			//	base.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
			//}
			//else
			//{
			//	base.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
			//}
		}

		protected override void OnBehaviorActivatedAux()
		{
			CalculateCurrentOrder();
			base.Formation.SetMovementOrder(base.CurrentOrder);
			base.Formation.FacingOrder = CurrentFacingOrder;
			base.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
			base.Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
			base.Formation.FormOrder = FormOrder.FormOrderDeep;
			base.Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
		}

		protected override float GetAiWeight()
		{
			if (_mainFormation == null || !_mainFormation.AI.IsMainFormation)
			{
				_mainFormation = base.Formation.Team.Formations.FirstOrDefaultQ((Formation f) => f.CountOfUnits > 0 && f.AI.IsMainFormation);
            }
			if (_mainFormation == null || base.Formation.AI.IsMainFormation)
			{
				return 0f;
			}
			return 1.2f;
		}

        public override TextObject GetBehaviorString()
        {
            TextObject behaviorString = new TextObject("Embolon");
            if (_mainFormation != null)
            {
                behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString()));
                behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", _mainFormation.PrimaryClass.GetName()));
            }
            return behaviorString;
        }
}
}
