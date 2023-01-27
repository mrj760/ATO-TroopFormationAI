using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.ArrangementOrder;
using static TaleWorlds.MountAndBlade.HumanAIComponent;

namespace RBMAI.AiModule
{
    [HarmonyPatch]
    internal class Behaviours
    {
        [HarmonyPatch(typeof(BehaviorSkirmishLine))]
        private class OverrideBehaviorSkirmishLine
        {
            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(Formation ____mainFormation,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (____mainFormation != null)
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(____mainFormation.Direction);
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            private static void PostfixOnBehaviorActivatedAux(ref BehaviorSkirmishLine __instance)
            {
                __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
                __instance.Formation.FormOrder = FormOrder.FormOrderCustom(110f);
            }
        }

        [HarmonyPatch(typeof(BehaviorDefend))]
        private class OverrideBehaviorDefend
        {
            private static readonly Dictionary<Formation, WorldPosition> positionsStorage =
                new Dictionary<Formation, WorldPosition>();

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(ref BehaviorDefend __instance,
                ref MovementOrder ____currentOrder, ref bool ___IsCurrentOrderChanged,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (__instance.Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
                    return;

                var medianPositionNew = __instance.Formation.QuerySystem.MedianPosition;
                medianPositionNew.SetVec2(__instance.Formation.QuerySystem.AveragePosition);

                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                if (significantEnemy == null)
                    return;

                var enemyDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                     __instance.Formation.QuerySystem.MedianPosition.AsVec2;

                if (enemyDirection.Normalize() < 200f)
                {
                    positionsStorage.TryGetValue(__instance.Formation, out var newPosition);
                    ____currentOrder = MovementOrder.MovementOrderMove(newPosition);
                    ___IsCurrentOrderChanged = true;
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
                }
                else
                {
                    if (__instance.DefensePosition.IsValid)
                    {
                        var newPosition = __instance.DefensePosition;
                        newPosition.SetVec2(newPosition.AsVec2 + __instance.Formation.Direction * 10f);
                        ____currentOrder = MovementOrder.MovementOrderMove(newPosition);
                        positionsStorage[__instance.Formation] = newPosition;

                        ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
                    }
                    else
                    {
                        var newPosition = medianPositionNew;
                        newPosition.SetVec2(newPosition.AsVec2 + __instance.Formation.Direction * 10f);
                        positionsStorage[__instance.Formation] = newPosition;
                        ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BehaviorHoldHighGround))]
        private class OverrideBehaviorHoldHighGround
        {
            private static readonly Dictionary<Formation, WorldPosition> positionsStorage =
                new Dictionary<Formation, WorldPosition>();

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(ref BehaviorHoldHighGround __instance,
                ref MovementOrder ____currentOrder, ref bool ___IsCurrentOrderChanged,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (__instance.Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
                    return;

                var medianPositionNew = __instance.Formation.QuerySystem.MedianPosition;
                medianPositionNew.SetVec2(__instance.Formation.QuerySystem.AveragePosition);

                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                if (significantEnemy == null)
                    return;

                var enemyDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                     __instance.Formation.QuerySystem.MedianPosition.AsVec2;

                if (enemyDirection.Normalize() < 200f)
                {
                    positionsStorage.TryGetValue(__instance.Formation, out var newPosition);
                    ____currentOrder = MovementOrder.MovementOrderMove(newPosition);
                    ___IsCurrentOrderChanged = true;
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
                }
                else
                {
                    var newPosition = medianPositionNew;
                    newPosition.SetVec2(newPosition.AsVec2 + __instance.Formation.Direction * 10f);
                    positionsStorage[__instance.Formation] = newPosition;
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(enemyDirection);
                }
            }
        }

        [HarmonyPatch(typeof(BehaviorScreenedSkirmish))]
        private class OverrideBehaviorScreenedSkirmish
        {
            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(ref Formation ____mainFormation,
                ref BehaviorScreenedSkirmish __instance, ref MovementOrder ____currentOrder,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (____mainFormation != null && (____mainFormation.CountOfUnits == 0 || !____mainFormation.IsInfantry()))
                {
                    ____mainFormation = __instance.Formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);
                }

                if (____mainFormation == null
                    || __instance.Formation == null
                    || ____mainFormation.CountOfUnits <= 0
                    || !____mainFormation.IsInfantry())
                    return;

                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(____mainFormation.Direction);
                var medianPosition = ____mainFormation.QuerySystem.MedianPosition;

                Vec2 calcPosition;

                if (__instance.Formation.QuerySystem.IsRangedCavalryFormation)
                {
                    calcPosition =
                        medianPosition.AsVec2
                        - ____mainFormation.Direction.Normalized()
                        * (____mainFormation.Depth / 2f + __instance.Formation.Depth / 2f + 15f);
                }
                else
                {
                    calcPosition =
                        medianPosition.AsVec2
                        - ____mainFormation.Direction.Normalized()
                        * (____mainFormation.Depth / 2f + __instance.Formation.Depth / 2f + 5f);
                }

                medianPosition.SetVec2(calcPosition);

                if (!Mission.Current.IsPositionInsideBoundaries(calcPosition) || medianPosition.GetNavMesh() == UIntPtr.Zero)
                {
                    medianPosition = ____mainFormation.QuerySystem.MedianPosition;
                }

                ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
            }

            [HarmonyPrefix]
            [HarmonyPatch("TickOccasionally")]
            private static bool PrefixTickOccasionally(Formation ____mainFormation, BehaviorScreenedSkirmish __instance,
                ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
            {
                var method = typeof(BehaviorScreenedSkirmish).GetMethod("CalculateCurrentOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                var _ = method?.DeclaringType?.GetMethod("CalculateCurrentOrder");
                method?.Invoke(__instance, new object[] { });
                __instance.Formation.SetMovementOrder(____currentOrder);
                __instance.Formation.FacingOrder = ___CurrentFacingOrder;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            private static void PostfixOnBehaviorActivatedAux(ref BehaviorScreenedSkirmish __instance)
            {
                __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
            }
        }
    }

    [HarmonyPatch(typeof(BehaviorCautiousAdvance))]
    internal class OverrideBehaviorCautiousAdvance
    {
        public static Dictionary<Formation, int> waitCountShootingStorage = new Dictionary<Formation, int>();
        public static Dictionary<Formation, int> waitCountApproachingStorage = new Dictionary<Formation, int>();

        [HarmonyPostfix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static void PostfixCalculateCurrentOrder(ref Vec2 ____shootPosition, ref Formation ____archerFormation,
            BehaviorCautiousAdvance __instance, ref BehaviorState ____behaviorState, ref MovementOrder ____currentOrder,
            ref FacingOrder ___CurrentFacingOrder)
        {
            var formation = __instance.Formation;
            var medpos = formation.QuerySystem.MedianPosition;

            if (____archerFormation == null || formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
                return;

            var significantEnemy = Utilities.FindSignificantEnemy(formation, true, true, false, false, false, false);

            if (significantEnemy == null)
                return;

            if (!waitCountShootingStorage.TryGetValue(formation, out _))
                waitCountShootingStorage[formation] = 0;
            if (!waitCountApproachingStorage.TryGetValue(formation, out _))
                waitCountApproachingStorage[formation] = 0;

            var vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 - medpos.AsVec2;
            var distance = vec.Normalize();

            switch (____behaviorState)
            {
                case BehaviorState.Shooting:
                    {
                        if (waitCountShootingStorage[formation] > 70)
                        {
                            if (distance > 100f)
                            {
                                var medianPosition = medpos;
                                medianPosition.SetVec2(medianPosition.AsVec2 + vec * 5f);
                                ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                            }

                            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                            waitCountShootingStorage[formation] = 0;
                            waitCountApproachingStorage[formation] = 0;
                        }
                        else
                        {
                            if (distance > 100f)
                                waitCountShootingStorage[formation] += 2;
                            else
                                waitCountShootingStorage[formation] += 1;
                            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                        }

                        break;
                    }
                case BehaviorState.Approaching:
                    {
                        if (distance > 160f)
                        {
                            var medianPosition = medpos;
                            medianPosition.SetVec2(medianPosition.AsVec2 + vec * 10f);
                            ____shootPosition = medianPosition.AsVec2 + vec * 10f;
                            ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                        }
                        else
                        {
                            if (waitCountApproachingStorage[formation] > 35)
                            {
                                if (distance < 150f)
                                {
                                    var medianPosition = medpos;
                                    medianPosition.SetVec2(medianPosition.AsVec2 + vec * 5f);
                                    ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                }

                                waitCountApproachingStorage[formation] = 0;
                            }
                            else
                            {
                                if (distance < 150f)
                                {
                                    var medianPosition = medpos;
                                    medianPosition.SetVec2(____shootPosition);
                                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                }

                                waitCountApproachingStorage[formation] += 1;
                            }
                        }

                        break;
                    }
                case BehaviorState.PullingBack:
                    {
                        if (waitCountApproachingStorage[formation] > 30)
                        {
                            if (distance < 150f)
                            {
                                var medianPosition = medpos;
                                medianPosition.SetVec2(medianPosition.AsVec2 - vec * 10f);
                                ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                            }

                            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                            waitCountApproachingStorage[formation] = 0;
                        }
                        else
                        {
                            if (distance < 150f)
                            {
                                var medianPosition = medpos;
                                medianPosition.SetVec2(____shootPosition);
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                            }

                            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                            waitCountApproachingStorage[formation] += 1;
                        }

                        break;
                    }
            }
        }

        private enum BehaviorState
        {
            Approaching,
            Shooting,
            PullingBack
        }
    }

    [HarmonyPatch(typeof(BehaviorMountedSkirmish))]
    internal class OverrideBehaviorMountedSkirmish
    {
        public enum RotationDirection
        {
            Left,
            Right
        }

        public static Dictionary<Formation, RotationChangeClass> rotationDirectionDictionary =
            new Dictionary<Formation, RotationChangeClass>();

        [HarmonyPostfix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static void PostfixCalculateCurrentOrder(BehaviorMountedSkirmish __instance, ref bool ____engaging,
            ref MovementOrder ____currentOrder, ref bool ____isEnemyReachable)
        {
            var form = __instance.Formation;
            var fqs = form.QuerySystem;

            var position = fqs.MedianPosition;
            var targetform = Utilities.FindSignificantEnemy(form, true, true, false, false, false);


            var targetfqs =
                targetform != null ?
                    targetform.QuerySystem
                    : fqs.ClosestSignificantlyLargeEnemyFormation;

            ____isEnemyReachable = targetfqs != null
                                   && (!(form.Team.TeamAI is TeamAISiegeComponent)
                                       || !TeamAISiegeComponent.IsFormationInsideCastle(targetfqs.Formation, false));
            if (!____isEnemyReachable)
            {
                position.SetVec2(fqs.AveragePosition);
            }
            else
            {
                var withinrange = (fqs.AverageAllyPosition - form.Team.QuerySystem.AverageEnemyPosition).LengthSquared <= 160000f;
                ____engaging = withinrange
                               || (!____engaging ?
                                   (fqs.AveragePosition - fqs.AverageAllyPosition).LengthSquared <= 160000f
                                   : !(fqs.UnderRangedAttackRatio * 0.2f > fqs.MakingRangedAttackRatio));
                if (!____engaging)
                {
                    position = new WorldPosition(Mission.Current.Scene,
                        new Vec3(fqs.AverageAllyPosition,
                            form.Team.QuerySystem.MedianPosition.GetNavMeshZ() + 100f));
                }
                else
                {
                    var closestEnemy = fqs.ClosestEnemyFormation;

                    var enemyFormation = targetfqs?.Formation;

                    if (fqs.IsInfantryFormation)
                    {
                        enemyFormation = Utilities.FindSignificantEnemyToPosition(form, position, true, true, false, false, false, false);
                    }

                    if (closestEnemy != null
                        && closestEnemy.AveragePosition.Distance(form.CurrentPosition)
                            < form.Depth / 2f + (closestEnemy.Formation.QuerySystem.FormationPower / fqs.FormationPower * 20f + 10f))
                    {
                        ____currentOrder = MovementOrder.MovementOrderChargeToTarget(closestEnemy.Formation);
                        return;
                    }

                    if (enemyFormation?.QuerySystem != null)
                    {
                        var distance = 60f;
                        if (!fqs.IsRangedCavalryFormation) distance = 30f;

                        if (!rotationDirectionDictionary.TryGetValue(form, out var rotDir))
                        {
                            rotDir = new RotationChangeClass();
                            rotationDirectionDictionary.Add(form, rotDir);
                        }

                        if (fqs.IsRangedCavalryFormation)
                        {
                            var radius = enemyFormation.ArrangementOrder == ArrangementOrderLoose
                                ? enemyFormation.Width * 0.5f
                                : enemyFormation.Width * 0.25f;

                            var ellipse = new Ellipse(enemyFormation.QuerySystem.MedianPosition.AsVec2, distance, radius, enemyFormation.Direction);

                            position.SetVec2(ellipse.GetTargetPos(form.SmoothedAverageUnitPosition, 25f, rotDir.rotationDirection));
                        }
                        else
                        {
                            var ellipse = new Ellipse(enemyFormation.QuerySystem.MedianPosition.AsVec2, distance, enemyFormation.Width * 0.5f, enemyFormation.Direction);
                            position.SetVec2(ellipse.GetTargetPos(form.SmoothedAverageUnitPosition, 25f, rotDir.rotationDirection));
                        }

                        if (rotDir.waitbeforeChangeCooldownCurrent > 0)
                        {
                            if (rotDir.waitbeforeChangeCooldownCurrent >
                                rotDir.waitbeforeChangeCooldownMax)
                            {
                                rotDir.waitbeforeChangeCooldownCurrent = 0;
                                rotationDirectionDictionary[form] = rotDir;
                            }
                            else
                            {
                                rotDir.waitbeforeChangeCooldownCurrent++;
                                rotationDirectionDictionary[form] = rotDir;
                            }

                            position.SetVec2(
                                enemyFormation.CurrentPosition
                                + enemyFormation.Direction.Normalized()
                                    * (form.Depth / 2f + enemyFormation.Depth / 2f + 50f));

                            if (!Mission.Current.IsPositionInsideBoundaries(position.AsVec2) || position.GetNavMesh() == UIntPtr.Zero)
                            {
                                position.SetVec2(enemyFormation.CurrentPosition
                                                 + enemyFormation.Direction.Normalized()
                                                    * -(form.Depth / 2f + enemyFormation.Depth / 2f + 50f));
                            }
                        }

                        var distanceFromBoudnary = Mission.Current
                            .GetClosestBoundaryPosition(form.CurrentPosition)
                            .Distance(form.CurrentPosition);

                        if (distanceFromBoudnary <= form.Width / 2f)
                        {
                            if (rotDir.waitbeforeChangeCooldownCurrent >
                                rotDir.waitbeforeChangeCooldownMax)
                            {
                                rotDir.waitbeforeChangeCooldownCurrent = 0;
                                rotationDirectionDictionary[form] = rotDir;
                            }
                            else
                            {
                                rotDir.waitbeforeChangeCooldownCurrent++;
                                rotationDirectionDictionary[form] = rotDir;
                            }
                        }
                    }
                    else
                    {
                        position.SetVec2(fqs.AveragePosition);
                    }
                }
            }

            if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(position.AsVec2))
            {
                position = fqs.MedianPosition;
                ____currentOrder = MovementOrder.MovementOrderMove(position);
            }
            else
            {
                ____currentOrder = MovementOrder.MovementOrderMove(position);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetAiWeight")]
        private static void PostfixGetAiWeight(ref BehaviorMountedSkirmish __instance, ref float __result,
            ref bool ____isEnemyReachable)
        {
            var form = __instance?.Formation;
            var fqs = form?.QuerySystem;

            if (fqs == null)
                goto LabelFooAlligatorDonkeyBalls;

            if (fqs.IsCavalryFormation)
            {
                if (Utilities.CheckIfMountedSkirmishFormation(form, 0.6f))
                {
                    __result = 5f;
                    return;
                }

                __result = 0f;
                return;
            }

            if (fqs.IsRangedCavalryFormation)
            {
                var enemyCav = Utilities.FindSignificantEnemy(form, false, false, true, false, false);
                if (enemyCav != null
                    && enemyCav.QuerySystem.IsCavalryFormation
                    && fqs.MedianPosition.AsVec2.Distance(enemyCav.QuerySystem.MedianPosition.AsVec2) < 55f
                    && enemyCav.CountOfUnits >= form.CountOfUnits * 0.5f)
                {
                    __result = 0.01f;
                    return;
                }

                if (!____isEnemyReachable)
                {
                    __result = 0.01f;
                    return;
                }

                var powerSum = 0f;
                if (!Utilities.HasBattleBeenJoined(form, false))
                {
                    foreach (var enemyArcherFormation in Utilities.FindSignificantArcherFormations(form))
                    {
                        powerSum += enemyArcherFormation.QuerySystem.FormationPower;
                    }

                    if (powerSum > 0f
                        && fqs.FormationPower > 0f
                        && fqs.FormationPower / powerSum < 0.75f)
                    {
                        __result = 0.01f;
                        return;
                    }
                }

                __result = 100f;
                return;
            }

            LabelFooAlligatorDonkeyBalls:

            var countOfSkirmishers = 0;
            form?.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
            {
                if (Utilities.CheckIfSkirmisherAgent(agent, 1)) countOfSkirmishers++;
            });
            if (countOfSkirmishers / form?.CountOfUnits > 0.6f)
            {
                __result = 1f;
                return;
            }

            __result = 0f;
        }

        public class RotationChangeClass
        {
            public RotationDirection rotationDirection = RotationDirection.Left;
            public int waitbeforeChangeCooldownCurrent;
            public int waitbeforeChangeCooldownMax = 100;
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

            public Vec2 GetTargetPos(Vec2 position, float distance, RotationDirection rotationDirection)
            {
                var vec = rotationDirection == RotationDirection.Left
                    ? _direction.LeftVec()
                    : _direction.RightVec();

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
                var num = MathF.PI * 2f * _radius;
                while (distance > 0f)
                {
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
                                var num4 = MathF.PI * 2f * (distance / num);
                                var num5 = num3 + num4 < MathF.PI ? num3 + num4 : MathF.PI;
                                var num6 = (num5 - num3) / MathF.PI * (num / 2f);
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
                                var num9 = MathF.PI * 2f * (distance / num);
                                var num10 = num8 - num9 > 0f ? num8 - num9 : 0f;
                                var num11 = num8 - num10;
                                var num12 = num11 / MathF.PI * (num / 2f);
                                var vec9 = vec8;
                                vec9.RotateCCW(num11);
                                position = vec3 + vec9 * _radius;
                                distance -= num12;
                                flag2 = true;
                                flag = true;
                                break;
                            }
                    }
                }

                return position;
            }
        }
    }

    [HarmonyPatch(typeof(BehaviorProtectFlank))]
    internal class OverrideBehaviorProtectFlank
    {
        [HarmonyPrefix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static bool PrefixCalculateCurrentOrder(ref BehaviorProtectFlank __instance,
            ref FormationAI.BehaviorSide ___FlankSide, ref FacingOrder ___CurrentFacingOrder,
            ref MovementOrder ____currentOrder,
            ref MovementOrder ____movementOrder, ref BehaviorState ____protectFlankState,
            ref Formation ____mainFormation, ref FormationAI.BehaviorSide ____behaviorSide)
        {
            var form = __instance.Formation;
            var fqs = form?.QuerySystem;

            if (form == null || fqs == null)
                return true; // maybe supposed to be false? doubt it

            var position = fqs.MedianPosition;

            var distanceFromMainFormation = 90f;
            var closerDistanceFromMainFormation = 30f;
            var distanceOffsetFromMainFormation = 55f;

            if (fqs.IsInfantryFormation)
            {
                distanceFromMainFormation = 30f;
                closerDistanceFromMainFormation = 10f;
                distanceOffsetFromMainFormation = 30f;
            }

            if (____mainFormation == null || fqs.ClosestEnemyFormation == null)
            {
                ____currentOrder = MovementOrder.MovementOrderStop;
                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
            else if (____protectFlankState == BehaviorState.HoldingFlank || ____protectFlankState == BehaviorState.Returning)
            {
                var direction = ____mainFormation.Direction;
                var v = (fqs.Team.MedianTargetFormationPosition.AsVec2 - ____mainFormation.QuerySystem.MedianPosition.AsVec2).Normalized();
                Vec2 vec;
                if (____behaviorSide == FormationAI.BehaviorSide.Right || ___FlankSide == FormationAI.BehaviorSide.Right)
                {
                    vec = ____mainFormation.CurrentPosition + v.RightVec().Normalized() * (____mainFormation.Width / 2f + form.Width / 2f + distanceFromMainFormation);
                    vec -= v * (____mainFormation.Depth + form.Depth);
                    vec += ____mainFormation.Direction * (____mainFormation.Depth / 2f + form.Depth / 2f + distanceOffsetFromMainFormation);
                    position.SetVec2(vec);
                    if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec) || fqs.UnderRangedAttackRatio > 0.1f)
                    {
                        vec = ____mainFormation.CurrentPosition + v.RightVec().Normalized() * (____mainFormation.Width / 2f + form.Width / 2f + closerDistanceFromMainFormation);
                        vec -= v * (____mainFormation.Depth + form.Depth);
                        vec += ____mainFormation.Direction;
                        position.SetVec2(vec);
                        if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec))
                        {
                            vec = ____mainFormation.CurrentPosition + v.RightVec().Normalized();
                            vec -= ____mainFormation.Direction * 5f;
                            position.SetVec2(vec);
                        }
                    }
                }
                else if (____behaviorSide == FormationAI.BehaviorSide.Left || ___FlankSide == FormationAI.BehaviorSide.Left)
                {
                    vec = ____mainFormation.CurrentPosition + v.LeftVec().Normalized() * (____mainFormation.Width / 2f + form.Width / 2f + distanceFromMainFormation);
                    vec -= v * (____mainFormation.Depth + form.Depth);
                    vec += ____mainFormation.Direction * (____mainFormation.Depth / 2f + form.Depth / 2f + distanceOffsetFromMainFormation);
                    position.SetVec2(vec);
                    if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec) || fqs.UnderRangedAttackRatio > 0.1f)
                    {
                        vec = ____mainFormation.CurrentPosition + v.LeftVec().Normalized() * (____mainFormation.Width / 2f + form.Width / 2f + closerDistanceFromMainFormation);
                        vec -= v * (____mainFormation.Depth + form.Depth);
                        vec += ____mainFormation.Direction;
                        position.SetVec2(vec);
                        if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec))
                        {
                            vec = ____mainFormation.CurrentPosition + v.LeftVec().Normalized();
                            vec -= ____mainFormation.Direction * 10f;
                            position.SetVec2(vec);
                        }
                    }
                }
                else
                {
                    vec = ____mainFormation.CurrentPosition + v * ((____mainFormation.Depth + form.Depth) * 0.5f + 10f);
                    position.SetVec2(vec);
                }

                ____movementOrder = MovementOrder.MovementOrderMove(position);
                ____currentOrder = ____movementOrder;
                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
            }

            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch("CheckAndChangeState")]
        private static bool PrefixCheckAndChangeState(ref BehaviorProtectFlank __instance,
            ref MovementOrder ____currentOrder, ref MovementOrder ____chargeToTargetOrder,
            ref MovementOrder ____movementOrder, ref BehaviorState ____protectFlankState)
        {
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation)
            {
                if (__instance.Formation == null || ____movementOrder == null)
                    return true;

                var position = ____movementOrder.GetPosition(__instance.Formation);
                switch (____protectFlankState)
                {
                    case BehaviorState.Charging:

                        var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;
                        if (closestFormation?.Formation != null)
                        {
                            if (closestFormation == null)
                            {
                                ____currentOrder = ____movementOrder;
                                ____protectFlankState = BehaviorState.Returning;
                                break;
                            }

                            var returnDistance =
                                40f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                            if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) >
                                returnDistance * returnDistance)
                            {
                                ____currentOrder = ____movementOrder;
                                ____protectFlankState = BehaviorState.Returning;
                            }
                        }

