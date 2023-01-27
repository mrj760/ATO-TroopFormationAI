using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmBehaviors
{
    public class RBMBehaviorHorseArcherSkirmish : BehaviorComponent
    {
        private bool _engaging = true;

        private bool _isEnemyReachable = true;

        public RBMBehaviorHorseArcherSkirmish(Formation formation)
            : base(formation)
        {
            CalculateCurrentOrder();
            BehaviorCoherence = 0.5f;
        }

        protected sealed override void CalculateCurrentOrder()
        {
            var fqs = Formation.QuerySystem;
            var position = fqs.MedianPosition;
            var targetFormation = Utilities.FindSignificantEnemy(Formation, true, true, false, false, false);
            _isEnemyReachable = targetFormation != null && (!(Formation.Team.TeamAI is TeamAISiegeComponent) ||
                                                            !TeamAISiegeComponent.IsFormationInsideCastle(
                                                                targetFormation, false));
            if (!_isEnemyReachable)
            {
                position.SetVec2(fqs.AveragePosition);
            }
            else
            {
                var num = (fqs.AverageAllyPosition - Formation.Team.QuerySystem.AverageEnemyPosition)
                    .LengthSquared <= 3600f;
                _engaging = num 
                            || (!_engaging 
                                ? (fqs.AveragePosition - fqs.AverageAllyPosition).LengthSquared <= 3600f 
                                : !(fqs.UnderRangedAttackRatio * 0.5f > fqs.MakingRangedAttackRatio));
                if (_engaging)
                {
                    if (targetFormation != null)
                    {
                        var distance = 60f;
                        if (!fqs.IsRangedCavalryFormation) distance = 30f;
                        var ellipse = new Ellipse(targetFormation.QuerySystem.MedianPosition.AsVec2, distance,
                            targetFormation.Width * 0.5f, targetFormation.Direction);
                        position.SetVec2(ellipse.GetTargetPos(fqs.AveragePosition, 35f));
                        CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(targetFormation.QuerySystem.AveragePosition);
                    }
                }
                else
                {
                    position = new WorldPosition(Mission.Current.Scene,
                        new Vec3(fqs.AverageAllyPosition,
                            Formation.Team.QuerySystem.MedianPosition.GetNavMeshZ() + 100f));
                }
            }

            if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(position.AsVec2))
            {
                position = fqs.MedianPosition;
                CurrentOrder = MovementOrder.MovementOrderMove(position);
            }
            else
            {
                CurrentOrder = MovementOrder.MovementOrderMove(position);
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
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
            Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
        }

        protected override float GetAiWeight()
        {
            var fqs = Formation.QuerySystem;

            if (Formation != null && fqs.IsCavalryFormation)
            {
                return Utilities.CheckIfMountedSkirmishFormation(Formation, 0.6f) ? 5f : 0f;
            }

            if (Formation != null && fqs.IsRangedCavalryFormation)
            {
                var enemyFormation = Utilities.FindSignificantEnemy(Formation, false, false, true, false, false);
                var efqs = enemyFormation.QuerySystem;
                if (efqs.IsCavalryFormation 
                    && fqs.MedianPosition.AsVec2.Distance(efqs.MedianPosition.AsVec2) < 55f 
                    && enemyFormation.CountOfUnits >= Formation.CountOfUnits * 0.5f)
                {
                    return 0.01f;
                }

                return !_isEnemyReachable ? 0.01f : 100f;
            }

            var countOfSkirmishers = 0f;
            Formation?.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                if (Utilities.CheckIfSkirmisherAgent(agent, 1)) countOfSkirmishers++;
            });

            if (Formation != null && countOfSkirmishers / Formation.CountOfUnits > 0.6f)
                return 1f;

            return 0f;
        }

        private struct Ellipse
        {
            private readonly Vec2 _center;

            private readonly float _radius;

            private readonly float _halfLength;

            private Vec2 _direction;

            public Ellipse(Vec2 center, float radius, float halfLength, Vec2 direction)
            {
                _center = center;
                _radius = radius;
                _halfLength = halfLength;
                _direction = direction;
            }

            public Vec2 GetTargetPos(Vec2 position, float distance)
            {
                var vec = _direction.LeftVec();
                var vec2 = _center + vec * _halfLength;
                var vec3 = _center - vec * _halfLength;
                var vec4 = position - _center;
                var flag = vec4.Normalized().DotProduct(_direction) > 0f;
                var vec5 = vec4.DotProduct(vec) * vec;
                var flag2 = vec5.Length < _halfLength;
                var flag3 = true;

                if (flag2)
                {
                    position = _center + vec5 + _direction * (_radius * (flag ? 1 : -1));
                }
                else
                {
                    flag3 = vec5.DotProduct(vec) > 0f;
                    var vec6 = (position - (flag3 ? vec2 : vec3)).Normalized();
                    position = (flag3 ? vec2 : vec3) + vec6 * _radius;
                }

                var vec7 = _center + vec5;
                var num = (float)Math.PI * 2f * _radius;
                while (distance > 0f)
                    switch (flag2)
                    {
                        case true when flag:
                        {
                            var num2 = (vec2 - vec7).Length < distance ? (vec2 - vec7).Length : distance;
                            position = vec7 + (vec2 - vec7).Normalized() * num2;
                            position += _direction * _radius;
                            distance -= num2;
                            flag2 = false;
                            flag3 = true;
                            break;
                        }
                        case false when flag3:
                        {
                            var v = (position - vec2).Normalized();
                            var num3 = MathF.Acos(MBMath.ClampFloat(_direction.DotProduct(v), -1f, 1f));
                            var num4 = (float)Math.PI * 2f * (distance / num);
                            var num5 = num3 + num4 < (float)Math.PI ? num3 + num4 : (float)Math.PI;
                            var num6 = (num5 - num3) / (float)Math.PI * (num / 2f);
                            var direction = _direction;
                            direction.RotateCCW(num5);
                            position = vec2 + direction * _radius;
                            distance -= num6;
                            flag2 = true;
                            flag = false;
                            break;
                        }
                        case true:
                        {
                            var num7 = (vec3 - vec7).Length < distance ? (vec3 - vec7).Length : distance;
                            position = vec7 + (vec3 - vec7).Normalized() * num7;
                            position -= _direction * _radius;
                            distance -= num7;
                            flag2 = false;
                            flag3 = false;
                            break;
                        }
                        default:
                        {
                            var vec8 = (position - vec3).Normalized();
                            var num8 = MathF.Acos(MBMath.ClampFloat(_direction.DotProduct(vec8), -1f, 1f));
                            var num9 = (float)Math.PI * 2f * (distance / num);
                            var num10 = num8 - num9 > 0f ? num8 - num9 : 0f;
                            var num11 = num8 - num10;
                            var num12 = num11 / (float)Math.PI * (num / 2f);
                            var vec9 = vec8;
                            vec9.RotateCCW(num11);
                            position = vec3 + vec9 * _radius;
                            distance -= num12;
                            flag2 = true;
                            flag = true;
                            break;
                        }
                    }

                return position;
            }
        }
    }
}