using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace RBMAI.AiModule
{
    
    [HarmonyPatch(typeof(ArrangementOrder))]
    [HarmonyPatch("GetShieldDirectionOfUnit")]
    internal class HoldTheDoor
    {
        private static void Postfix(ref Agent.UsageDirection __result, Formation formation, Agent unit,
            ArrangementOrderEnum orderEnum)
        {
            if (unit.IsDetachedFromFormation)
            {
                __result = Agent.UsageDirection.None;
                return;
            }

            var test = true;
            switch (orderEnum)
            {
                case ArrangementOrderEnum.ShieldWall:

                    if (unit.Formation.FiringOrder.OrderEnum != FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire)
                    {
                        var hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        var hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);

                        if (hasRanged || hasTwoHanded)
                            test = false;
                    }

                    if (test)
                    {
                        if (((IFormationUnit)unit).FormationRankIndex == 0)
                        {
                            __result = Agent.UsageDirection.DefendDown;
                            return;
                        }

                        if (formation.Arrangement.GetNeighborUnitOfLeftSide(unit) == null)
                        {
                            __result = Agent.UsageDirection.DefendLeft;
                            return;
                        }

                        if (formation.Arrangement.GetNeighborUnitOfRightSide(unit) == null)
                        {
                            __result = Agent.UsageDirection.DefendRight;
                            return;
                        }

                        __result = Agent.UsageDirection.AttackEnd;
                        return;
                    }

                    __result = Agent.UsageDirection.None;
                    return;

                case ArrangementOrderEnum.Circle:
                case ArrangementOrderEnum.Square:

                    if (unit.Formation.FiringOrder.OrderEnum != FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire)
                    {
                        var hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        var hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);

                        if (hasRanged || hasTwoHanded)
                            test = false;
                    }

                    if (test)
                    {
                        if (((IFormationUnit)unit).FormationRankIndex == 0)
                        {
                            __result = Agent.UsageDirection.DefendDown;
                            return;
                        }

                        __result = Agent.UsageDirection.AttackEnd;
                        return;
                    }

                    __result = Agent.UsageDirection.None;
                    return;

                default:
                    __result = Agent.UsageDirection.None;
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(Agent))]
    [HarmonyPatch("UpdateLastAttackAndHitTimes")]
    internal class UpdateLastAttackAndHitTimesFix
    {
        private static bool Prefix(ref Agent __instance, Agent attackerAgent, bool isMissile)
        {
            var LastRangedHitTime = typeof(Agent).GetProperty("LastRangedHitTime");
            var _ = LastRangedHitTime?.DeclaringType?.GetProperty("LastRangedHitTime");

            var LastRangedAttackTime = typeof(Agent).GetProperty("LastRangedAttackTime");
            _ = LastRangedAttackTime?.DeclaringType?.GetProperty("LastRangedAttackTime");

            var LastMeleeHitTime = typeof(Agent).GetProperty("LastMeleeHitTime");
            _ = LastMeleeHitTime?.DeclaringType?.GetProperty("LastMeleeHitTime");

            var LastMeleeAttackTime = typeof(Agent).GetProperty("LastMeleeAttackTime");
            _ = LastMeleeAttackTime?.DeclaringType?.GetProperty("LastMeleeAttackTime");

            var currentTime = MBCommon.GetTotalMissionTime();



            if (attackerAgent != __instance && attackerAgent != null)
            {
                if (isMissile)
                    LastRangedAttackTime?.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                else
                    LastMeleeAttackTime?.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }
            else
            {
                if (isMissile)
                    LastRangedHitTime?.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                else
                    LastMeleeHitTime?.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }

            if (__instance.IsHuman || __instance.RiderAgent == null)
                return false;

            if (isMissile)
                LastRangedHitTime?.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            else
                LastMeleeHitTime?.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);

            return false;
        }
    }

    [HarmonyPatch(typeof(HumanAIComponent))]
    [HarmonyPatch("OnTickAsAI")]
    internal class OnTickAsAIPatch
    {
        public static Dictionary<Agent, float> itemPickupDistanceStorage = new Dictionary<Agent, float>();

        private static void Postfix(ref SpawnedItemEntity ____itemToPickUp, ref Agent ___Agent)
        {
            if (____itemToPickUp == null || (___Agent.AIStateFlags & Agent.AIStateFlag.UseObjectMoving) == 0)
                return;

            var num = MissionGameModels.Current.AgentStatCalculateModel.GetInteractionDistance(___Agent) * 3f;
            var userFrameForAgent = ____itemToPickUp.GetUserFrameForAgent(___Agent);
            ref var origin = ref userFrameForAgent.Origin;
            var targetPoint = ___Agent.Position;
            var distanceSq = origin.DistanceSquaredWithLimit(in targetPoint, num * num + 1E-05f);

            itemPickupDistanceStorage.TryGetValue(___Agent, out var newDist);

            if (Math.Abs(0f - newDist) < .01f)
            {
                itemPickupDistanceStorage[___Agent] = distanceSq;
            }
            else
            {
                if (Math.Abs(distanceSq - newDist) < .01f)
                {
                    ___Agent.StopUsingGameObject(false);
                    itemPickupDistanceStorage.Remove(___Agent);
                }

                itemPickupDistanceStorage[___Agent] = distanceSq;
            }
        }
    }
    
    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentDismount")]
    internal class OnAgentDismountPatch
    {
        private static void Postfix(Agent agent, Mission __instance)
        {
            if (agent.IsPlayerControlled || Mission.Current == null || !Mission.Current.IsFieldBattle || agent.Formation == null)
                return;

            var inf = agent.Team.GetFormation(FormationClass.Infantry);
            var rang = agent.Team.GetFormation(FormationClass.Ranged);
            var agentform = agent.Formation;

            var isInfFormationActive = inf != null && inf.CountOfUnits > 0;
            var isArcFormationActive = rang != null && rang.CountOfUnits > 0;

            if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) || agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
            {
                var distanceToInf = -1f;
                var distanceToArc = -1f;

                if (agentform != null)
                {
                    if (isInfFormationActive)
                        distanceToInf = inf.QuerySystem.MedianPosition.AsVec2.Distance(agentform.QuerySystem.MedianPosition.AsVec2);

                    if (isArcFormationActive)
                        distanceToArc = rang.QuerySystem.MedianPosition.AsVec2.Distance(agentform.QuerySystem.MedianPosition.AsVec2);
                }

                if (distanceToArc > 0f && distanceToArc < distanceToInf)
                {
                    agentform = rang;
                }
                else if (distanceToInf > 0f && distanceToInf < distanceToArc)
                {
                    agentform = inf;
                }
                else if (distanceToInf > 0f)
                {
                    agentform = inf;
                }
                else if (distanceToArc > 0f)
                {
                    agentform = rang;
                }

                agent.DisableScriptedMovement();
                agent.Formation = agentform;
            }
            else
            {
                if (agentform == null || !isInfFormationActive)
                    return;

                agent.Formation = inf;
                agent.DisableScriptedMovement();
            }
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentMount")]
    internal class OnAgentMountPatch
    {
        private static void Postfix(Agent agent, Mission __instance)
        {
            if (Mission.Current == null || !Mission.Current.IsFieldBattle || agent.IsPlayerControlled || agent.Formation == null)
                return;

            var isCavFormationActive = agent.Team.GetFormation(FormationClass.Cavalry) != null &&
                                       agent.Team.GetFormation(FormationClass.Cavalry).CountOfUnits > 0;
            var isHaFormationActive = agent.Team.GetFormation(FormationClass.HorseArcher) != null &&
                                      agent.Team.GetFormation(FormationClass.HorseArcher).CountOfUnits > 0;

            if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) ||
                agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
            {
                if (!isHaFormationActive || agent.Formation == null)
                    return;

                agent.Formation = agent.Team.GetFormation(FormationClass.HorseArcher);
                agent.DisableScriptedMovement();
            }
            else
            {
                if (!isCavFormationActive || agent.Formation == null)
                    return;

                agent.Formation = agent.Team.GetFormation(FormationClass.Cavalry);
                agent.DisableScriptedMovement();
            }
        }
    }

    [HarmonyPatch(typeof(Formation))]
    [HarmonyPatch("ApplyActionOnEachUnit", typeof(Action<Agent>))]
    internal class ApplyActionOnEachUnitPatch
    {
        private static bool Prefix(ref Action<Agent> action, ref Formation __instance)
        {
            try
            {
                __instance.ApplyActionOnEachUnitViaBackupList(action);
                return false;
            }
            catch (Exception)
            {
                {
                    return true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MissionAgentLabelView))]
    [HarmonyPatch("SetHighlightForAgents")]
    internal class SetHighlightForAgentsPatch
    {
        private static bool Prefix(bool highlight, ref bool useSiegeMachineUsers, ref bool useAllTeamAgents,
            Dictionary<Agent, MetaMesh> ____agentMeshes, MissionAgentLabelView __instance)
        {
            if (__instance.Mission == null)
                return true;

            var pordercont = __instance.Mission.PlayerTeam?.PlayerOrderController;

            if (pordercont == null)
            {
                var flag = __instance.Mission.PlayerTeam == null;
                Debug.Print($"PlayerOrderController is null and playerTeamIsNull: {flag}", 0, Debug.DebugColor.White, 17179869184uL);
                return true;
            }

            if (useSiegeMachineUsers)
            {
                foreach (var selectedWeapon in pordercont.SiegeWeaponController.SelectedWeapons)
                    foreach (var user in selectedWeapon.Users)
                    {
                        if (!____agentMeshes.TryGetValue(user, out var agentMesh))
                            continue;

                        var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                        method?.Invoke(__instance, new object[] { user, agentMesh, highlight });
                    }

                return false;
            }

            if (useAllTeamAgents)
            {
                foreach (var activeAgent in pordercont.Owner.Team.ActiveAgents)
                {
                    if (!____agentMeshes.TryGetValue(activeAgent, out var agentMesh))
                        continue;

                    var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                    method?.Invoke(__instance, new object[] { activeAgent, agentMesh, highlight });
                }

                return false;
            }

            foreach (var selectedFormation in pordercont.SelectedFormations)
                selectedFormation.ApplyActionOnEachUnit(delegate (Agent agent)
                {
                    if (!____agentMeshes.TryGetValue(agent, out var agentMesh))
                        return;

                    var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                    method?.Invoke(__instance, new object[] { agent, agentMesh, highlight });
                });
            return false;
        }
    }
}