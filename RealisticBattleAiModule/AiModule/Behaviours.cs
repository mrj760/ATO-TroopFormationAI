using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.ArrangementOrder;
using static TaleWorlds.MountAndBlade.HumanAIComponent;

namespace RBMAI
{
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
                //__instance.Formation.FormOrder = FormOrder.FormOrderWide;
                __instance.Formation.FormOrder = FormOrder.FormOrderCustom(110f);
            }
        }

        [HarmonyPatch(typeof(BehaviorDefend))]
        private class OverrideBehaviorDefend
        {
            public static readonly Dictionary<Formation, WorldPosition> positionsStorage =
                new Dictionary<Formation, WorldPosition>();

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(ref BehaviorDefend __instance,
                ref MovementOrder ____currentOrder, ref bool ___IsCurrentOrderChanged,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (__instance.Formation != null &&
                    __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    var medianPositionNew = __instance.Formation.QuerySystem.MedianPosition;
                    medianPositionNew.SetVec2(__instance.Formation.QuerySystem.AveragePosition);

                    var significantEnemy =
                        Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                    if (significantEnemy != null)
                    {
                        var enemyDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                             __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                        var distance = enemyDirection.Normalize();
                        if (distance < 200f)
                        {
                            var newPosition = WorldPosition.Invalid;
                            positionsStorage.TryGetValue(__instance.Formation, out newPosition);
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
            }
        }

        [HarmonyPatch(typeof(BehaviorHoldHighGround))]
        private class OverrideBehaviorHoldHighGround
        {
            public static readonly Dictionary<Formation, WorldPosition> positionsStorage =
                new Dictionary<Formation, WorldPosition>();

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            private static void PostfixCalculateCurrentOrder(ref BehaviorHoldHighGround __instance,
                ref MovementOrder ____currentOrder, ref bool ___IsCurrentOrderChanged,
                ref FacingOrder ___CurrentFacingOrder)
            {
                if (__instance.Formation != null &&
                    __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    var medianPositionNew = __instance.Formation.QuerySystem.MedianPosition;
                    medianPositionNew.SetVec2(__instance.Formation.QuerySystem.AveragePosition);

                    var significantEnemy =
                        Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                    if (significantEnemy != null)
                    {
                        var enemyDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                             __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                        var distance = enemyDirection.Normalize();

                        if (distance < 200f)
                        {
                            var newPosition = WorldPosition.Invalid;
                            positionsStorage.TryGetValue(__instance.Formation, out newPosition);
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
                if (____mainFormation != null &&
                    (____mainFormation.CountOfUnits == 0 || !____mainFormation.IsInfantry()))
                    ____mainFormation = __instance.Formation.Team.Formations.FirstOrDefault(f => f.AI.IsMainFormation);
                if (____mainFormation != null && __instance.Formation != null && ____mainFormation.CountOfUnits > 0 &&
                    ____mainFormation.IsInfantry())
                {
                    ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(____mainFormation.Direction);
                    var medianPosition = ____mainFormation.QuerySystem.MedianPosition;
                    Vec2 calcPosition;
                    if (__instance.Formation.QuerySystem.IsRangedCavalryFormation)
                        calcPosition = medianPosition.AsVec2 - ____mainFormation.Direction.Normalized() *
                            (____mainFormation.Depth / 2f + __instance.Formation.Depth / 2f + 15f);
                    else
                        calcPosition = medianPosition.AsVec2 - ____mainFormation.Direction.Normalized() *
                            (____mainFormation.Depth / 2f + __instance.Formation.Depth / 2f + 5f);
                    medianPosition.SetVec2(calcPosition);
                    if (!Mission.Current.IsPositionInsideBoundaries(calcPosition) ||
                        medianPosition.GetNavMesh() == UIntPtr.Zero)
                        medianPosition = ____mainFormation.QuerySystem.MedianPosition;
                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("TickOccasionally")]
            private static bool PrefixTickOccasionally(Formation ____mainFormation, BehaviorScreenedSkirmish __instance,
                ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
            {
                var method = typeof(BehaviorScreenedSkirmish).GetMethod("CalculateCurrentOrder",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.DeclaringType.GetMethod("CalculateCurrentOrder");
                method.Invoke(__instance, new object[] { });
                //bool flag = formation.QuerySystem.ClosestEnemyFormation == null || _mainFormation.QuerySystem.MedianPosition.AsVec2.DistanceSquared(formation.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) <= formation.QuerySystem.AveragePosition.DistanceSquared(formation.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) || formation.QuerySystem.AveragePosition.DistanceSquared(position.AsVec2) <= (_mainFormation.Depth + formation.Depth) * (_mainFormation.Depth + formation.Depth) * 0.25f;
                //if (flag != _isFireAtWill)
                //{
                //    _isFireAtWill = flag;
                //    formation.FiringOrder = (_isFireAtWill ? FiringOrder.FiringOrderFireAtWill : FiringOrder.FiringOrderHoldYourFire);
                //}
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

            //[HarmonyPostfix]
            //[HarmonyPatch("GetAiWeight")]
            //static void PostfixGetAiWeight( ref float __result)
            //{
            //    __result.ToString();
            //}
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
            if (__instance.Formation != null && ____archerFormation != null &&
                __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false, false);

                if (significantEnemy != null)
                {
                    var waitCountShooting = 0;
                    var waitCountApproaching = 0;
                    if (!waitCountShootingStorage.TryGetValue(__instance.Formation, out waitCountShooting))
                        waitCountShootingStorage[__instance.Formation] = 0;
                    if (!waitCountApproachingStorage.TryGetValue(__instance.Formation, out waitCountApproaching))
                        waitCountApproachingStorage[__instance.Formation] = 0;

                    var vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                              __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                    var distance = vec.Normalize();

                    switch (____behaviorState)
                    {
                        case BehaviorState.Shooting:
                        {
                            if (waitCountShootingStorage[__instance.Formation] > 70)
                            {
                                if (distance > 100f)
                                {
                                    var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                    medianPosition.SetVec2(medianPosition.AsVec2 + vec * 5f);
                                    ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                }

                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                                waitCountShootingStorage[__instance.Formation] = 0;
                                waitCountApproachingStorage[__instance.Formation] = 0;
                            }
                            else
                            {
                                if (distance > 100f)
                                    waitCountShootingStorage[__instance.Formation] =
                                        waitCountShootingStorage[__instance.Formation] + 2;
                                else
                                    waitCountShootingStorage[__instance.Formation] =
                                        waitCountShootingStorage[__instance.Formation] + 1;
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                            }

                            break;
                        }
                        case BehaviorState.Approaching:
                        {
                            if (distance > 160f)
                            {
                                var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                medianPosition.SetVec2(medianPosition.AsVec2 + vec * 10f);
                                ____shootPosition = medianPosition.AsVec2 + vec * 10f;
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                            }
                            else
                            {
                                if (waitCountApproachingStorage[__instance.Formation] > 35)
                                {
                                    if (distance < 150f)
                                    {
                                        var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                        medianPosition.SetVec2(medianPosition.AsVec2 + vec * 5f);
                                        ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                        ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                    }

                                    waitCountApproachingStorage[__instance.Formation] = 0;
                                }
                                else
                                {
                                    if (distance < 150f)
                                    {
                                        var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                        medianPosition.SetVec2(____shootPosition);
                                        ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                    }

                                    waitCountApproachingStorage[__instance.Formation] =
                                        waitCountApproachingStorage[__instance.Formation] + 1;
                                }
                            }

                            break;
                        }
                        case BehaviorState.PullingBack:
                        {
                            if (waitCountApproachingStorage[__instance.Formation] > 30)
                            {
                                if (distance < 150f)
                                {
                                    var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                    medianPosition.SetVec2(medianPosition.AsVec2 - vec * 10f);
                                    ____shootPosition = medianPosition.AsVec2 + vec * 5f;
                                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                }

                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                                waitCountApproachingStorage[__instance.Formation] = 0;
                            }
                            else
                            {
                                if (distance < 150f)
                                {
                                    var medianPosition = __instance.Formation.QuerySystem.MedianPosition;
                                    medianPosition.SetVec2(____shootPosition);
                                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                                }

                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
                                waitCountApproachingStorage[__instance.Formation] =
                                    waitCountApproachingStorage[__instance.Formation] + 1;
                            }

                            break;
                        }
                    }
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
            ref MovementOrder ____currentOrder, ref bool ____isEnemyReachable, ref FacingOrder ___CurrentFacingOrder)
        {
            var position = __instance.Formation.QuerySystem.MedianPosition;
            var position2 = __instance.Formation.QuerySystem.MedianPosition;
            var targetFormation = Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);
            FormationQuerySystem targetFormationQS = null;
            if (targetFormation != null)
                targetFormationQS = targetFormation.QuerySystem;
            else
                targetFormationQS = __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            ____isEnemyReachable = targetFormationQS != null &&
                                   (!(__instance.Formation.Team.TeamAI is TeamAISiegeComponent) ||
                                    !TeamAISiegeComponent.IsFormationInsideCastle(targetFormationQS.Formation, false));
            if (!____isEnemyReachable)
            {
                position.SetVec2(__instance.Formation.QuerySystem.AveragePosition);
            }
            else
            {
                var num = (__instance.Formation.QuerySystem.AverageAllyPosition -
                           __instance.Formation.Team.QuerySystem.AverageEnemyPosition).LengthSquared <= 160000f;
                var engaging = ____engaging;
                engaging = ____engaging = num || (!____engaging
                    ? (__instance.Formation.QuerySystem.AveragePosition -
                       __instance.Formation.QuerySystem.AverageAllyPosition).LengthSquared <= 160000f
                    : !(__instance.Formation.QuerySystem.UnderRangedAttackRatio * 0.2f >
                        __instance.Formation.QuerySystem.MakingRangedAttackRatio));
                if (!____engaging)
                {
                    position = new WorldPosition(Mission.Current.Scene,
                        new Vec3(__instance.Formation.QuerySystem.AverageAllyPosition,
                            __instance.Formation.Team.QuerySystem.MedianPosition.GetNavMeshZ() + 100f));
                }
                else
                {
                    var vec = (__instance.Formation.QuerySystem.MedianPosition.AsVec2 -
                               targetFormationQS.MedianPosition.AsVec2).Normalized().LeftVec();
                    var closestSignificantlyLargeEnemyFormation =
                        __instance.Formation.QuerySystem.ClosestEnemyFormation;
                    var num2 = 50f + (targetFormationQS.Formation.Width + __instance.Formation.Depth) * 0.5f;
                    var num3 = 0f;

                    var enemyFormation = targetFormationQS.Formation;

                    if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation)
                        enemyFormation = Utilities.FindSignificantEnemyToPosition(__instance.Formation, position, true,
                            true, false, false, false, false);

                    if (closestSignificantlyLargeEnemyFormation != null &&
                        closestSignificantlyLargeEnemyFormation.AveragePosition.Distance(__instance.Formation
                            .CurrentPosition) < __instance.Formation.Depth / 2f + (
                            closestSignificantlyLargeEnemyFormation.Formation.QuerySystem.FormationPower /
                            __instance.Formation.QuerySystem.FormationPower * 20f + 10f))
                    {
                        ____currentOrder =
                            MovementOrder.MovementOrderChargeToTarget(closestSignificantlyLargeEnemyFormation
                                .Formation);
                        return;
                    }

                    //foreach (Team team in Mission.Current.Teams.ToList())
                    //{
                    //    if (!team.IsEnemyOf(__instance.Formation.Team))
                    //    {
                    //        continue;
                    //    }
                    //    foreach (Formation formation2 in team.FormationsIncludingSpecialAndEmpty.ToList())
                    //    {
                    //        if (formation2.CountOfUnits > 0 && formation2.QuerySystem != closestSignificantlyLargeEnemyFormation)
                    //        {
                    //            Vec2 v = formation2.QuerySystem.AveragePosition - closestSignificantlyLargeEnemyFormation.AveragePosition;
                    //            float num4 = v.Normalize();
                    //            if (vec.DotProduct(v) > 0.8f && num4 < num2 && num4 > num3)
                    //            {
                    //                num3 = num4;
                    //                enemyFormation = formation2;
                    //            }
                    //        }
                    //    }
                    //}

                    //if (__instance.Formation.QuerySystem.RangedCavalryUnitRatio > 0.95f && targetFormationQS.Formation == enemyFormation)
                    //{
                    //    ____currentOrder = MovementOrder.MovementOrderCharge;
                    //    return;
                    //}

                    if (enemyFormation != null && enemyFormation.QuerySystem != null)
                    {
                        var isEnemyCav = enemyFormation.QuerySystem.IsCavalryFormation ||
                                         enemyFormation.QuerySystem.IsRangedCavalryFormation;
                        var distance = 60f;
                        if (!__instance.Formation.QuerySystem.IsRangedCavalryFormation) distance = 30f;

                        RotationChangeClass rotationDirection;
                        if (!rotationDirectionDictionary.TryGetValue(__instance.Formation, out rotationDirection))
                        {
                            rotationDirection = new RotationChangeClass();
                            rotationDirectionDictionary.Add(__instance.Formation, rotationDirection);
                        }

                        if (__instance.Formation.QuerySystem.IsRangedCavalryFormation)
                        {
                            var ellipse = new Ellipse(enemyFormation.QuerySystem.MedianPosition.AsVec2, distance,
                                enemyFormation.ArrangementOrder == ArrangementOrderLoose
                                    ? enemyFormation.Width * 0.25f
                                    : enemyFormation.Width * 0.5f, enemyFormation.Direction);
                            position.SetVec2(ellipse.GetTargetPos(__instance.Formation.SmoothedAverageUnitPosition, 25f,
                                rotationDirection.rotationDirection));
                        }
                        else
                        {
                            var ellipse = new Ellipse(enemyFormation.QuerySystem.MedianPosition.AsVec2, distance,
                                enemyFormation.Width * 0.5f, enemyFormation.Direction);
                            position.SetVec2(ellipse.GetTargetPos(__instance.Formation.SmoothedAverageUnitPosition, 25f,
                                rotationDirection.rotationDirection));
                        }

                        if (rotationDirection.waitbeforeChangeCooldownCurrent > 0)
                        {
                            if (rotationDirection.waitbeforeChangeCooldownCurrent >
                                rotationDirection.waitbeforeChangeCooldownMax)
                            {
                                rotationDirection.waitbeforeChangeCooldownCurrent = 0;
                                rotationDirectionDictionary[__instance.Formation] = rotationDirection;
                            }
                            else
                            {
                                rotationDirection.waitbeforeChangeCooldownCurrent++;
                                rotationDirectionDictionary[__instance.Formation] = rotationDirection;
                            }

                            position.SetVec2(enemyFormation.CurrentPosition + enemyFormation.Direction.Normalized() *
                                (__instance.Formation.Depth / 2f + enemyFormation.Depth / 2f + 50f));
                            if (position.GetNavMesh() == UIntPtr.Zero ||
                                !Mission.Current.IsPositionInsideBoundaries(position.AsVec2))
                                position.SetVec2(enemyFormation.CurrentPosition +
                                                 enemyFormation.Direction.Normalized() *
                                                 -(__instance.Formation.Depth / 2f + enemyFormation.Depth / 2f + 50f));
                        }

                        var distanceFromBoudnary = Mission.Current
                            .GetClosestBoundaryPosition(__instance.Formation.CurrentPosition)
                            .Distance(__instance.Formation.CurrentPosition);
                        if (distanceFromBoudnary <= __instance.Formation.Width / 2f)
                        {
                            if (rotationDirection.waitbeforeChangeCooldownCurrent >
                                rotationDirection.waitbeforeChangeCooldownMax)
                            {
                                rotationDirection.waitbeforeChangeCooldownCurrent = 0;
                                rotationDirectionDictionary[__instance.Formation] = rotationDirection;
                            }
                            else
                            {
                                rotationDirection.waitbeforeChangeCooldownCurrent++;
                                rotationDirectionDictionary[__instance.Formation] = rotationDirection;
                            }
                        }
                    }
                    else
                    {
                        position.SetVec2(__instance.Formation.QuerySystem.AveragePosition);
                    }
                }
            }

            if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(position.AsVec2))
            {
                position = __instance.Formation.QuerySystem.MedianPosition;
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
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsCavalryFormation)
            {
                if (Utilities.CheckIfMountedSkirmishFormation(__instance.Formation, 0.6f))
                {
                    __result = 5f;
                    return;
                }

                __result = 0f;
                return;
            }

            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsRangedCavalryFormation)
            {
                var enemyCav = Utilities.FindSignificantEnemy(__instance.Formation, false, false, true, false, false);
                if (enemyCav != null && enemyCav.QuerySystem.IsCavalryFormation &&
                    __instance.Formation.QuerySystem.MedianPosition.AsVec2.Distance(enemyCav.QuerySystem.MedianPosition
                        .AsVec2) < 55f && enemyCav.CountOfUnits >= __instance.Formation.CountOfUnits * 0.5f)
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
                if (!Utilities.HasBattleBeenJoined(__instance.Formation, false))
                {
                    foreach (var enemyArcherFormation in
                             Utilities.FindSignificantArcherFormations(__instance.Formation))
                        powerSum += enemyArcherFormation.QuerySystem.FormationPower;
                    if (powerSum > 0f && __instance.Formation.QuerySystem.FormationPower > 0f &&
                        __instance.Formation.QuerySystem.FormationPower / powerSum < 0.75f)
                    {
                        __result = 0.01f;
                        return;
                    }
                }

                __result = 100f;
                return;
            }

            var countOfSkirmishers = 0;
            __instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                if (Utilities.CheckIfSkirmisherAgent(agent, 1)) countOfSkirmishers++;
            });
            if (countOfSkirmishers / __instance.Formation.CountOfUnits > 0.6f)
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

            private readonly Vec2 _direction;

            public Ellipse(Vec2 center, float radius, float halfLength, Vec2 direction)
            {
                _center = center;
                _radius = radius;
                _halfLength = halfLength;
                _direction = direction;
            }

            public Vec2 GetTargetPos(Vec2 position, float distance, RotationDirection rotationDirection)
            {
                Vec2 vec;
                if (rotationDirection == RotationDirection.Left)
                    vec = _direction.LeftVec();
                else
                    vec = _direction.RightVec();
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
                    if (flag2 && flag)
                    {
                        var num2 = (vec2 - vec7).Length < distance ? (vec2 - vec7).Length : distance;
                        position = vec7 + (vec2 - vec7).Normalized() * num2;
                        position += _direction * _radius;
                        distance -= num2;
                        flag2 = false;
                        flag3 = true;
                    }
                    else if (!flag2 && flag3)
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
                    }
                    else if (flag2)
                    {
                        var num7 = (vec3 - vec7).Length < distance ? (vec3 - vec7).Length : distance;
                        position = vec7 + (vec3 - vec7).Normalized() * num7;
                        position -= _direction * _radius;
                        distance -= num7;
                        flag2 = false;
                        flag3 = false;
                    }
                    else
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
            ref MovementOrder ____currentOrder, ref MovementOrder ____chargeToTargetOrder,
            ref MovementOrder ____movementOrder, ref BehaviorState ____protectFlankState,
            ref Formation ____mainFormation, ref FormationAI.BehaviorSide ____behaviorSide)
        {
            var position = __instance.Formation.QuerySystem.MedianPosition;
            var averagePosition = __instance.Formation.QuerySystem.AveragePosition;

            var distanceFromMainFormation = 90f;
            var closerDistanceFromMainFormation = 30f;
            var distanceOffsetFromMainFormation = 55f;

            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation)
            {
                distanceFromMainFormation = 30f;
                closerDistanceFromMainFormation = 10f;
                distanceOffsetFromMainFormation = 30f;
            }

            if (____mainFormation == null || __instance.Formation == null ||
                __instance.Formation.QuerySystem.ClosestEnemyFormation == null)
            {
                ____currentOrder = MovementOrder.MovementOrderStop;
                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
            else if (____protectFlankState == BehaviorState.HoldingFlank ||
                     ____protectFlankState == BehaviorState.Returning)
            {
                var direction = ____mainFormation.Direction;
                var v = (__instance.Formation.QuerySystem.Team.MedianTargetFormationPosition.AsVec2 -
                         ____mainFormation.QuerySystem.MedianPosition.AsVec2).Normalized();
                Vec2 vec;
                if (____behaviorSide == FormationAI.BehaviorSide.Right ||
                    ___FlankSide == FormationAI.BehaviorSide.Right)
                {
                    vec = ____mainFormation.CurrentPosition + v.RightVec().Normalized() *
                        (____mainFormation.Width / 2f + __instance.Formation.Width / 2f + distanceFromMainFormation);
                    vec -= v * (____mainFormation.Depth + __instance.Formation.Depth);
                    vec += ____mainFormation.Direction * (____mainFormation.Depth / 2f +
                                                          __instance.Formation.Depth / 2f +
                                                          distanceOffsetFromMainFormation);
                    position.SetVec2(vec);
                    if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec) ||
                        __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.1f)
                    {
                        vec = ____mainFormation.CurrentPosition + v.RightVec().Normalized() *
                            (____mainFormation.Width / 2f + __instance.Formation.Width / 2f +
                             closerDistanceFromMainFormation);
                        vec -= v * (____mainFormation.Depth + __instance.Formation.Depth);
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
                else if (____behaviorSide == FormationAI.BehaviorSide.Left ||
                         ___FlankSide == FormationAI.BehaviorSide.Left)
                {
                    vec = ____mainFormation.CurrentPosition + v.LeftVec().Normalized() * (____mainFormation.Width / 2f +
                        __instance.Formation.Width / 2f + distanceFromMainFormation);
                    vec -= v * (____mainFormation.Depth + __instance.Formation.Depth);
                    vec += ____mainFormation.Direction * (____mainFormation.Depth / 2f +
                                                          __instance.Formation.Depth / 2f +
                                                          distanceOffsetFromMainFormation);
                    position.SetVec2(vec);
                    if (position.GetNavMesh() == UIntPtr.Zero || !Mission.Current.IsPositionInsideBoundaries(vec) ||
                        __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.1f)
                    {
                        vec = ____mainFormation.CurrentPosition + v.LeftVec().Normalized() *
                            (____mainFormation.Width / 2f + __instance.Formation.Width / 2f +
                             closerDistanceFromMainFormation);
                        vec -= v * (____mainFormation.Depth + __instance.Formation.Depth);
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
                    vec = ____mainFormation.CurrentPosition +
                          v * ((____mainFormation.Depth + __instance.Formation.Depth) * 0.5f + 10f);
                    position.SetVec2(vec);
                }

                //WorldPosition medianPosition = ____mainFormation.QuerySystem.MedianPosition;
                //medianPosition.SetVec2(vec);
                ____movementOrder = MovementOrder.MovementOrderMove(position);
                ____currentOrder = ____movementOrder;
                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
            }

            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch("CheckAndChangeState")]
        private static bool PrefixCheckAndChangeState(ref BehaviorProtectFlank __instance,
            ref FormationAI.BehaviorSide ___FlankSide, ref FacingOrder ___CurrentFacingOrder,
            ref MovementOrder ____currentOrder, ref MovementOrder ____chargeToTargetOrder,
            ref MovementOrder ____movementOrder, ref BehaviorState ____protectFlankState,
            ref Formation ____mainFormation, ref FormationAI.BehaviorSide ____behaviorSide)
        {
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsInfantryFormation)
            {
                if (__instance.Formation != null && ____movementOrder != null)
                {
                    var position = ____movementOrder.GetPosition(__instance.Formation);
                    switch (____protectFlankState)
                    {
                        case BehaviorState.HoldingFlank:
                        {
                            var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;
                            if (closestFormation != null && closestFormation.Formation != null &&
                                (closestFormation.Formation.QuerySystem.IsInfantryFormation ||
                                 closestFormation.Formation.QuerySystem.IsRangedFormation ||
                                 closestFormation.Formation.QuerySystem.IsCavalryFormation))
                            {
                                //float changeToChargeDistance = 30f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                                //if (closestFormation.Formation.QuerySystem.MedianPosition.AsVec2.DistanceSquared(position) < changeToChargeDistance * changeToChargeDistance)
                                //{
                                //    ____chargeToTargetOrder = MovementOrder.MovementOrderChargeToTarget(closestFormation.Formation);
                                //    ____currentOrder = ____chargeToTargetOrder;
                                //    ____protectFlankState = BehaviorState.Charging;
                                //}
                            }

                            break;
                        }
                        case BehaviorState.Charging:
                        {
                            var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;
                            if (closestFormation != null && closestFormation.Formation != null)
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
                        }
                        case BehaviorState.Returning:
                            if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) < 400f)
                                ____protectFlankState = BehaviorState.HoldingFlank;
                            break;
                    }

                    return false;
                }
            }
            else
            {
                if (__instance.Formation != null && ____movementOrder != null)
                {
                    var position = ____movementOrder.GetPosition(__instance.Formation);
                    switch (____protectFlankState)
                    {
                        case BehaviorState.HoldingFlank:
                        {
                            var closestFormation = __instance.Formation.QuerySystem.ClosestEnemyFormation;
                            if (closestFormation != null && closestFormation.Formation != null &&
                                (closestFormation.Formation.QuerySystem.IsCavalryFormation ||
                                 closestFormation.Formation.QuerySystem.IsRangedCavalryFormation))
                            {
                                var changeToChargeDistance =
                                    110f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                                if (closestFormation.Formation.QuerySystem.MedianPosition.AsVec2.Distance(position) <
                                    changeToChargeDistance ||
                                    __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.1f)
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
                            if (closestFormation != null && closestFormation.Formation != null)
                            {
                                if (closestFormation == null)
                                {
                                    ____currentOrder = ____movementOrder;
                                    ____protectFlankState = BehaviorState.Returning;
                                    break;
                                }

                                var returnDistance =
                                    80f + (__instance.Formation.Depth + closestFormation.Formation.Depth) / 2f;
                                if (__instance.Formation.QuerySystem.AveragePosition.DistanceSquared(position) >
                                    returnDistance * returnDistance)
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

            return true;
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
        private static bool AdjustSpeedLimitPrefix(ref HumanAIComponent __instance, ref Agent agent,
            ref float desiredSpeed, ref bool limitIsMultiplier, ref Agent ___Agent)
        {
            //FieldInfo _currentTacticField = typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
            //_currentTacticField.DeclaringType.GetField("_currentTactic");
            //if (agent.Formation != null && agent.Formation.QuerySystem.IsCavalryFormation && _currentTacticField.GetValue(agent.Formation?.Team?.TeamAI) != null && _currentTacticField.GetValue(agent.Formation?.Team?.TeamAI).ToString().Contains("Embolon"))
            //{
            //    if (limitIsMultiplier && desiredSpeed < 0.6f)
            //    {
            //        desiredSpeed = 0.6f;
            //    }
            //    return true;
            //}
            //if(agent != null && agent.Formation != null)
            //{
            //    float currentTime = MBCommon.GetTotalMissionTime();
            //    if (agent.Formation.QuerySystem.IsInfantryFormation)
            //    {
            //        float lastMeleeAttackTime = agent.LastMeleeAttackTime;
            //        float lastMeleeHitTime = agent.LastMeleeHitTime;
            //        if ((currentTime - lastMeleeAttackTime < 4f) || (currentTime - lastMeleeHitTime < 4f))
            //        {
            //            if(desiredSpeed > 0.65f)
            //            {
            //                desiredSpeed = 0.65f;
            //            }
            //            return true;
            //        }
            //    }
            //}
            if (agent.Formation != null && (agent.Formation.QuerySystem.IsRangedCavalryFormation ||
                                            agent.Formation.QuerySystem.IsCavalryFormation))
            {
                if (agent.MountAgent != null)
                {
                    var speed = agent.MountAgent.AgentDrivenProperties.MountSpeed;
                    ___Agent.SetMaximumSpeedLimit(speed, false);
                    agent.MountAgent.SetMaximumSpeedLimit(speed, false);
                    return false;
                }
            }
            else if (agent.Formation != null && agent.Formation.AI != null &&
                     agent.Formation.AI.ActiveBehavior != null &&
                     (agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorForwardSkirmish) ||
                      agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorInfantryAttackFlank)))
            {
                if (limitIsMultiplier && desiredSpeed < 0.85f) desiredSpeed = 0.85f;
                //___Agent.SetMaximumSpeedLimit(100f, false);
            }

            if (agent.Formation != null && agent.Formation.AI != null && agent.Formation.AI.ActiveBehavior != null &&
                agent.Formation.AI.ActiveBehavior.GetType() == typeof(BehaviorProtectFlank))
                if (desiredSpeed < 0.85f)
                {
                    limitIsMultiplier = true;
                    desiredSpeed = 0.85f;
                }

            //___Agent.SetMaximumSpeedLimit(100f, false);
            if (agent.Formation != null && agent.Formation.AI != null && agent.Formation.AI.ActiveBehavior != null &&
                agent.Formation.AI.ActiveBehavior.GetType() == typeof(BehaviorRegroup))
                if (limitIsMultiplier && desiredSpeed < 0.95f)
                    desiredSpeed = 0.95f;
            //___Agent.SetMaximumSpeedLimit(100f, false);
            if (agent.Formation != null && agent.Formation.AI != null && agent.Formation.AI.ActiveBehavior != null &&
                agent.Formation.AI.ActiveBehavior.GetType() == typeof(BehaviorCharge))
                if (limitIsMultiplier && desiredSpeed < 0.85f)
                    desiredSpeed = 0.85f;
            if (agent.Formation != null && agent.Formation.AI != null && agent.Formation.AI.ActiveBehavior != null &&
                agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorArcherFlank))
                if (limitIsMultiplier && desiredSpeed < 0.9f)
                    desiredSpeed = 0.9f;
            if (agent.Formation != null && agent.Formation.AI != null && agent.Formation.AI.ActiveBehavior != null &&
                agent.Formation.AI.ActiveBehavior.GetType() == typeof(RBMBehaviorArcherSkirmish))
                if (limitIsMultiplier && desiredSpeed < 0.9f)
                    desiredSpeed = 0.9f;
            //else if(agent.Formation != null && agent.Formation.Team.HasTeamAi)
            //{
            //    FieldInfo field = typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
            //    field.DeclaringType.GetField("_currentTactic");
            //    TacticComponent currentTactic = (TacticComponent)field.GetValue(agent.Formation.Team.TeamAI);

            //    if(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget)
            //    {
            //        if (currentTactic != null && currentTactic.GetType() != null && (currentTactic.GetType() == typeof(RBMTacticAttackSplitInfantry) || currentTactic.GetType() == typeof(RBMTacticAttackSplitInfantry)))
            //        {
            //            if (limitIsMultiplier && desiredSpeed < 0.8f)
            //            {
            //                desiredSpeed = 0.8f;
            //            }
            //        }
            //    }

            //}
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
            var method = typeof(BehaviorVanguard).GetMethod("CalculateCurrentOrder",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.DeclaringType.GetMethod("CalculateCurrentOrder");
            method.Invoke(__instance, new object[] { });

            __instance.Formation.SetMovementOrder(____currentOrder);
            __instance.Formation.FacingOrder = ___CurrentFacingOrder;
            if (__instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null &&
                __instance.Formation.QuerySystem.AveragePosition.DistanceSquared(__instance.Formation.QuerySystem
                    .ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2) > 1600f &&
                __instance.Formation.QuerySystem.UnderRangedAttackRatio > 0.2f -
                (__instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Loose ? 0.1f : 0f))
                __instance.Formation.ArrangementOrder = ArrangementOrderSkein;
            else
                __instance.Formation.ArrangementOrder = ArrangementOrderSkein;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnBehaviorActivatedAux")]
        private static void PostfixOnBehaviorActivatedAux(ref MovementOrder ____currentOrder,
            ref FacingOrder ___CurrentFacingOrder, BehaviorVanguard __instance)
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
            if (__instance.Formation != null &&
                !(__instance.Formation.Team.IsPlayerTeam || __instance.Formation.Team.IsPlayerAlly) &&
                Campaign.Current != null && MobileParty.MainParty != null && MobileParty.MainParty.MapEvent != null)
            {
                var defenderName = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender).Name;
                var attackerName = MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker).Name;
                if (defenderName.Contains("Looter") || defenderName.Contains("Bandit") ||
                    defenderName.Contains("Raider") || attackerName.Contains("Looter") ||
                    attackerName.Contains("Bandit") || attackerName.Contains("Raider")) return true;
            }

            if (__instance.Formation != null &&
                (__instance.Formation.QuerySystem.IsInfantryFormation ||
                 __instance.Formation.QuerySystem.IsRangedFormation) &&
                __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle &&
                    __instance.Formation.QuerySystem.IsInfantryFormation &&
                    !Utilities.FormationFightingInMelee(__instance.Formation, 0.5f))
                {
                    var enemyCav =
                        Utilities.FindSignificantEnemy(__instance.Formation, false, false, true, false, false);

                    if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) enemyCav = null;

                    var cavDist = 0f;
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
                        cavDist = cavDirection.Normalize();
                    }

                    var isOnlyCavReamining = Utilities.CheckIfOnlyCavRemaining(__instance.Formation);
                    if (enemyCav != null && cavDist <= signDist &&
                        enemyCav.CountOfUnits > __instance.Formation.CountOfUnits / 10 &&
                        (signDist > 35f || significantEnemy == enemyCav || isOnlyCavReamining))
                    {
                        if (isOnlyCavReamining)
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                      __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                            var storedPosition = WorldPosition.Invalid;
                            positionsStorage.TryGetValue(__instance.Formation, out storedPosition);

                            if (!storedPosition.IsValid)
                            {
                                positionsStorage.Add(__instance.Formation, positionNew);
                                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                            }
                            else
                            {
                                var storedPositonDistance =
                                    (storedPosition.AsVec2 - __instance.Formation.QuerySystem.MedianPosition.AsVec2)
                                    .Normalize();
                                if (storedPositonDistance > __instance.Formation.Depth / 2f + 10f)
                                {
                                    positionsStorage.Remove(__instance.Formation);
                                    positionsStorage.Add(__instance.Formation, positionNew);
                                    ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                                }
                                else
                                {
                                    ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                                }
                            }

                            if (cavDist > 85f)
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());
                            //__instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent) {
                            //    if (RBMAI.Utilities.CheckIfCanBrace(agent))
                            //    {
                            //        agent.SetFiringOrder(1);
                            //    }
                            //    else
                            //    {
                            //        agent.SetFiringOrder(0);
                            //    }
                            //});
                            //if (cavDist > 150f)
                            //{
                            //    positionsStorage.Remove(__instance.Formation);
                            //}
                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                            return false;
                        }

                        if (!(__instance.Formation.AI?.Side == FormationAI.BehaviorSide.Left ||
                              __instance.Formation.AI?.Side == FormationAI.BehaviorSide.Right) &&
                            enemyCav.TargetFormation == __instance.Formation)
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                      __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                            var storedPosition = WorldPosition.Invalid;
                            positionsStorage.TryGetValue(__instance.Formation, out storedPosition);

                            if (!storedPosition.IsValid)
                            {
                                positionsStorage.Add(__instance.Formation, positionNew);
                                ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                            }
                            else
                            {
                                var storedPositonDistance =
                                    (storedPosition.AsVec2 - __instance.Formation.QuerySystem.MedianPosition.AsVec2)
                                    .Normalize();
                                if (storedPositonDistance > __instance.Formation.Depth / 2f + 10f)
                                {
                                    positionsStorage.Remove(__instance.Formation);
                                    positionsStorage.Add(__instance.Formation, positionNew);
                                    ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                                }
                                else
                                {
                                    ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                                }
                            }

                            if (cavDist > 85f)
                                ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec.Normalized());
                            //__instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent) {
                            //    if (RBMAI.Utilities.CheckIfCanBrace(agent))
                            //    {
                            //        agent.SetFiringOrder(1);
                            //    }
                            //    else
                            //    {
                            //        agent.SetFiringOrder(0);
                            //    }
                            //});
                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                            return false;
                        }

                        positionsStorage.Remove(__instance.Formation);
                    }
                    else if (significantEnemy != null && !significantEnemy.QuerySystem.IsRangedFormation &&
                             signDist < 50f && Utilities.FormationActiveSkirmishersRatio(__instance.Formation, 0.38f))
                    {
                        var positionNew = __instance.Formation.QuerySystem.MedianPosition;
                        positionNew.SetVec2(positionNew.AsVec2 - __instance.Formation.Direction * 7f);

                        var storedPosition = WorldPosition.Invalid;
                        positionsStorage.TryGetValue(__instance.Formation, out storedPosition);

                        if (!storedPosition.IsValid)
                        {
                            positionsStorage.Add(__instance.Formation, positionNew);
                            ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                        }
                        else
                        {
                            ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                        }

                        __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                        return false;
                        //__instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent) {
                        //    agent.SetMaximumSpeedLimit(0.1f, true);
                        //});
                    }

                    positionsStorage.Remove(__instance.Formation);
                }

                if (significantEnemy != null)
                {
                    __instance.Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                    ____currentOrder = MovementOrder.MovementOrderChargeToTarget(significantEnemy);
                    if (__instance.Formation.TargetFormation != null && __instance.Formation.TargetFormation
                                                                         .ArrangementOrder == ArrangementOrderShieldWall
                                                                     && Utilities.ShouldFormationCopyShieldWall(
                                                                         __instance.Formation))
                        __instance.Formation.ArrangementOrder = ArrangementOrderShieldWall;
                    else if (__instance.Formation.TargetFormation != null &&
                             __instance.Formation.TargetFormation.ArrangementOrder == ArrangementOrderLine)
                        __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                    else if (__instance.Formation.TargetFormation != null &&
                             __instance.Formation.TargetFormation.ArrangementOrder == ArrangementOrderLoose)
                        __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
                    return false;
                }
            }

            if (__instance.Formation != null &&
                __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                __instance.Formation.TargetFormation = __instance.Formation.QuerySystem
                    .ClosestSignificantlyLargeEnemyFormation.Formation;
            ____currentOrder = MovementOrder.MovementOrderCharge;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetAiWeight")]
        private static void PrefixGetAiWeight(ref BehaviorCharge __instance, ref float __result)
        {
            if (__instance.Formation != null && __instance.Formation.QuerySystem.IsRangedCavalryFormation)
                __result = __result * 0.2f;
            //__result = __result;
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
            if (unit != null && unit.Formation != null)
            {
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

                    //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 1f, 2f, 4f, 20f, 1f);
                    //unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 25f, 5f, 30f, 5f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 1f, 7f, 4f, 20f, 6f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0f, 10f, 3f, 20f, 6f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                    return false;
                }

                if (unit.Formation.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget)
                {
                    if (unit.Formation.QuerySystem.IsInfantryFormation)
                    {
                        //podmienky: twohandedpolearm v rukach
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 0f, 40f, 4f, 50f, 6f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 7f, 1f, 10f, 0.01f);
                        //if (RBMAI.Utilities.CheckIfTwoHandedPolearmInfantry(unit))
                        //{
                        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 3.5f, 5f, 20f, 6f);
                        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 3.5f, 4f, 20f, 0.01f);
                        //}
                        //else {
                        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 4f, 10f, 6f);
                        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 1f, 10f, 0.01f);
                        //}

                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 5f, 20f, 6f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 1f, 20f, 1f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 40f, 4f, 60f, 0f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 5f, 7f, 10f, 8, 20f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 0f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);

                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 4f, 10f, 6f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 1f, 10f, 0.01f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 7f, 0.8f, 20f, 20f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 40f, 4f, 60f, 0f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 5f, 7f, 10f, 8, 20f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 0f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                        return false;
                    }

                    if (unit.Formation.QuerySystem.IsRangedFormation)
                    {
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 0f, 40f, 4f, 50f, 6f);
                        //unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 7f, 1f, 10f, 0.01f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 4f, 2f, 4f, 10f, 6f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 5.5f, 2f, 4f, 10f, 0.01f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 2f, 0f, 8f, 20f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 5f, 40f, 4f, 60f, 0f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 0f);
                        unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                        return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetFollowBehaviorValues")]
        private static bool PrefixSetFollowBehaviorValues(Agent unit)
        {
            if (unit.Formation != null)
                if (unit.Formation.QuerySystem.IsRangedCavalryFormation)
                {
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 5f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 0.55f, 2f, 4f, 20f, 0.55f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.55f, 7f, 0.55f, 20f, 0.55f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 8f, 2f, 0.55f, 30f, 0.55f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 10f, 15f, 0.065f, 30f, 0.065f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
                    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
                    return false;
                }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SetDefaultMoveBehaviorValues")]
        private static bool PrefixSetDefaultMoveBehaviorValues(Agent unit)
        {
            if (unit.Formation != null)
                if (unit.Formation.QuerySystem.IsRangedCavalryFormation)
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

            if (unit.Formation != null)
            {
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

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetSubstituteOrder")]
        private static bool PrefixGetSubstituteOrder(MovementOrder __instance, ref MovementOrder __result,
            Formation formation)
        {
            if (formation != null &&
                (formation.QuerySystem.IsInfantryFormation || formation.QuerySystem.IsRangedFormation) &&
                __instance.OrderType == OrderType.ChargeWithTarget)
            {
                if (formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                    __result = MovementOrder.MovementOrderChargeToTarget(formation.QuerySystem
                        .ClosestSignificantlyLargeEnemyFormation.Formation);
                else
                    __result = MovementOrder.MovementOrderCharge;
                //var position = formation.QuerySystem.MedianPosition;
                //position.SetVec2(formation.CurrentPosition);
                //__result = MovementOrder.MovementOrderMove(position);
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetPositionAux")]
        private static void GetPositionAuxPostfix(ref MovementOrder __instance, ref WorldPosition __result,
            ref Formation f, ref WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache)
        {
            if (__instance.OrderEnum == MovementOrder.MovementOrderEnum.FallBack)
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

            if (__instance.OrderEnum == MovementOrder.MovementOrderEnum.Advance)
            {
                var enemyFormation = Utilities.FindSignificantEnemy(f, true, true, false, false, false);
                var querySystem = f.QuerySystem;
                FormationQuerySystem enemyQuerySystem;
                if (enemyFormation != null)
                    enemyQuerySystem = enemyFormation.QuerySystem;
                else
                    enemyQuerySystem = querySystem.ClosestEnemyFormation;
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
                    if (!(newPosition.AsVec2.DistanceSquared(querySystem.AveragePosition) >
                          effectiveMissileRange * effectiveMissileRange))
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
                        var tempPos = WorldPosition.Invalid;
                        if (positionsStorage.TryGetValue(f, out tempPos))
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
                if (enemyQuerySystem.FormationPower < f.QuerySystem.FormationPower * 0.2f) num = 0.1f;
                newPosition.SetVec2(newPosition.AsVec2 - vec * num);

                if (distance > 7f)
                {
                    positionsStorage[f] = newPosition;
                    __result = newPosition;
                }
                else
                {
                    __instance = MovementOrder.MovementOrderChargeToTarget(enemyFormation);
                    var tempPos = WorldPosition.Invalid;
                    if (positionsStorage.TryGetValue(f, out tempPos))
                    {
                        __result = tempPos;
                        return;
                    }

                    __result = oldPosition;
                }
            }
        }

        internal enum MovementOrderEnum
        {
            Invalid,
            Attach,
            AttackEntity,
            Charge,
            ChargeToTarget,
            Follow,
            FollowEntity,
            Guard,
            Move,
            Retreat,
            Stop,
            Advance,
            FallBack
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("GetPosition")]
        //static bool PrefixGetPosition(Formation f, ref WorldPosition __result)
        //{
        //    if(f == null)
        //    {
        //        __result = WorldPosition.Invalid;
        //        return false;
        //    }
        //    else
        //    {
        //        InformationManager.DisplayMessage(new InformationMessage(f.Team.IsAttacker + " " + f.AI.Side.ToString() + " " + f.PrimaryClass.GetName()));
        //        return true;
        //    }
        //}
    }

    [HarmonyPatch(typeof(Agent))]
    internal class OverrideAgent
    {
        //[HarmonyPrefix]
        //[HarmonyPatch("GetTargetAgent")]
        //static bool PrefixGetTargetAgent(ref Agent __instance, ref Agent __result)
        //{
        //    List<Formation> formations;
        //    if (__instance != null)
        //    {
        //        Formation formation = __instance.Formation;
        //        if (formation != null)
        //        {
        //            if ((formation.QuerySystem.IsInfantryFormation || formation.QuerySystem.IsRangedFormation) && (formation.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget))
        //            {
        //                formations = RBMAI.Utilities.FindSignificantFormations(formation);
        //                if (formations.Count > 0)
        //                {
        //                    __result = RBMAI.Utilities.NearestAgentFromMultipleFormations(__instance.Position.AsVec2, formations);
        //                    return false;
        //                }
        //                //Formation enemyFormation = formation.MovementOrder.TargetFormation;
        //                //if(enemyFormation != null)
        //                //{
        //                //    __result = RBMAI.Utilities.NearestAgentFromFormation(__instance.Position.AsVec2, enemyFormation);
        //                //    return false;
        //                //}
        //            }
        //        }
        //    }
        //    return true;
        //}

        [HarmonyPrefix]
        [HarmonyPatch("SetFiringOrder")]
        private static bool PrefixSetFiringOrder(ref Agent __instance, ref int order)
        {
            if (__instance.Formation != null && __instance.Formation.GetReadonlyMovementOrderReference().OrderType ==
                OrderType.ChargeWithTarget)
                if (__instance.Formation != null &&
                    __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
                {
                    var significantEnemy =
                        Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                    if (__instance.Formation.QuerySystem.IsInfantryFormation &&
                        !Utilities.FormationFightingInMelee(__instance.Formation, 0.5f))
                    {
                        var enemyCav =
                            Utilities.FindSignificantEnemy(__instance.Formation, false, false, true, false, false);

                        if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) enemyCav = null;

                        var cavDist = 0f;
                        var signDist = 1f;
                        if (enemyCav != null && significantEnemy != null)
                        {
                            var cavDirection = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                               __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            cavDist = cavDirection.Normalize();

                            var signDirection = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                                                __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            signDist = signDirection.Normalize();
                        }

                        if (enemyCav != null && cavDist <= signDist &&
                            enemyCav.CountOfUnits > __instance.Formation.CountOfUnits / 10 && signDist > 35f)
                            if (enemyCav.TargetFormation == __instance.Formation &&
                                (enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget ||
                                 enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.Charge))
                            {
                                if (Utilities.CheckIfCanBrace(__instance))
                                    //__instance.SetLookAgent(__instance.GetTargetAgent());
                                    order = 1;
                                else
                                    order = 0;
                            }
                    }
                }

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
            if (__instance.Formation != null && __instance.IsAIControlled &&
                __instance.Formation.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget)
            {
                if (__instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Square ||
                    __instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Circle ||
                    __instance.Formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.ShieldWall)
                {
                    __instance.EnforceShieldUsage(GetShieldDirectionOfUnit(__instance.Formation, __instance,
                        __instance.Formation.ArrangementOrder.OrderEnum));
                }
                else
                {
                    if (!__instance.WieldedOffhandWeapon.IsEmpty)
                    {
                        var hasnotusableonehand =
                            __instance.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);
                        var hasranged = __instance.IsRangedCached;
                        //bool hasranged = __instance.Equipment.HasAnyWeaponWithFlags(WeaponFlags.RangedWeapon);
                        var distance = __instance.GetTargetAgent() != null
                            ? __instance.Position.Distance(__instance.GetTargetAgent().Position)
                            : 100f;
                        if (!hasnotusableonehand && !hasranged && __instance.GetTargetAgent() != null && distance < 7f)
                            __instance.EnforceShieldUsage(Agent.UsageDirection.DefendDown);
                        else
                            __instance.EnforceShieldUsage(Agent.UsageDirection.None);
                    }
                }

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Formation))]
    internal class SetPositioningPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetPositioning")]
        private static bool PrefixSetPositioning(ref Formation __instance, ref int? unitSpacing)
        {
            if (__instance.ArrangementOrder == ArrangementOrderScatter)
            {
                if (unitSpacing == null) unitSpacing = 2;
                unitSpacing = 2;
            }

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
            if (input.OrderType == OrderType.Charge)
                if (__instance.QuerySystem.ClosestEnemyFormation != null)
                    input = MovementOrder.MovementOrderChargeToTarget(__instance.QuerySystem.ClosestEnemyFormation
                        .Formation);
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
            if (__instance.Formation != null)
            {
                var querySystem = __instance.Formation.QuerySystem;
                if (__instance.Formation.AI.ActiveBehavior == null || querySystem.IsRangedFormation)
                {
                    __result = 0f;
                    return false;
                }

                //if(__instance.Formation.QuerySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents > 15f)
                //{
                //__result = 10f;
                //return false;
                //}
                //__result =  MBMath.Lerp(0.1f, 1.2f, MBMath.ClampFloat(behaviorCoherence * (querySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents + 1f) / (querySystem.IdealAverageDisplacement + 1f), 0f, 3f) / 3f);
                __result = MBMath.Lerp(0.1f, 1.2f,
                    MBMath.ClampFloat(
                        __instance.BehaviorCoherence *
                        (querySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents + 1f) /
                        (querySystem.IdealAverageDisplacement + 1f), 0f, 3f) / 3f);
                return false;
            }

            return true;
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
            if (__instance.Formation != null &&
                __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null)
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);

                if (__instance.Formation.QuerySystem.IsInfantryFormation &&
                    !Utilities.FormationFightingInMelee(__instance.Formation, 0.5f))
                {
                    var _currentTacticField = typeof(TeamAIComponent).GetField("_currentTactic",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    _currentTacticField.DeclaringType.GetField("_currentTactic");
                    //TacticComponent _currentTactic = (TacticComponent);
                    if (__instance.Formation?.Team?.TeamAI != null)
                        if (_currentTacticField.GetValue(__instance.Formation?.Team?.TeamAI) != null &&
                            _currentTacticField.GetValue(__instance.Formation?.Team?.TeamAI).ToString()
                                .Contains("SplitArchers"))
                        {
                            var allyArchers = Utilities.FindSignificantAlly(__instance.Formation, false, true, false,
                                false, false);
                            if (allyArchers != null)
                            {
                                var dir = allyArchers.QuerySystem.MedianPosition.AsVec2 -
                                          __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                                var allyArchersDist = dir.Normalize();
                                if (allyArchersDist - allyArchers.Width / 2f - __instance.Formation.Width / 2f > 60f)
                                {
                                    ____currentOrder =
                                        MovementOrder.MovementOrderMove(__instance.Formation.QuerySystem
                                            .MedianPosition);
                                    return false;
                                }
                            }
                        }

                    var enemyCav =
                        Utilities.FindSignificantEnemy(__instance.Formation, false, false, true, false, false);

                    if (enemyCav != null && !enemyCav.QuerySystem.IsCavalryFormation) enemyCav = null;

                    var cavDist = 0f;
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
                        cavDist = cavDirection.Normalize();
                    }

                    if (enemyCav != null && cavDist <= signDist &&
                        enemyCav.CountOfUnits > __instance.Formation.CountOfUnits / 10 && signDist > 35f)
                    {
                        if (enemyCav.TargetFormation == __instance.Formation &&
                            (enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.ChargeWithTarget ||
                             enemyCav.GetReadonlyMovementOrderReference().OrderType == OrderType.Charge))
                        {
                            var vec = enemyCav.QuerySystem.MedianPosition.AsVec2 -
                                      __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                            var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                            var storedPosition = WorldPosition.Invalid;
                            positionsStorage.TryGetValue(__instance.Formation, out storedPosition);

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
                            //__instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent) {
                            //    if (RBMAI.Utilities.CheckIfCanBrace(agent))
                            //    {
                            //        agent.SetFiringOrder(1);
                            //    }
                            //    else
                            //    {
                            //        agent.SetFiringOrder(0);
                            //    }
                            //});
                            //__instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                            return false;
                        }

                        positionsStorage.Remove(__instance.Formation);
                        //medianPositionOld = WorldPosition.Invalid;
                    }
                    else if (significantEnemy != null && signDist < 60f &&
                             Utilities.FormationActiveSkirmishersRatio(__instance.Formation, 0.33f))
                    {
                        var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                        var storedPosition = WorldPosition.Invalid;
                        positionsStorage.TryGetValue(__instance.Formation, out storedPosition);

                        if (!storedPosition.IsValid)
                        {
                            positionsStorage.Add(__instance.Formation, positionNew);
                            ____currentOrder = MovementOrder.MovementOrderMove(positionNew);
                        }
                        else
                        {
                            ____currentOrder = MovementOrder.MovementOrderMove(storedPosition);
                        }

                        //__instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                        return false;
                        //__instance.Formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent) {
                        //    agent.SetMaximumSpeedLimit(0.1f, true);
                        //});
                    }

                    positionsStorage.Remove(__instance.Formation);
                }

                if (significantEnemy != null)
                {
                    //int storedWaitCount;
                    //if (Mission.Current.AllowAiTicking)
                    //{
                    //    if (waitCountStorage.TryGetValue(__instance.Formation, out storedWaitCount))
                    //    {
                    //        if (storedWaitCount < 100)
                    //        {
                    //            storedWaitCount++;
                    //            Vec2 direction = significantEnemy.QuerySystem.MedianPosition.AsVec2 - __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                    //            WorldPosition pos = __instance.Formation.QuerySystem.MedianPosition;
                    //            ____currentOrder = MovementOrder.MovementOrderMove(pos);
                    //            ___CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction.Normalized());
                    //            waitCountStorage[__instance.Formation] = storedWaitCount;
                    //            return false;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        waitCountStorage.Add(__instance.Formation, 0);
                    //    }
                    //}

                    var vec = significantEnemy.QuerySystem.MedianPosition.AsVec2 -
                              __instance.Formation.QuerySystem.MedianPosition.AsVec2;
                    var positionNew = __instance.Formation.QuerySystem.MedianPosition;

                    //if (!Mission.Current.IsPositionInsideBoundaries(positionNew.AsVec2) || positionNew.GetNavMesh() == UIntPtr.Zero)
                    //{
                    //}
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
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("TickOccasionally")]
        private static bool PrefixTickOccasionally(ref BehaviorAdvance __instance, ref MovementOrder ____currentOrder,
            ref FacingOrder ___CurrentFacingOrder,
            ref bool ____isInShieldWallDistance, ref bool ____switchedToShieldWallRecently,
            ref Timer ____switchedToShieldWallTimer)
        {
            var method = typeof(BehaviorAdvance).GetMethod("CalculateCurrentOrder",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.DeclaringType.GetMethod("CalculateCurrentOrder");
            method.Invoke(__instance, new object[] { });

            __instance.Formation.SetMovementOrder(__instance.CurrentOrder);
            __instance.Formation.FacingOrder = ___CurrentFacingOrder;
            if (__instance.Formation.IsInfantry())
            {
                var significantEnemy =
                    Utilities.FindSignificantEnemy(__instance.Formation, true, true, false, false, false);
                if (significantEnemy != null)
                {
                    var num = __instance.Formation.QuerySystem.AveragePosition.Distance(significantEnemy.QuerySystem
                        .MedianPosition.AsVec2);
                    if (num < 150f)
                    {
                        if (significantEnemy.ArrangementOrder == ArrangementOrderLine)
                            __instance.Formation.ArrangementOrder = ArrangementOrderLine;
                        else if (significantEnemy.ArrangementOrder == ArrangementOrderLoose)
                            __instance.Formation.ArrangementOrder = ArrangementOrderLoose;
                    }
                }
                //if (flag != ____isInShieldWallDistance)
                //{
                //    ____isInShieldWallDistance = flag;
                //    if (____isInShieldWallDistance)
                //    {
                //        if (__instance.Formation.QuerySystem.HasShield)
                //        {
                //            __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                //        }
                //        else
                //        {
                //            __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                //        }
                //        ____switchedToShieldWallRecently = true;
                //        ____switchedToShieldWallTimer.Reset(Mission.Current.CurrentTime, 5f);
                //    }
                //    else
                //    {
                //        __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                //    }
                //}
            }

            __instance.Formation.SetMovementOrder(__instance.CurrentOrder);
            return false;
        }
    }
}