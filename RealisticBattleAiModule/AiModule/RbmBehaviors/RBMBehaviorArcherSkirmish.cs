﻿using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
	class RBMBehaviorArcherSkirmish : BehaviorComponent
    {
        private Formation _mainFormation;

		private int flankCooldownMax = 40;
		//public float customWidth = 110f;
        public Timer repositionTimer = null;
        public Timer refreshPositionTimer = null;
        public Timer flankinTimer = null;
		public int side = MBRandom.RandomInt(2);
		public int cooldown = 0;
        public bool nudgeFormation;

		public bool wasShootingBefore = false;
		private enum BehaviorState
		{
			Approaching,
			Shooting,
			PullingBack,
            Flanking
		}

        private BehaviorState _behaviorState = BehaviorState.PullingBack;

		private Timer _cantShootTimer;

        private bool firstTime = true;

		public RBMBehaviorArcherSkirmish(Formation formation)
			: base(formation)
		{
            this._mainFormation = formation.Team.Formations.FirstOrDefault<Formation>((Func<Formation, bool>) (f => f.AI.IsMainFormation));
			base.BehaviorCoherence = 0.5f;
			_cantShootTimer = new Timer(0f, 0f);
			CalculateCurrentOrder();
		}

		protected override void CalculateCurrentOrder()
		{
			WorldPosition medianPosition = base.Formation.QuerySystem.MedianPosition;
			bool flag = false;
			Vec2 vec;
			Vec2 vec2;
            if (base.Formation.CountOfUnits <= 1)
            {
                vec = base.Formation.Direction;
                medianPosition.SetVec2(base.Formation.QuerySystem.AveragePosition);
                base.CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                return;
            }
            if (base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
            {
                vec = base.Formation.Direction;
                medianPosition.SetVec2(base.Formation.QuerySystem.AveragePosition);
            }
            else
            {
                Formation significantEnemy = null;
                if (base.Formation != null && base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    significantEnemy = RBMAI.Utilities.FindSignificantEnemy(base.Formation, true, false, false, false, false, true);
                }
                if (significantEnemy == null)
                {
                    significantEnemy = base.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                }
                Formation significantAlly = null;
                significantAlly = RBMAI.Utilities.FindSignificantAlly(base.Formation, true, false, false, false, false, true);

                //vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 - base.Formation.QuerySystem.MedianPosition.AsVec2;
                vec = significantEnemy.SmoothedAverageUnitPosition- base.Formation.SmoothedAverageUnitPosition;
                float distance = vec.Normalize();

                bool isFormationShooting = Utilities.IsFormationShooting(base.Formation);
                float effectiveShootingRange = (Formation.Depth / 2f) + (Formation.QuerySystem.MissileRange / 1.7f);
                FieldInfo _currentTacticField = typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
                _currentTacticField.DeclaringType.GetField("_currentTactic");
                if (base.Formation?.Team?.TeamAI != null)
                {
                    if (_currentTacticField.GetValue(base.Formation?.Team?.TeamAI) != null && _currentTacticField.GetValue(base.Formation?.Team?.TeamAI).ToString().Contains("SplitArchers"))
                    {
                        if (significantEnemy != null && base.Formation?.Team?.Formations.Where((Formation f) => f.CountOfUnits > 0).Count((Formation f) => f.QuerySystem.IsRangedFormation) > 1)
                        {
                            effectiveShootingRange += significantEnemy.Width / 3.5f;
                        }
                    }
                }

                if(significantEnemy != null)
                {
                    effectiveShootingRange += (significantEnemy.Depth / 2f);
                }

                if (significantAlly != null && (significantAlly == base.Formation || !significantAlly.QuerySystem.IsInfantryFormation))
                {
                    effectiveShootingRange *= 1.9f;
                }
                float rollPullBackAngle = 0f;
                BehaviorState previousBehavior = _behaviorState;
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
                                if (base.Formation.QuerySystem.IsRangedFormation && distance < effectiveShootingRange * 0.5f)
                                {
                                    Agent agent;
                                    Formation meleeFormation = RBMAI.Utilities.FindSignificantAlly(base.Formation, true, false, false, false, false);
                                    if (meleeFormation != null && meleeFormation.QuerySystem.IsInfantryFormation)
                                    {
                                        rollPullBackAngle = MBRandom.RandomFloat;
                                        _behaviorState = BehaviorState.PullingBack;
                                        break;
                                    }
                                }

                            }
                            else
                            {
                                //_cantShootDistance = distance;
                                if (base.Formation.QuerySystem.IsRangedFormation && distance < effectiveShootingRange * 0.4f)
                                {
                                    Formation meleeFormation = RBMAI.Utilities.FindSignificantAlly(base.Formation, true, false, false, false, false);
                                    if (meleeFormation != null && meleeFormation.QuerySystem.IsInfantryFormation && meleeFormation.QuerySystem.MedianPosition.AsVec2.Distance(base.Formation.QuerySystem.MedianPosition.AsVec2) <= base.Formation.QuerySystem.MissileRange)
                                    {
                                        rollPullBackAngle = MBRandom.RandomFloat;
                                        _behaviorState = BehaviorState.PullingBack;
                                        break;
                                    }
                                }
                                else
                                {
                                    if(refreshPositionTimer == null)
                                    {
                                        refreshPositionTimer = new Timer(Mission.Current.CurrentTime, 30f);
                                        _behaviorState = BehaviorState.Approaching;
                                    }
                                    else
                                    {
                                        if(refreshPositionTimer.Check(Mission.Current.CurrentTime)){
                                            refreshPositionTimer = null;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case BehaviorState.Approaching:
                        {
                            if (distance < effectiveShootingRange * 0.4f)
                            {
                                rollPullBackAngle = MBRandom.RandomFloat;
                                _behaviorState = BehaviorState.PullingBack;
                                flag = true;
                            }
                            else if (distance < effectiveShootingRange * 0.9f)
                            {
                                _behaviorState = BehaviorState.Shooting;
                                flag = true;
                            }
                            else if (Utilities.IsFormationShooting(base.Formation, 0.2f) && distance < effectiveShootingRange * 0.9f)
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
                            Formation meleeFormationPull = RBMAI.Utilities.FindSignificantAlly(base.Formation, true, false, false, false, false);
                            if (meleeFormationPull != null && meleeFormationPull.QuerySystem.MedianPosition.AsVec2.Distance(base.Formation.QuerySystem.MedianPosition.AsVec2) > base.Formation.QuerySystem.MissileRange)
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
                bool isOnlyCavReamining = RBMAI.Utilities.CheckIfOnlyCavRemaining(base.Formation);
                if (isOnlyCavReamining)
                {
                    _behaviorState = BehaviorState.Shooting;
                }

                bool shouldReposition = false;
                if(_behaviorState == BehaviorState.PullingBack)
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
                if (firstTime || previousBehavior != _behaviorState || shouldReposition)
                {
                    switch (_behaviorState)
                    {
                        case BehaviorState.Shooting:
                            medianPosition.SetVec2(base.Formation.QuerySystem.AveragePosition);
                            break;
                        case BehaviorState.Approaching:
                            rollPullBackAngle = MBRandom.RandomFloat;
                            
                            if (side == 0)
                            {
                                medianPosition.SetVec2(significantEnemy.QuerySystem.AveragePosition + significantEnemy.QuerySystem.MedianPosition.AsVec2.LeftVec().Normalized() * rollPullBackAngle * 70f);
                            }
                            else if (side == 1)
                            {
                                medianPosition.SetVec2(significantEnemy.QuerySystem.AveragePosition + significantEnemy.QuerySystem.MedianPosition.AsVec2.RightVec().Normalized() * rollPullBackAngle * 70f);
                            }
                            break;
                        case BehaviorState.PullingBack:
                            medianPosition = significantEnemy.QuerySystem.MedianPosition;
                            rollPullBackAngle = MBRandom.RandomFloat;
                            if (side == 0)
                            {
                                medianPosition.SetVec2((medianPosition.AsVec2 - vec * (effectiveShootingRange - base.Formation.Depth * 0.5f)) + significantEnemy.QuerySystem.MedianPosition.AsVec2.LeftVec().Normalized() * rollPullBackAngle * 70f);
                            }
                            else if (side == 1)
                            {
                                medianPosition.SetVec2((medianPosition.AsVec2 - (vec * (effectiveShootingRange - base.Formation.Depth * 0.5f)) + (significantEnemy.QuerySystem.MedianPosition.AsVec2.RightVec().Normalized() * (rollPullBackAngle * 70f))));
                            }
                            break;
                    }
                    if (!base.CurrentOrder.GetPosition(base.Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag)
                    {
                        base.CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                    }
                    if (!CurrentFacingOrder.GetDirection(base.Formation).IsValid || _behaviorState != BehaviorState.Shooting || flag)
                    {
                        Vec2 averageAllyFormationPosition = base.Formation.QuerySystem.Team.AveragePosition;
                        WorldPosition medianTargetFormationPosition = base.Formation.QuerySystem.Team.MedianTargetFormationPosition;
                        CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection((medianTargetFormationPosition.AsVec2 - base.Formation.QuerySystem.AveragePosition).Normalized());
                    }
                    firstTime = false;
                }
            }
        }

        public override void TickOccasionally()
		{
			CalculateCurrentOrder();
            //if(base.Formation.Width > customWidth)
            //{
            //    base.Formation.FormOrder = FormOrder.FormOrderCustom(customWidth);
            //}
            base.Formation.SetMovementOrder(base.CurrentOrder);
			base.Formation.FacingOrder = CurrentFacingOrder;
		}

		protected override void OnBehaviorActivatedAux()
		{
			//_cantShootDistance = float.MaxValue;
			_behaviorState = BehaviorState.PullingBack;
			_cantShootTimer.Reset(Mission.Current.CurrentTime, MBMath.Lerp(5f, 10f, (MBMath.ClampFloat(base.Formation.CountOfUnits, 10f, 60f) - 10f) * 0.02f));
			CalculateCurrentOrder();
			base.Formation.SetMovementOrder(base.CurrentOrder);
			base.Formation.FacingOrder = CurrentFacingOrder;
			base.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
			base.Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
			base.Formation.FormOrder = FormOrder.FormOrderWide;
			base.Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
		}

		protected override float GetAiWeight()
		{
			FormationQuerySystem querySystem = base.Formation.QuerySystem;
			return MBMath.Lerp(0.1f, 1f, MBMath.ClampFloat(querySystem.RangedUnitRatio + querySystem.RangedCavalryUnitRatio, 0f, 0.5f) * 2f);
		}

        public override TextObject GetBehaviorString()
        {
            TextObject behaviorString = new TextObject("Archer Skirmish");
            if (_mainFormation != null)
            {
                behaviorString.SetTextVariable("AI_SIDE", GameTexts.FindText("str_formation_ai_side_strings", _mainFormation.AI.Side.ToString()));
                behaviorString.SetTextVariable("CLASS", GameTexts.FindText("str_formation_class_string", _mainFormation.PrimaryClass.GetName()));
            }
            return behaviorString;
        }
}
}