                        break;

                    case BehaviorState.Returning:

                        if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) < 400f)
                            ____protectFlankState = BehaviorState.HoldingFlank;

                        break;
                }

                return false;
            }

            if (__instance.Formation == null || ____movementOrder == null)
                return true;

            {
                var position = ____movementOrder.GetPosition(__instance.Formation);
                switch (____protectFlankState)
                {
                    case BehaviorState.HoldingFlank:
                        {
                            var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;
                            if (closestFormation?.Formation != null
                                && (closestFormation.Formation.QuerySystem.IsCavalryFormation
                                    || closestFormation.Formation.QuerySystem.IsRangedCavalryFormation))
                            {
                                var changeToChargeDistance = 110f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                                if (closestFormation.Formation.QuerySystem.MedianPosition.AsVec2.Distance(position) < changeToChargeDistance
                                    || __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.1f)
                                {
                                    ____chargeToTargetOrder =
                                        MovementOrder.MovementOrderChargeToTarget(closestFormation.Formation);
                                    ____currentOrder = ____chargeToTargetOrder;
                                    ____protectFlankState = BehaviorState.Charging;
                                }
                            }

                            break;
                        }
                    case BehaviorState.Charging:
                        {
                            var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;

                            if (closestFormation?.Formation != null)
                            {
                                var returnDistance = 80f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                                if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) > returnDistance * returnDistance)
                                {
                                    ____currentOrder = ____movementOrder;
                                    ____protectFlankState = BehaviorState.Returning;
                                }
                            }

                            break;
                        }
                    case BehaviorState.Returning:

                        if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) < 400f)
                            ____protectFlankState = BehaviorState.HoldingFlank;

                        break;
                }

                return false;
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch("OnBehaviorActivatedAux")]
        private static void PostfixOnBehaviorActivatedAux(ref BehaviorProtectFlank __instance)
        {
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation)
                __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetAiWeight")]
        private static bool PostfixGetAiWeight(ref BehaviorProtectFlank __instance, ref float __result,
            ref Formation ____mainFormation)
        {
            if (____mainFormation == null || !____mainFormation.AI.IsMainFormation)
                ____mainFormation = __instance.Formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);

            if (____mainFormation == null || __instance.Formation.AI.IsMainFormation)
            {
                __result = 0f;
                return false;
            }

            __result = 10f;
            return false;
        }

        private enum BehaviorState
        {
            HoldingFlank,
            Charging,
            Returning
        }
    }

    [MBCallback]
    [HarmonyPatch(typeof(HumanAIComponent))]
    internal class OnDismountPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("AdjustSpeedLimit")]
        private static bool AdjustSpeedLimitPrefix(ref Agent agent,
            ref float desiredSpeed, ref bool limitIsMultiplier, ref Agent ___Agent)
        {
            if (agent.Formation != null && (agent.Formation.QuerySystem.IsRangedCavalryFormation
                                            || agent.Formation.QuerySystem.IsCavalryFormation))
            {
                if (agent.MountAgent != null)
                {
                    var speed = agent.MountAgent.AgentDrivenProperties.MountSpeed;
                    ___Agent.SetMaximumSpeedLimit(speed, false);
                    agent.MountAgent.SetMaximumSpeedLimit(speed, false);
                    return false;
                }
            }
            else if (agent.Formation?.AI?.ActiveBehavior != null
                     && (agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorForwardSkirmish)
                         || agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorInfantryAttackFlank))
                     && limitIsMultiplier && desiredSpeed < 0.85f)
            {
                desiredSpeed = 0.85f;
            }

            if (agent.Formation?.AI?.ActiveBehavior == null)
                return true;

            var type = agent.Formation.AI.ActiveBehavior.GetType();

            if (type == typeof(BehaviorProtectFlank) && desiredSpeed < 0.85f)
            {
                limitIsMultiplier = true;
                desiredSpeed = 0.85f;
                return true;
            }

            if (!limitIsMultiplier)
                return true;

            if (type == typeof(BehaviorRegroup) && desiredSpeed < 0.95f)
            {
                desiredSpeed = 0.95f;
            }
            else if (type == typeof(BehaviorCharge) && desiredSpeed < 0.85f)
            {
                desiredSpeed = 0.85f;
            }
            else if (type == typeof(RBMBehaviorArcherFlank) && desiredSpeed < 0.9f)
            {
                desiredSpeed = 0.9f;
            }
            else if (type == typeof(RBMBehaviorArcherSkirmish) && desiredSpeed < 0.9f)
            {
                desiredSpeed = 0.9f;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BehaviorHorseArcherSkirmish))]
    internal class OverrideBehaviorHorseArcherSkirmish
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetAiWeight")]
        private static bool PrefixGetAiWeight(ref float __result)
        {
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(BehaviorPullBack))]
    internal class OverrideBehaviorPullBack
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetAiWeight")]
        private static bool PrefixGetAiWeight(ref float __result)
        {
            __result = 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(BehaviorVanguard))]
    internal class OverrideBehaviorVanguard
    {
        [HarmonyPrefix]
        [HarmonyPatch("TickOccasionally")]
        private static bool PrefixTickOccasionally(ref MovementOrder ____currentOrder,
            ref FacingOrder ___CurrentFacingOrder, BehaviorVanguard __instance)
        {
            var method = typeof(BehaviorVanguard).GetMethod("CalculateCurrentOrder", BindingFlags.NonPublic | BindingFlags.Instance);
            var _ = method?.DeclaringType?.GetMethod("CalculateCurrentOrder");
            method?.Invoke(__instance, new object[] { });

            __instance.Formation.SetMovementOrder(____currentOrder);
            __instance.Formation.FacingOrder = ___CurrentFacingOrder;
            if (__instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null
                && __instance.Formation.QuerySystem.AveragePosition.DistanceSquared(__instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2) > 1600f
                && __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.2f - (__instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Loose ? 0.1f : 0f))
            {
                __instance.Formation.ArrangementOrder = ArrangementOrderSkein;
            }
            else
            {
                __instance.Formation.ArrangementOrder = ArrangementOrderSkein;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnBehaviorActivatedAux")]
        private static void PostfixOnBehaviorActivatedAux(BehaviorVanguard __instance)
        {
            __instance.Formation.FormOrder = FormOrder.FormOrderDeep;
            __instance.Formation.ArrangementOrder = ArrangementOrderSkein;
        }
    }

    [HarmonyPatch(typeof(BehaviorCharge))]
    internal class OverrideBehaviorCharge
    {
        public static Dictionary<Formation, WorldPosition>
            positionsStorage = new Dictionary<Formation, WorldPosition>();

        public static Dictionary<Formation, float> timeToMoveStorage = new Dictionary<Formation, float>();

        [HarmonyPrefix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static bool PrefixCalculateCurrentOrder(ref BehaviorCharge __instance,
            ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
        {
            var form = __instance.Formation;
            var fqs = form.QuerySystem;

            if (!(form.Team.IsPlayerTeam || form.Team.IsPlayerAlly)
                && Campaign.Current != null && MobileParty.MainParty != null && MobileParty.MainParty.MapEvent != null)
            {
                var defenderName = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender).Name;
                var attackerName = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker).Name;
                if (defenderName.Contains("Looter") || defenderName.Contains("Bandit")
                    || defenderName.Contains("Raider") || attackerName.Contains("Looter")
                    || attackerName.Contains("Bandit") || attackerName.Contains("Raider"))
                {
                    return true;
                }
            }

            if ((fqs.IsInfantryFormation || fqs.IsRangedFormation) 
                && fqs.ClosestSignificantlyLargeEnemyFormation != null)
            {
                var significantEnemy = Utilities.FindSignificantEnemy(form, true, true, false, false, false);

                if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle 
                    && fqs.IsInfantryFormation 
                    && !Utilities.FormationFightingInMelee(form, 0.5f))
                {
                    var enemyCav = Utilities.FindSignificantEnemy(form, false, false, true, false, false);

                    if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) 
                        enemyCav = null;

                    var cavDist = 0f;
                    var signDist = 1f;

                    if (significantEnemy != null)
                    {
                        var signDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                            fqs.MedianPosition.AsVec2;
                        signDist = signDirection.Normalize();
                    }

                    if (enemyCav != null)
                    {
                        var cavDirection = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                           fqs.MedianPosition.AsVec2;
                        cavDist = cavDirection.Normalize();
                    }

                    var isOnlyCavReamining = Utilities.CheckIfOnlyCavRemaining(form);

                    if (enemyCav != null 
                        && cavDist <= signDist 
                        && enemyCav.CountOfUnits > form.CountOfUnits / 10 
                        && (signDist > 35f || significantEnemy == enemyCav || isOnlyCavReamining))
                    {
                        if (isOnlyCavReamining)
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 
                                      - fqs.MedianPosition.AsVec2;
                            var positionNew = fqs.MedianPosition;

                            positionsStorage.TryGetValue(form, out var storedPosition);

                            if (!storedPosition.IsValid)
                            {
                                positionsStorage.Add(form, positionNew);
                                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                            }
                            else
                            {
                                var storedPositonDistance = 
                                    (storedPosition.AsVec2 - fqs.MedianPosition.AsVec2)
                                    .Normalize();
                                if (storedPositonDistance > form.Depth / 2f + 10f)
                                {
                                    positionsStorage.Remove(form);
                                    positionsStorage.Add(form, positionNew);
                                    ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                                }
                                else
                                {
                                    ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                                }
                            }

                            if (cavDist > 85f)
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());
                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                            return false;
                        }

                        if (!(form.AI?.Side == FormationAI.BehaviorSide.Left ||
                              form.AI?.Side == FormationAI.BehaviorSide.Right) &&
                            enemyCav.TargetFormation == form)
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                      fqs.MedianPosition.AsVec2;
                            var positionNew = fqs.MedianPosition;

                            positionsStorage.TryGetValue(form, out var storedPosition);

                            if (!storedPosition.IsValid)
                            {
                                positionsStorage.Add(form, positionNew);
                                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                            }
                            else
                            {
                                var storedPositonDistance =
                                    (storedPosition.AsVec2 - fqs.MedianPosition.AsVec2)
                                    .Normalize();
                                if (storedPositonDistance > form.Depth / 2f + 10f)
                                {
                                    positionsStorage.Remove(form);
                                    positionsStorage.Add(form, positionNew);
                                    ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                                }
                                else
                                {
                                    ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                                }
                            }

                            if (cavDist > 85f)
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());

                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                            return false;
                        }

                        positionsStorage.Remove(form);
                    }
                    else if (significantEnemy != null && !significantEnemy.QuerySystem.IsRangedFormation &&
                             signDist < 50f && Utilities.FormationActiveSkirmishersRatio(form, 0.38f))
                    {
                        var positionNew = fqs.MedianPosition;
                        positionNew.SetVec2(positionNew.AsVec2 - form.Direction * 7f);

                        positionsStorage.TryGetValue(form, out var storedPosition);

                        if (!storedPosition.IsValid)
                        {
                            positionsStorage.Add(form, positionNew);
                            ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                        }
                        else
                        {
                            ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                        }

                        __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                        return false;
                    }

                    positionsStorage.Remove(form);
                }

                if (significantEnemy != null)
                {
                    form.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                    ____currentOrder = MovementOrder.MovementOrderChargeToTarget(significantEnemy);

                    var targetform = form.TargetFormation;

                    if (targetform == null)
                    {
                        // skip
                    }
                    else if (targetform.ArrangementOrder == ArrangementOrderShieldWall
                        && Utilities.ShouldFormationCopyShieldWall(form))
                    {
                        __instance.Formation.ArrangementOrder = ArrangementOrderShieldWall;
                    }
                    else if (targetform.ArrangementOrder == ArrangementOrderLine)
                    {
                        __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                    }
                    else if (targetform.ArrangementOrder == ArrangementOrderLoose)
                    {
                        __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
                    }

                    return false;
                }
            }

            if (form?.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                form.TargetFormation = fqs.ClosestSignificantlyLargeEnemyFormation?.Formation;
            }

            ____currentOrder = MovementOrder.MovementOrderCharge;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetAiWeight")]
        private static void PrefixGetAiWeight(ref BehaviorCharge __instance, ref float __result)
        {
            if (__instance.Formation == null || !__instance.Formation.QuerySystem.IsRangedCavalryFormation) 
                return;
            __result *= 0.2f;
        }
    }

    [HarmonyPatch(typeof(MovementOrder))]
    internal class OverrideMovementOrder
    {
        public static Dictionary<Formation, WorldPosition>
            positionsStorage = new Dictionary<Formation, WorldPosition>();

        [HarmonyPrefix]
        [HarmonyPatch("SetChargeBehaviorValues")]
        private static bool PrefixSetChargeBehaviorValues(Agent unit)
        {
            if (unit?.Formation == null) 
                return true;

            if (unit.Formation.QuerySystem.IsRangedCavalryFormation)
            {
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 0.01f, 7f, 4f, 20f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 0.55f, 2f, 0.55f, 20f, 0.55f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 4f, 2f, 0.55f, 30f, 0.55f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 8f, 15f, 10f, 30f, 10f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            if (unit.Formation.QuerySystem.IsCavalryFormation)
            {
                if (unit.HasMount)
                {
                    if (Utilities.GetHarnessTier(unit) > 3)
                    {
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 4f, 20f, 1f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 25f, 5f, 30f, 5f);
                    }
                    else
                    {
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 1f, 2f, 1f, 20f, 1f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 25f, 5f, 30f, 5f);
                    }
                }
                else
                {
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 4f, 20f, 1f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 25f, 5f, 30f, 5f);
                }

                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 1f, 7f, 4f, 20f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0f, 10f, 3f, 20f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            if (unit.Formation.GetReadonlyMovementOrderReference().OrderType != OrderType.ChargeWithTarget) 
                return true;

            if (unit.Formation.QuerySystem.IsInfantryFormation)
            {
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 4f, 10f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 1f, 10f, 0.01f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 7f, 0.8f, 20f, 20f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 40f, 4f, 60f, 0f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 5f, 7f, 10f, 8, 20f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 0f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            if (!unit.Formation.QuerySystem.IsRangedFormation) 
                return true;

            unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 4f, 10f, 6f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 4f, 10f, 0.01f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 2f, 0f, 8f, 20f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 40f, 4f, 60f, 0f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 0f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
            return false;

        }

        [HarmonyPrefix]
        [HarmonyPatch("SetFollowBehaviorValues")]
        private static bool PrefixSetFollowBehaviorValues(Agent unit)
        {
            if (unit.Formation == null || !unit.Formation.QuerySystem.IsRangedCavalryFormation) 
                return true;

            unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 5f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 0.55f, 2f, 4f, 20f, 0.55f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.55f, 7f, 0.55f, 20f, 0.55f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 8f, 2f, 0.55f, 30f, 0.55f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 10f, 15f, 0.065f, 30f, 0.065f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
            return false;

        }

        [HarmonyPrefix]
        [HarmonyPatch("SetDefaultMoveBehaviorValues")]
        private static bool PrefixSetDefaultMoveBehaviorValues(Agent unit)
        {
            if (unit.Formation != null && unit.Formation.QuerySystem.IsRangedCavalryFormation)
            {
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 15f, 5f, 20f, 5f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 0f, 2f, 0f, 20f, 0f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.02f, 7f, 0.04f, 20f, 0.03f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 0.01f, 2f, 0.01f, 30f, 0.01f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 1f, 15f, 0.065f, 30f, 0.065f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
            {
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 4f, 3f, 20f, 0.01f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.02f, 7f, 0.04f, 20f, 0.03f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 10f, 7f, 5f, 30f, 0.05f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            if (unit.Formation == null) 
                return true;

            if (unit.Formation.GetReadonlyMovementOrderReference().OrderEnum ==
                MovementOrder.MovementOrderEnum.FallBack)
            {
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 0f, 4f, 0f, 20f, 0f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 7f, 0f, 20f, 0f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 10f, 7f, 5f, 30f, 0.05f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                return false;
            }

            unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 5f, 3f, 20f, 0.01f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.02f, 7f, 0.04f, 20f, 0.03f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 10f, 7f, 5f, 30f, 0.05f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 9f);
            unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
            return false;

        }

        [HarmonyPrefix]
        [HarmonyPatch("GetSubstituteOrder")]
        private static bool PrefixGetSubstituteOrder(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (formation == null ||
                (!formation.QuerySystem.IsInfantryFormation && !formation.QuerySystem.IsRangedFormation) ||
                __instance.OrderType != OrderType.ChargeWithTarget) 
                return true;

            if (formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                __result = MovementOrder.MovementOrderChargeToTarget(
                    formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
            }
            else
                __result = MovementOrder.MovementOrderCharge;

            return false;

        }

        [HarmonyPostfix]
        [HarmonyPatch("GetPositionAux")]
        private static void GetPositionAuxPostfix(ref MovementOrder __instance, ref WorldPosition __result,
            ref Formation f, ref WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
        {
            switch (__instance.OrderEnum)
            {
                case MovementOrder.MovementOrderEnum.FallBack:
                {
                    Vec2 directionAux;
                    if ((uint)(__instance.OrderEnum - 10) <= 1u)
                    {
                        var querySystem = f.QuerySystem;
                        var closestEnemyFormation = querySystem.ClosestEnemyFormation;
                        if (closestEnemyFormation == null)
                            directionAux = Vec2.One;
                        else
                            directionAux = (closestEnemyFormation.MedianPosition.AsVec2 - querySystem.AveragePosition)
                                .Normalized();
                    }
                    else
                    {
                        directionAux = Vec2.One;
                    }

                    var medianPosition = f.QuerySystem.MedianPosition;
                    medianPosition.SetVec2(f.QuerySystem.AveragePosition - directionAux * 0.35f);
                    __result = medianPosition;

                    return;
                }
                case MovementOrder.MovementOrderEnum.Advance:
                {
                    var enemyFormation = Utilities.FindSignificantEnemy(f, true, true, false, false, false);
                    var querySystem = f.QuerySystem;
                    var enemyQuerySystem = 
                        enemyFormation != null ? 
                            enemyFormation.QuerySystem : 
                            querySystem.ClosestEnemyFormation;

                    if (enemyQuerySystem == null)
                    {
                        __result = f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
                        return;
                    }

                    var oldPosition = enemyQuerySystem.MedianPosition;
                    var newPosition = enemyQuerySystem.MedianPosition;

                    if (querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation)
                    {
                        var effectiveMissileRange = querySystem.MissileRange / 2.25f;
                        if (!(newPosition.AsVec2.DistanceSquared(
                                querySystem.AveragePosition) > effectiveMissileRange * effectiveMissileRange))
                        {
                            var directionAux2 = (enemyQuerySystem.MedianPosition.AsVec2 - querySystem.MedianPosition.AsVec2)
                                .Normalized();

                            newPosition.SetVec2(newPosition.AsVec2 - directionAux2 * effectiveMissileRange);
                        }

                        if (oldPosition.AsVec2.Distance(newPosition.AsVec2) > 7f)
                        {
                            positionsStorage[f] = newPosition;
                            __result = newPosition;
                        }
                        else
                        {
                            if (positionsStorage.TryGetValue(f, out var tempPos))
                            {
                                __result = tempPos;
                                return;
                            }

                            __result = oldPosition;
                        }

                        return;
                    }

                    var vec = (enemyQuerySystem.AveragePosition - f.QuerySystem.AveragePosition).Normalized();
                    var distance = enemyQuerySystem.AveragePosition.Distance(f.QuerySystem.AveragePosition);
                    var num = 5f;

                    if (enemyQuerySystem.FormationPower < f.QuerySystem.FormationPower * 0.2f) 
                        num = 0.1f;

                    newPosition.SetVec2(newPosition.AsVec2 - vec * num);

                    if (distance > 7f)
                    {
                        positionsStorage[f] = newPosition;
                        __result = newPosition;
                    }
                    else
                    {
                        __instance = MovementOrder.MovementOrderChargeToTarget(enemyFormation);
                        if (positionsStorage.TryGetValue(f, out var tempPos))
                        {
                            __result = tempPos;
                            return;
                        }

                        __result = oldPosition;
                    }

                    break;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Agent))]
    internal class OverrideAgent
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetFiringOrder")]
        private static bool PrefixSetFiringOrder(ref Agent __instance, ref int order)
        {
            var form = __instance.Formation;

            if (form == null || form.GetReadonlyMovementOrderReference().OrderType != OrderType.ChargeWithTarget)
                return true;

            var fqs = form.QuerySystem;

            if (fqs.ClosestSignificantlyLargeEnemyFormation == null) 
                return true;

            if (!fqs.IsInfantryFormation || Utilities.FormationFightingInMelee(form, 0.5f))
                return true;

            var significantEnemy = Utilities.FindSignificantEnemy(form, true, true, false, false, false);

            var enemyCav = Utilities.FindSignificantEnemy(form, false, false, true, false, false);

            if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) enemyCav = null;

            var cavDist = 0f;
            var signDist = 1f;
            if (enemyCav != null && significantEnemy != null)
            {
                var cavDirection = enemyCav.QuerySystem.MedianPosition.AsVec2 - fqs.MedianPosition.AsVec2;
                cavDist = cavDirection.Normalize();

                var signDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 - fqs.MedianPosition.AsVec2;
                signDist = signDirection.Normalize();
            }

            if (enemyCav == null || !(cavDist <= signDist) || enemyCav.CountOfUnits <= form.CountOfUnits / 10 || !(signDist > 35f)) 
                return true;

            if (enemyCav.TargetFormation != form 
                || (enemyCav.GetReadonlyMovementOrderReference().OrderType != OrderType.ChargeWithTarget 
                    && enemyCav.GetReadonlyMovementOrderReference().OrderType != OrderType.Charge))
            {
                return true;
            }

            order = Utilities.CheckIfCanBrace(__instance) ? 1 : 0;

            return true;
        }
    }

    [HarmonyPatch(typeof(Agent))]
    internal class OverrideUpdateFormationOrders
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateFormationOrders")]
        private static bool PrefixUpdateFormationOrders(ref Agent __instance)
        {
            if (__instance.Formation == null || !__instance.IsAIControlled ||
                __instance.Formation.GetReadonlyMovementOrderReference().OrderType != OrderType.ChargeWithTarget)
            {
                return true;
            }

            if (__instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Square ||
                __instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Circle ||
                __instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.ShieldWall)
            {
                __instance.EnforceShieldUsage(GetShieldDirectionOfUnit(
                    __instance.Formation, __instance, __instance.Formation.ArrangementOrder.OrderEnum));
            }
            else
            {
                if (__instance.WieldedOffhandWeapon.IsEmpty) 
                    return false;

                var hasnotusableonehand =
                    __instance.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);
                var hasranged = __instance.IsRangedCached;
                var distance = 
                    __instance.GetTargetAgent() != null ? 
                        __instance.Position.Distance(__instance.GetTargetAgent().Position) 
                        : 100f;
                if (!hasnotusableonehand && !hasranged && __instance.GetTargetAgent() != null && distance < 7f)
                    __instance.EnforceShieldUsage(Agent.UsageDirection.DefendDown);
                else
                    __instance.EnforceShieldUsage(Agent.UsageDirection.None);
            }

            return false;

        }
    }

    [HarmonyPatch(typeof(Formation))]
    internal class SetPositioningPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetPositioning")]
        private static bool PrefixSetPositioning(ref Formation __instance, ref int? unitSpacing)
        {
            if (__instance.ArrangementOrder != ArrangementOrderScatter) 
                return true;

            unitSpacing = 2;

            return true;
        }
    }

    //[HarmonyPatch(typeof(OrderController))]
    //class OverrideOrderController
    //{
    //    [HarmonyPostfix]
    //    [HarmonyPatch("SetOrder")]
    //    static void PostfixSetOrder(OrderController __instance, OrderType orderType, ref Mission ____mission)
    //    {
    //        if (orderType == OrderType.Charge)
    //        {
    //            foreach (Formation selectedFormation in __instance.SelectedFormations)
    //            {
    //                //if ((selectedFormation.QuerySystem.IsInfantryFormation || selectedFormation.QuerySystem.IsRangedFormation) || ____mission.IsTeleportingAgents)
    //                //{
    //                if (selectedFormation.QuerySystem.ClosestEnemyFormation == null)
    //                {
    //                    selectedFormation.SetMovementOrder(MovementOrder.MovementOrderCharge);
    //                }
    //                else
    //                {
    //                    selectedFormation.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(selectedFormation.QuerySystem.ClosestEnemyFormation.Formation));
    //                }
    //                //}
    //            }
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(Formation))]
    internal class OverrideSetMovementOrder
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetMovementOrder")]
        private static bool PrefixSetOrder(Formation __instance, ref MovementOrder input)
        {
            if (input.OrderType != OrderType.Charge) 
                return true;

            if (__instance.QuerySystem.ClosestEnemyFormation != null)
            {
                input = MovementOrder.MovementOrderChargeToTarget(
                    __instance.QuerySystem.ClosestEnemyFormation.Formation);
            }

            return true;
        }
    }

    //[HarmonyPatch(typeof(BehaviorComponent))]
    //class OverrideFindBestBehavior
    //{
    //    [HarmonyPrefix]
    //    [HarmonyPatch("GetAIWeight")]
    //    static bool PrefixFindBestBehavior(ref BehaviorComponent __instance, ref float __result)
    //    {
    //        __instance.NavmeshlessTargetPositionPenalty = 1f;
    //        return true;
    //    }
    //}


    //[HarmonyPatch(typeof(FormationAI))]
    //class OverrideFindBestBehavior
    //{
    //    [HarmonyPrefix]
    //    [HarmonyPatch("FindBestBehavior")]
    //    static bool PrefixFindBestBehavior(FormationAI __instance, ref bool __result,
    //        ref List<BehaviorComponent> ____behaviors, ref Formation ____formation, ref BehaviorComponent ____activeBehavior)
    //    {
    //        BehaviorComponent behaviorComponent = null;
    //        float num = float.MinValue;
    //        foreach (BehaviorComponent behavior in ____behaviors)
    //        {
    //            if (!(behavior.WeightFactor > 1E-07f))
    //            {
    //                continue;
    //            }
    //            float num2 = behavior.GetAIWeight() * behavior.WeightFactor;
    //            if (behavior == __instance.ActiveBehavior)
    //            {
    //                num2 *= MBMath.Lerp(1.2f, 2f, MBMath.ClampFloat((behavior.PreserveExpireTime - Mission.Current.CurrentTime) / 5f, 0f, 1f), float.MinValue);
    //            }
    //            if (num2 > num)
    //            {
    //                if (behavior.NavmeshlessTargetPositionPenalty > 0f)
    //                {
    //                    num2 /= behavior.NavmeshlessTargetPositionPenalty;
    //                }
    //                behavior.PrecalculateMovementOrder();
    //                num2 *= behavior.NavmeshlessTargetPositionPenalty;
    //                if (num2 > num)
    //                {
    //                    behaviorComponent = behavior;
    //                    num = num2;
    //                }
    //            }
    //        }
    //        if (behaviorComponent != null)
    //        {
    //            typeof(FormationAI).GetProperty("ActiveBehavior").SetValue(__instance, behaviorComponent, null);
    //            if (behaviorComponent != ____behaviors[0])
    //            {
    //                ____behaviors.Remove(behaviorComponent);
    //                ____behaviors.Insert(0, behaviorComponent);
    //            }
    //            __result = true;
    //            return false;
    //        }
    //        __result = false;
    //        return false;
    //    }
    //}

    [HarmonyPatch(typeof(BehaviorRegroup))]
    internal class OverrideBehaviorRegroup
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetAiWeight")]
        private static bool PrefixGetAiWeight(ref BehaviorRegroup __instance, ref float __result)
        {
            if (__instance.Formation == null) 
                return true;

            var querySystem = __instance.Formation.QuerySystem;
            if (__instance.Formation.AI.ActiveBehavior == null || querySystem.IsRangedFormation)
            {
                __result = 0f;
                return false;
            }

            __result = MBMath.Lerp(0.1f, 1.2f,
                MBMath.ClampFloat(
                    __instance.BehaviorCoherence *
                    (querySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents + 1f) /
                    (querySystem.IdealAverageDisplacement + 1f), 0f, 3f) / 3f);

            return false;

        }

        [HarmonyPrefix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static bool PrefixCalculateCurrentOrder(ref BehaviorRegroup __instance,
            ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
        {
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation &&
                __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);
                if (significantEnemy != null)
                {
                    var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);

                    var direction =
                        (significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                         __instance.Formation.QuerySystem.AveragePosition).Normalized();
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);

                    return false;
                }
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("TickOccasionally")]
        private static void PrefixTickOccasionally(ref BehaviorRegroup __instance)
        {
            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
        }
    }

    [HarmonyPatch(typeof(BehaviorAdvance))]
    internal class OverrideBehaviorAdvance
    {
        public static Dictionary<Formation, WorldPosition>
            positionsStorage = new Dictionary<Formation, WorldPosition>();

        public static Dictionary<Formation, int> waitCountStorage = new Dictionary<Formation, int>();

        [HarmonyPrefix]
        [HarmonyPatch("CalculateCurrentOrder")]
        private static bool PrefixCalculateCurrentOrder(ref BehaviorAdvance __instance,
            ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
        {
            if (__instance?.Formation?.QuerySystem?.ClosestSignificantlyLargeEnemyFormation == null) 
                return true;

            var significantEnemy =
                Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

            if (__instance.Formation.QuerySystem.IsInfantryFormation && !Utilities.FormationFightingInMelee(__instance.Formation, 0.5f))
            {
                var _currentTacticField = typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
                var _ = _currentTacticField?.DeclaringType?.GetField("_currentTactic");
                if (__instance.Formation?.Team?.TeamAI != null &&
                    _currentTacticField?.GetValue(__instance.Formation?.Team?.TeamAI) != null &&
                    _currentTacticField.GetValue(__instance.Formation?.Team?.TeamAI).ToString().Contains("SplitArchers"))
                {
                    var allyArchers = Utilities.FindSignificantAlly(__instance.Formation, false, true, false, false, false);
                    if (allyArchers != null)
                    {
                        var dir = allyArchers.QuerySystem.MedianPosition.AsVec2 - __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                        var allyArchersDist = dir.Normalize();
                        if (allyArchersDist - allyArchers.Width / 2f - __instance.Formation?.Width / 2f > 60f)
                        {
                            ____currentOrder =
                                MovementOrder.MovementOrderMove(__instance.Formation.QuerySystem
                                    .MedianPosition);
                            return false;
                        }
                    }
                }

                var enemyCav = Utilities.FindSignificantEnemy(__instance.Formation, false, false, true, false, false);

                if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) 
                    enemyCav = null;

                var signDist = 1f;

                if (significantEnemy != null)
                {
                    var signDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                        __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                    signDist = signDirection.Normalize();
                }

                if (enemyCav != null)
                {
                    var cavDirection = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                       __instance.Formation.QuerySystem.MedianPosition.AsVec2;

                    var cavDist = cavDirection.Normalize();

                    if (cavDist <= signDist && enemyCav.CountOfUnits > __instance.Formation.CountOfUnits / 10 && signDist > 35f)
                    {
                        if (enemyCav.TargetFormation == __instance.Formation &&
                            (enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget ||
                             enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.Charge))
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                      __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                            positionsStorage.TryGetValue(__instance.Formation, out var storedPosition);

                            if (!storedPosition.IsValid)
                            {
                                positionsStorage.Add(__instance.Formation, positionNew);
                                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                            }
                            else
                            {
                                ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                            }

                            if (cavDist > 70f)
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());
                            return false;
                        }

                        positionsStorage.Remove(__instance.Formation);
                    }

                    else if (significantEnemy != null && signDist < 60f && Utilities.FormationActiveSkirmishersRatio(__instance.Formation, 0.33f))
                    {
                        var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                        positionsStorage.TryGetValue(__instance.Formation, out var storedPosition);

                        if (!storedPosition.IsValid)
                        {
                            positionsStorage.Add(__instance.Formation, positionNew);
                            ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                        }
                        else
                        {
                            ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                        }

                        return false;
                    }
                }

                
                positionsStorage.Remove(__instance.Formation);
            }

            if (significantEnemy != null)
            {
                var vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                          __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                var disper = __instance.Formation.QuerySystem.FormationDispersedness;

                if (disper > 10f)
                {
                    positionNew.SetVec2(positionNew.AsVec2 + vec.Normalized() * (20f + __instance.Formation.Depth));
                    if (!Mission.Current.IsPositionInsideBoundaries(positionNew.AsVec2) ||
                        positionNew.GetNavMesh() == UIntPtr.Zero)
                        positionNew.SetVec2(significantEnemy.CurrentPosition);
                }
                else
                {
                    positionNew.SetVec2(significantEnemy.CurrentPosition);
                }

                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());
                return false;
            }

            return true;

        }

        [HarmonyPrefix]
        [HarmonyPatch("TickOccasionally")]
        private static bool PrefixTickOccasionally(ref BehaviorAdvance __instance,
            ref FacingOrder ___CurrentFacingOrder)
        {
            var method = typeof(BehaviorAdvance).GetMethod("CalculateCurrentOrder",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var _ = method?.DeclaringType?.GetMethod("CalculateCurrentOrder");
            method?.Invoke(__instance, new object[] { });

            __instance.Formation.SetMovementOrder(__instance.CurrentOrder);
            __instance.Formation.FacingOrder = ___CurrentFacingOrder;
            if (__instance.Formation.IsInfantry())
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                if (significantEnemy != null)
                {
                    var num = __instance.Formation.QuerySystem.AveragePosition.Distance(
                        significantEnemy.QuerySystem.MedianPosition.AsVec2);

                    if (num < 150f)
                    {
                        if (significantEnemy.ArrangementOrder == ArrangementOrderLine)
                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                        else if (significantEnemy.ArrangementOrder == ArrangementOrderLoose)
                            __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
                    }
                }
            }

            __instance.Formation.SetMovementOrder(__instance.CurrentOrder);
            return false;
        }
    }
}