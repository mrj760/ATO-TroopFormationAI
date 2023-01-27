using System.Collections.Generic;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionSpawnHandlers;

namespace RBMAI.AiModule
{
    internal class SpawningPatches
    {
        [HarmonyPatch(typeof(Mission))]
        private class SpawnTroopPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("SpawnTroop")]
            private static bool PrefixSpawnTroop(ref Mission __instance, IAgentOriginBase troopOrigin,
                bool isPlayerSide, bool hasFormation, bool spawnWithHorse, bool isReinforcement,
                int formationTroopCount, int formationTroopIndex, bool isAlarmed, bool wieldInitialWeapons,
                bool forceDismounted, ref Vec3? initialPosition, ref Vec2? initialDirection,
                string specialActionSetSuffix = null)
            {
                if (Mission.Current == null
                    || Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle
                    || !isReinforcement
                    || !hasFormation)
                    return true;


                var troop = troopOrigin.Troop;
                var agentTeam = Mission.GetAgentTeam(troopOrigin, isPlayerSide);
                var formation = agentTeam.GetFormation(troop.GetFormationClass());

                if (formation.CountOfUnits == 0)
                {
                    foreach (var allyFormation in agentTeam.Formations)
                    {
                        if (allyFormation.CountOfUnits <= 0) 
                            continue;

                        formation = allyFormation;
                        break;
                    }
                }

                if (formation.CountOfUnits == 0) 
                    return true;

                var tempWorldPosition = Mission.Current.GetClosestFleePositionForFormation(formation);
                var tempPos = tempWorldPosition.AsVec2;
                tempPos.x += MBRandom.RandomInt(20);
                tempPos.y += MBRandom.RandomInt(20);

                initialPosition = Mission.Current.DeploymentPlan
                    ?.GetClosestDeploymentBoundaryPosition(agentTeam.Side, tempPos,
                        DeploymentPlanType.Reinforcement).ToVec3();
                initialDirection = tempPos - formation.CurrentPosition;

                return true;
            }
        }

        [HarmonyPatch(typeof(SandBoxSiegeMissionSpawnHandler))]
        private class OverrideSandBoxSiegeMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            private static bool PrefixAfterStart(ref MapEvent ____mapEvent,
                ref MissionAgentSpawnLogic ____missionAgentSpawnLogic)
            {
                if (____mapEvent == null) 
                    return true;

                var battleSize = ____missionAgentSpawnLogic.BattleSize;

                var numberOfInvolvedMen = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
                var numberOfInvolvedMen2 = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);

                var totalBattleSize = numberOfInvolvedMen + numberOfInvolvedMen2;

                if (totalBattleSize <= battleSize) 
                    return true;

                var defenderAdvantage =
                    battleSize / (numberOfInvolvedMen * (battleSize * 2f / totalBattleSize));
                if (numberOfInvolvedMen < battleSize / 2f)
                    defenderAdvantage = totalBattleSize / (float)battleSize;
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);

                var spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                spawnSettings.DefenderAdvantageFactor = defenderAdvantage;

                ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2,
                    numberOfInvolvedMen, numberOfInvolvedMen2, true, true, in spawnSettings);

                return false;

            }
        }

        [HarmonyPatch(typeof(CustomSiegeMissionSpawnHandler))]
        private class OverrideCustomSiegeMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            private static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic,
                ref CustomBattleCombatant[] ____battleCombatants)
            {
                var battleSize = ____missionAgentSpawnLogic.BattleSize;

                var numberOfInvolvedMen = ____battleCombatants[0].NumberOfHealthyMembers;
                var numberOfInvolvedMen2 = ____battleCombatants[1].NumberOfHealthyMembers;

                var totalBattleSize = numberOfInvolvedMen + numberOfInvolvedMen2;

                if (totalBattleSize <= battleSize) 
                    return true;

                var defenderAdvantage = battleSize / (numberOfInvolvedMen * (battleSize * 2f / totalBattleSize));

                if (numberOfInvolvedMen < battleSize / 2f) 
                    defenderAdvantage = totalBattleSize / (float)battleSize;

                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);

                var spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                spawnSettings.DefenderAdvantageFactor = defenderAdvantage;

                ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2,
                    numberOfInvolvedMen, numberOfInvolvedMen2, true, true, in spawnSettings);

                return false;

            }
        }

        [HarmonyPatch(typeof(CustomBattleMissionSpawnHandler))]
        private class OverrideAfterStartCustomBattleMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            private static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic,
                ref CustomBattleCombatant ____defenderParty, ref CustomBattleCombatant ____attackerParty)
            {
                var battleSize = ____missionAgentSpawnLogic.BattleSize;

                var numberOfHealthyMembers = ____defenderParty.NumberOfHealthyMembers;
                var numberOfHealthyMembers2 = ____attackerParty.NumberOfHealthyMembers;

                var totalBattleSize = numberOfHealthyMembers + numberOfHealthyMembers2;

                if (totalBattleSize <= battleSize) 
                    return true;

                var defenderAdvantage = battleSize / (numberOfHealthyMembers * (battleSize * 2f / totalBattleSize));

                if (numberOfHealthyMembers < battleSize / 2f) 
                    defenderAdvantage = totalBattleSize / (float)battleSize;

                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, !Mission.Current.IsSiegeBattle);
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, !Mission.Current.IsSiegeBattle);

                var spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                spawnSettings.DefenderAdvantageFactor = defenderAdvantage;
                spawnSettings.ReinforcementBatchPercentage = 0.25f;
                spawnSettings.DesiredReinforcementPercentage = 0.5f;
                spawnSettings.ReinforcementTroopsSpawnMethod = MissionSpawnSettings.ReinforcementSpawnMethod.Fixed;

                ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfHealthyMembers, numberOfHealthyMembers2,
                    numberOfHealthyMembers, numberOfHealthyMembers2, true, true, in spawnSettings);

                return false;

            }
        }

        [HarmonyPatch(typeof(SandBoxBattleMissionSpawnHandler))]
        private class OverrideAfterStartSandBoxBattleMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            private static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic,
                ref MapEvent ____mapEvent)
            {
                if (____mapEvent == null) 
                    return true;

                var battleSize = ____missionAgentSpawnLogic.BattleSize;

                var numberOfInvolvedMen = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
                var numberOfInvolvedMen2 = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);

                var totalBattleSize = numberOfInvolvedMen + numberOfInvolvedMen2;

                if (totalBattleSize <= battleSize) 
                    return true;

                var defenderAdvantage =
                    battleSize / (numberOfInvolvedMen * (battleSize * 2f / totalBattleSize));
                if (numberOfInvolvedMen < battleSize / 2f)
                    defenderAdvantage = totalBattleSize / (float)battleSize;
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender,
                    !Mission.Current.IsSiegeBattle);
                ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker,
                    !Mission.Current.IsSiegeBattle);

                var spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                spawnSettings.DefenderAdvantageFactor = defenderAdvantage;
                spawnSettings.ReinforcementBatchPercentage = 0.25f;
                spawnSettings.DesiredReinforcementPercentage = 0.5f;
                spawnSettings.ReinforcementTroopsSpawnMethod =
                    MissionSpawnSettings.ReinforcementSpawnMethod.Fixed;

                ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2,
                    numberOfInvolvedMen, numberOfInvolvedMen2, true, true, in spawnSettings);
                return false;

            }
        }

        [HarmonyPatch(typeof(MissionAgentSpawnLogic))]
        private class OverrideBattleSizeSpawnTick
        {
            private static int numOfDefWhenSpawning = -1;
            private static int numOfAttWhenSpawning = -1;

            [HarmonyPrefix]
            [HarmonyPatch("CheckReinforcementBatch")]
            private static bool PrefixBattleSizeSpawnTick(ref MissionAgentSpawnLogic __instance,
                ref bool ____reinforcementSpawnEnabled, ref int ____battleSize, ref List<SpawnPhase>[] ____phases,
                ref MissionSpawnSettings ____spawnSettings)
            {
                if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle) 
                    return true;

                var numberOfTroops = __instance.NumberOfAgents;

                for (var i = 0; i < 2; i++)
                {
                    var numberOfTroopsCanBeSpawned = ____phases[i][0].RemainingSpawnNumber;

                    if (numberOfTroops <= 0 || numberOfTroopsCanBeSpawned <= 0) 
                        continue;

                    if (__instance.NumberOfRemainingTroops <= 0) 
                        return true;

                    var num4 = (____phases[0][0].InitialSpawnedNumber - __instance.NumberOfActiveDefenderTroops) 
                               / (float)____phases[0][0].InitialSpawnedNumber;

                    var num5 = (____phases[1][0].InitialSpawnedNumber - __instance.NumberOfActiveAttackerTroops) 
                               / (float)____phases[1][0].InitialSpawnedNumber;

                    if (!(____battleSize * 0.4f > __instance.NumberOfActiveDefenderTroops + __instance.NumberOfActiveAttackerTroops)
                        && !(num4 >= 0.6f)
                        && !(num5 >= 0.6f))
                    {
                        return false;
                    }

                    ____reinforcementSpawnEnabled = true;
                    numOfDefWhenSpawning = __instance.NumberOfActiveDefenderTroops;
                    numOfAttWhenSpawning = __instance.NumberOfActiveAttackerTroops;

                    var numberOfInvolvedMen = __instance.GetTotalNumberOfTroopsForSide(BattleSideEnum.Defender);
                    var numberOfInvolvedMen2 = __instance.GetTotalNumberOfTroopsForSide(BattleSideEnum.Attacker);

                    ____spawnSettings.DefenderReinforcementBatchPercentage =
                        (____battleSize * 0.5f - numOfDefWhenSpawning) /
                        (numberOfInvolvedMen + numberOfInvolvedMen2);

                    ____spawnSettings.AttackerReinforcementBatchPercentage =
                        (____battleSize * 0.5f - numOfAttWhenSpawning) /
                        (numberOfInvolvedMen + numberOfInvolvedMen2);

                    return true;

                }

                return true;
            }

            private class SpawnPhase
            {
                public int InitialSpawnedNumber;

                public int InitialSpawnNumber;

                public int NumberActiveTroops;

                public int RemainingSpawnNumber;
                public int TotalSpawnNumber;

                public void OnInitialTroopsSpawned()
                {
                    InitialSpawnedNumber = InitialSpawnNumber;
                    InitialSpawnNumber = 0;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerEncounter))]
        [HarmonyPatch("CheckIfBattleShouldContinueAfterBattleMission")]
        private class SetRoutedPatch
        {
            private static bool Prefix(ref CampaignBattleResult ____campaignBattleResult, ref bool __result)
            {
                if (____campaignBattleResult == null 
                    || !____campaignBattleResult.PlayerVictory 
                    || !____campaignBattleResult.BattleResolved) 
                    return true;

                __result = false;
                return false;
            }
        }
    }
}