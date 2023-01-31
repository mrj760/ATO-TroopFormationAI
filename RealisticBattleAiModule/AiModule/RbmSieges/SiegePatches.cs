using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace RBMAI.AiModule
{
    [HarmonyPatch]
    class SiegePatches
    {
        public static Dictionary<Team, bool> carryOutDefenceEnabled = new Dictionary<Team, bool>();
        public static Dictionary<Team, bool> archersShiftAroundEnabled = new Dictionary<Team, bool>();
        public static Dictionary<Team, bool> balanceLaneDefendersEnabled = new Dictionary<Team, bool>();

        [HarmonyPatch(typeof(MissionCombatantsLogic))]
        [HarmonyPatch("EarlyStart")]
        public class TeamAiFieldBattle
        {
            public static void Postfix()
            {
                balanceLaneDefendersEnabled.Clear();
                archersShiftAroundEnabled.Clear();
                carryOutDefenceEnabled.Clear();
            }
        }


        [HarmonyPatch(typeof(Mission))]
        class MissionPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnObjectDisabled")]
            static void PostfixOnObjectDisabled(DestructableComponent destructionComponent)
            {
                var ge = destructionComponent.GameEntity;
                if (ge.GetFirstScriptOfType<UsableMachine>() == null
                    || ge.GetFirstScriptOfType<UsableMachine>().GetType() != typeof(BatteringRam)
                    || !ge.GetFirstScriptOfType<UsableMachine>().IsDestroyed)
                    return;

                balanceLaneDefendersEnabled.Clear();
                carryOutDefenceEnabled.Clear();
            }
        }

        [HarmonyPatch(typeof(BehaviorShootFromCastleWalls))]
        class OverrideBehaviorShootFromCastleWalls
        {
            [HarmonyPrefix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            static bool PrefixOnBehaviorActivatedAux(ref BehaviorShootFromCastleWalls __instance, ref FacingOrder ___CurrentFacingOrder, ref MovementOrder ____currentOrder)
            {
                __instance.Formation.SetMovementOrder(____currentOrder);
                __instance.Formation.FacingOrder = ___CurrentFacingOrder;
                __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderScatter;
                __instance.Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                __instance.Formation.FormOrder = FormOrder.FormOrderWider;
                __instance.Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("TickOccasionally")]
            static bool PrefixTickOccasionally(ref BehaviorShootFromCastleWalls __instance, ref FacingOrder ___CurrentFacingOrder, ref MovementOrder ____currentOrder, ref TacticalPosition ____tacticalArcherPosition)
            {
                if (__instance.Formation.ArrangementOrder == ArrangementOrder.ArrangementOrderLine)
                {
                    __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderScatter;
                }
                __instance.Formation.SetMovementOrder(____currentOrder);
                __instance.Formation.FacingOrder = ___CurrentFacingOrder;
                if (____tacticalArcherPosition != null)
                {
                    __instance.Formation.FormOrder = FormOrder.FormOrderCustom(____tacticalArcherPosition.Width * 5f);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(BehaviorUseSiegeMachines))]
        class OverrideBehaviorUseSiegeMachines
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetAiWeight")]
            static bool PrefixGetAiWeight(ref BehaviorUseSiegeMachines __instance, ref float __result, ref TeamAISiegeComponent ____teamAISiegeComponent, List<UsableMachine> ____primarySiegeWeapons)
            {
                var result = 0f;
                if (____teamAISiegeComponent != null
                    && ____primarySiegeWeapons.Any()
                    && ____primarySiegeWeapons.All((UsableMachine psw) => !((IPrimarySiegeWeapon)psw).HasCompletedAction()))
                {
                    result = (____teamAISiegeComponent.IsCastleBreached() ? 0.75f : 1.5f);
                }
                __result = result;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("TickOccasionally")]
            static void PrefixTickOccasionally(ref BehaviorUseSiegeMachines __instance)
            {
                if (__instance.Formation.ArrangementOrder == ArrangementOrder.ArrangementOrderShieldWall)
                {
                    __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                }
            }
        }

        class OverrideBehaviorWaitForLadders
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetAiWeight")]
            static bool PrefixOnGetAiWeight(ref BehaviorWaitForLadders __instance, MovementOrder ____followOrder, ref TacticalPosition ____followTacticalPosition, ref float __result, ref TeamAISiegeComponent ____teamAISiegeComponent)
            {
                if (____followTacticalPosition == null
                    || !(____followTacticalPosition.Position.AsVec2.Distance(__instance.Formation.QuerySystem.AveragePosition) > 10f)
                    || ____followOrder.OrderEnum == 0
                    || ____teamAISiegeComponent.AreLaddersReady)
                    return true;

                __result = ((!____teamAISiegeComponent.IsCastleBreached()) ? 2f : 1f);
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("TickOccasionally")]
            static void PrefixTickOccasionally(ref BehaviorWaitForLadders __instance)
            {
                if (__instance.Formation.ArrangementOrder == ArrangementOrder.ArrangementOrderShieldWall)
                {
                    __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                }
            }
        }

        [HarmonyPatch(typeof(BehaviorDefendCastleKeyPosition))]
        class OverrideBehaviorDefendCastleKeyPosition
        {

            private enum BehaviorState
            {
                UnSet,
                Waiting,
                Ready
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            static bool PrefixOnBehaviorActivatedAux(ref BehaviorDefendCastleKeyPosition __instance, ref FacingOrder ___CurrentFacingOrder, ref MovementOrder ____currentOrder)
            {
                MethodInfo method = typeof(BehaviorDefendCastleKeyPosition).GetMethod("ResetOrderPositions", BindingFlags.NonPublic | BindingFlags.Instance);
                var _ = method?.DeclaringType?.GetMethod("ResetOrderPositions");
                method?.Invoke(__instance, new object[] { });

                __instance.Formation.SetMovementOrder(____currentOrder);
                __instance.Formation.FacingOrder = ___CurrentFacingOrder;
                __instance.Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                __instance.Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                __instance.Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("CalculateCurrentOrder")]
            static bool PrefixCalculateCurrentOrder(ref BehaviorDefendCastleKeyPosition __instance, ref FacingOrder ____waitFacingOrder,
                ref FacingOrder ____readyFacingOrder, ref TeamAISiegeComponent ____teamAISiegeDefender, ref FacingOrder ___CurrentFacingOrder,
                FormationAI.BehaviorSide ____behaviorSide, ref List<SiegeLadder> ____laddersOnThisSide, ref CastleGate ____innerGate,
                ref CastleGate ____outerGate, ref TacticalPosition ____tacticalMiddlePos, ref TacticalPosition ____tacticalWaitPos,
                ref MovementOrder ____waitOrder, ref MovementOrder ____readyOrder, ref MovementOrder ____currentOrder, ref BehaviorState ____behaviorState)
            {
                ____behaviorSide = __instance.Formation.AI.Side;
                ____innerGate = null;
                ____outerGate = null;
                ____laddersOnThisSide.Clear();
                var num =
                    Mission.Current.ActiveMissionObjects.FindAllWithType<CastleGate>().Any((CastleGate cg)
                        => cg.DefenseSide == ____behaviorSide && cg.GameEntity.HasTag("outer_gate"));

                var worldFrame = new WorldFrame();
                var worldFrame2 = new WorldFrame();

                //
                if (num)
                {
                    var outerGate = ____teamAISiegeDefender.OuterGate;
                    ____innerGate = ____teamAISiegeDefender.InnerGate;
                    ____outerGate = ____teamAISiegeDefender.OuterGate;
                    worldFrame = outerGate.MiddleFrame;
                    worldFrame2 = outerGate.DefenseWaitFrame;
                    ____tacticalMiddlePos = outerGate.MiddlePosition;
                    ____tacticalWaitPos = outerGate.WaitPosition;
                }
                else
                {
                    var wallSegment = (from ws in Mission.Current.ActiveMissionObjects.FindAllWithType<WallSegment>()
                                       where ws.DefenseSide == ____behaviorSide && ws.IsBreachedWall
                                       select ws).FirstOrDefault();
                    if (wallSegment != null)
                    {
                        worldFrame = wallSegment.MiddleFrame;
                        worldFrame2 = wallSegment.DefenseWaitFrame;
                        ____tacticalMiddlePos = wallSegment.MiddlePosition;
                        ____tacticalWaitPos = wallSegment.WaitPosition;
                    }
                    else
                    {
                        var source =
                            from sw
                                in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
                            where sw is IPrimarySiegeWeapon
                                  && ((IPrimarySiegeWeapon)sw).WeaponSide == ____behaviorSide
                                  && (!sw.IsDestroyed)
                            select sw;

                        var siegeWeapons = source.ToArray();

                        if (!siegeWeapons.Any())
                        {
                            worldFrame = WorldFrame.Invalid;
                            worldFrame2 = WorldFrame.Invalid;
                            ____tacticalMiddlePos = null;
                            ____tacticalWaitPos = null;
                        }
                        else
                        {
                            if ((siegeWeapons.FirstOrDefault() as IPrimarySiegeWeapon)?.TargetCastlePosition is ICastleKeyPosition castleKeyPosition)
                            {
                                worldFrame = castleKeyPosition.MiddleFrame;
                                worldFrame2 = castleKeyPosition.DefenseWaitFrame;
                                ____tacticalMiddlePos = castleKeyPosition.MiddlePosition;
                                ____tacticalWaitPos = castleKeyPosition.WaitPosition;
                            }
                        }
                    }
                }

                //
                if (____tacticalMiddlePos != null)
                {
                    ____readyFacingOrder = FacingOrder.FacingOrderLookAtDirection(____tacticalMiddlePos.Direction);
                    ____readyOrder = MovementOrder.MovementOrderMove(____tacticalMiddlePos.Position);
                }
                else if (worldFrame.Origin.IsValid)
                {
                    worldFrame.Rotation.f.Normalize();
                    ____readyOrder = MovementOrder.MovementOrderMove(worldFrame.Origin);
                    ____readyFacingOrder = FacingOrder.FacingOrderLookAtDirection(worldFrame.Rotation.f.AsVec2);
                }
                else
                {
                    ____readyOrder = MovementOrder.MovementOrderStop;
                    ____readyFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                }


                if (____tacticalWaitPos != null)
                {
                    ____waitFacingOrder = FacingOrder.FacingOrderLookAtDirection(____tacticalWaitPos.Direction);
                    ____waitOrder = MovementOrder.MovementOrderMove(____tacticalWaitPos.Position);
                }
                else if (worldFrame2.Origin.IsValid)
                {
                    worldFrame2.Rotation.f.Normalize();
                    ____waitOrder = MovementOrder.MovementOrderMove(worldFrame2.Origin);
                    ____waitFacingOrder = FacingOrder.FacingOrderLookAtDirection(worldFrame2.Rotation.f.AsVec2);
                }
                else
                {
                    ____waitOrder = MovementOrder.MovementOrderStop;
                    ____waitFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                }

                //
                switch (____behaviorState)
                {
                    case BehaviorState.Ready when ____tacticalMiddlePos != null:
                        __instance.Formation.FormOrder = FormOrder.FormOrderCustom(____tacticalMiddlePos.Width * 2f);
                        break;
                    case BehaviorState.Waiting when ____tacticalWaitPos != null:
                        __instance.Formation.FormOrder = FormOrder.FormOrderCustom(____tacticalWaitPos.Width * 2f);
                        break;
                }

                ____currentOrder = (____behaviorState == BehaviorState.Ready ? ____readyOrder : ____waitOrder);
                ___CurrentFacingOrder =
                    (__instance.Formation.QuerySystem.ClosestEnemyFormation != null
                     && TeamAISiegeComponent.IsFormationInsideCastle(
                         __instance.Formation.QuerySystem.ClosestEnemyFormation.Formation, includeOnlyPositionedUnits: true)
                        ? FacingOrder.FacingOrderLookAtEnemy
                        : ____behaviorState == BehaviorState.Ready ?
                            ____readyFacingOrder :
                            ____waitFacingOrder);

                ____laddersOnThisSide.Clear();

                //
                var significantLargeEnemyFormation =
                    __instance.Formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation;

                if (significantLargeEnemyFormation == null)
                    return true;

                float distance;

                if (____tacticalMiddlePos != null)
                {
                    if (____innerGate == null)
                    {
                        if (____outerGate != null)
                        {
                            distance = __instance.Formation.SmoothedAverageUnitPosition.Distance(__instance.Formation
                                .QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation
                                .SmoothedAverageUnitPosition);

                            if ((____outerGate.IsDestroyed || ____outerGate.IsGateOpen)
                                && (TeamAISiegeComponent.IsFormationInsideCastle(__instance.Formation,
                                        includeOnlyPositionedUnits: false, 0.25f)
                                    && distance < 35f
                                    || TeamAISiegeComponent.IsFormationInsideCastle(
                                        __instance.Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation
                                            .Formation, includeOnlyPositionedUnits: false, 0.2f)))
                            {
                                ____readyOrder = MovementOrder.MovementOrderChargeToTarget(__instance.Formation
                                    .QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
                                ____currentOrder = ____readyOrder;
                            }
                        }
                    }
                    else
                    {
                        distance = __instance.Formation.SmoothedAverageUnitPosition.Distance(significantLargeEnemyFormation.Formation.SmoothedAverageUnitPosition);

                        if ((____innerGate.IsDestroyed || ____innerGate.IsGateOpen)
                            && (TeamAISiegeComponent.IsFormationInsideCastle(__instance.Formation,
                                    includeOnlyPositionedUnits: false, 0.25f) && distance < 35f
                                || TeamAISiegeComponent.IsFormationInsideCastle(significantLargeEnemyFormation.Formation, includeOnlyPositionedUnits: false, 0.2f)))
                        {
                            ____readyOrder = MovementOrder.MovementOrderChargeToTarget(significantLargeEnemyFormation.Formation);
                            ____currentOrder = ____readyOrder;
                        }

                        if (!____innerGate.IsDestroyed)
                        {
                            var position = ____tacticalMiddlePos.Position;
                            if (____behaviorState == BehaviorState.Ready)
                            {
                                var direction =
                                    (____innerGate.GetPosition().AsVec2
                                     - __instance.Formation.QuerySystem.MedianPosition.AsVec2).Normalized();

                                var newPosition = position;
                                newPosition.SetVec2(position.AsVec2 - direction * 2f);

                                ____readyOrder = MovementOrder.MovementOrderMove(newPosition);
                                ____currentOrder = ____readyOrder;
                            }
                        }
                    }
                }

                if (____tacticalMiddlePos != null && ____innerGate == null && ____outerGate == null)
                {
                    var position = ____tacticalMiddlePos.Position;
                    var correctEnemy = RBMAI.Utilities.FindSignificantEnemyToPosition(__instance.Formation, position, true, false, false, false, false, true);
                    if (correctEnemy != null)
                    {
                        distance = __instance.Formation.QuerySystem.MedianPosition.AsVec2.Distance(significantLargeEnemyFormation.Formation.QuerySystem.MedianPosition.AsVec2);
                        if (TeamAISiegeComponent.IsFormationInsideCastle(correctEnemy, includeOnlyPositionedUnits: false, 0.2f)
                            || (distance < 35f
                                && TeamAISiegeComponent.IsFormationInsideCastle(correctEnemy, includeOnlyPositionedUnits: false, 0.05f)
                                && TeamAISiegeComponent.IsFormationInsideCastle(__instance.Formation, includeOnlyPositionedUnits: false, 0.25f)))
                        {
                            ____readyOrder = MovementOrder.MovementOrderChargeToTarget(correctEnemy);
                            ____waitOrder = MovementOrder.MovementOrderChargeToTarget(correctEnemy);
                            ____currentOrder = ____readyOrder;
                        }

                    }
                }

                if (____tacticalWaitPos == null || ____tacticalMiddlePos != null)
                    return true;

                distance = __instance.Formation.QuerySystem.MedianPosition.AsVec2.Distance(significantLargeEnemyFormation.Formation.QuerySystem.AveragePosition);

                if ((____innerGate.IsDestroyed || ____innerGate.IsGateOpen)
                    && (TeamAISiegeComponent.IsFormationInsideCastle(__instance.Formation, includeOnlyPositionedUnits: false, 0.25f) && distance < 35f
                        || TeamAISiegeComponent.IsFormationInsideCastle(significantLargeEnemyFormation.Formation, includeOnlyPositionedUnits: false, 0.2f)))
                {
                    ____readyOrder = MovementOrder.MovementOrderChargeToTarget(significantLargeEnemyFormation.Formation);
                    ____currentOrder = ____readyOrder;
                }

                return true;
            }

        }

        [HarmonyPatch(typeof(LadderQueueManager))]
        class OverrideLadderQueueManager
        {

            [HarmonyPostfix]
            [HarmonyPatch("Initialize")]
            static void PostfixInitialize(ref float ____arcAngle, ref int ____maxUserCount, ref float ____agentSpacing,
                ref float ____queueBeginDistance, ref float ____queueRowSize)
            {
                if (____maxUserCount == 3)
                {
                    ____arcAngle = (float)Math.PI * 1f / 2f;
                    ____agentSpacing = 1f;
                    ____queueBeginDistance = 3f;
                    ____queueRowSize = 1f;
                    ____maxUserCount = 15;
                }
                if (____maxUserCount == 1)
                {
                    ____maxUserCount = 0;
                }

            }
        }

        [HarmonyPatch(typeof(AgentMoraleInteractionLogic))]
        class AgentMoraleInteractionLogicPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ApplyMoraleEffectOnAgentIncapacitated")]
            static bool PrefixAfterStart(Agent affectedAgent, Agent affectorAgent, float affectedSideMaxMoraleLoss, float affectorSideMoraleMaxGain, float effectRadius)
            {
                if (affectedAgent == null) return true;

                return !Mission.Current.IsSiegeBattle || !affectedAgent.Team.IsDefender;
            }
        }

        [HarmonyPatch(typeof(TacticDefendCastle))]
        class StopUsingStrategicAreasPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("StopUsingStrategicAreas")]
            static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(TacticDefendCastle))]
        class StopUsingAllMachinesPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("StopUsingAllMachines")]
            static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(TacticDefendCastle))]
        class StopUsingAllRangedSiegeWeaponsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("StopUsingAllRangedSiegeWeapons")]
            static bool Prefix()
            {
                return false;
            }
        }
    }
}