﻿using System.Collections.Generic;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionSpawnHandlers;

namespace RBMAI.Patches
{
    [HarmonyPatch]
    public class SpawningPatches
    {

        [HarmonyPatch(typeof(Mission))]
        public class SpawnTroopPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("SpawnTroop")]
            public static bool PrefixSpawnTroop(ref Mission __instance, IAgentOriginBase troopOrigin, bool isPlayerSide, bool hasFormation, bool spawnWithHorse, bool isReinforcement, int formationTroopCount, int formationTroopIndex, bool isAlarmed, bool wieldInitialWeapons, bool forceDismounted, ref Vec3? initialPosition, ref Vec2? initialDirection, string specialActionSetSuffix = null)
            {
                if (Mission.Current != null && Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle)
                {
                    if (isReinforcement)
                    {
                        if (hasFormation)
                        {
                            BasicCharacterObject troop = troopOrigin.Troop;
                            Team agentTeam = Mission.GetAgentTeam(troopOrigin, isPlayerSide);
                            Formation formation = agentTeam.GetFormation(troop.GetFormationClass());
                            if (formation.CountOfUnits == 0)
                            {
                                foreach (Formation allyFormation in agentTeam.Formations)
                                {
                                    if (allyFormation.CountOfUnits > 0)
                                    {
                                        formation = allyFormation;
                                        break;
                                    }
                                }
                            }
                            if (formation.CountOfUnits == 0)
                            {
                                return true;
                            }
                            WorldPosition tempWorldPosition = Mission.Current.GetClosestFleePositionForFormation(formation);
                            Vec2 tempPos = tempWorldPosition.AsVec2;
                            tempPos.x = tempPos.x + MBRandom.RandomInt(20);
                            tempPos.y = tempPos.y + MBRandom.RandomInt(20);

                            initialPosition = Mission.Current.DeploymentPlan?.GetClosestDeploymentBoundaryPosition(agentTeam.Side, tempPos, DeploymentPlanType.Reinforcement).ToVec3();
                            initialDirection = tempPos - formation.CurrentPosition;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SandBoxSiegeMissionSpawnHandler))]
        public class OverrideSandBoxSiegeMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            public static bool PrefixAfterStart(ref MapEvent ____mapEvent, ref MissionAgentSpawnLogic ____missionAgentSpawnLogic)
            {
                if (____mapEvent != null)
                {
                    int battleSize = ____missionAgentSpawnLogic.BattleSize;

                    int numberOfInvolvedMen = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
                    int numberOfInvolvedMen2 = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
                    int defenderInitialSpawn = numberOfInvolvedMen;
                    int attackerInitialSpawn = numberOfInvolvedMen2;

                    int totalBattleSize = defenderInitialSpawn + attackerInitialSpawn;

                    if (totalBattleSize > battleSize)
                    {

                        float defenderAdvantage = (float)battleSize / ((float)defenderInitialSpawn * ((battleSize * 2f) / (totalBattleSize)));
                        if (defenderInitialSpawn < (battleSize / 2f))
                        {
                            defenderAdvantage = (float)totalBattleSize / (float)battleSize;
                        }
                        ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
                        ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);

                        MissionSpawnSettings spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                        spawnSettings.DefenderAdvantageFactor = defenderAdvantage;

                        ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
                        return false;
                    }
                    return true;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CustomSiegeMissionSpawnHandler))]
        public class OverrideCustomSiegeMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            public static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic, ref CustomBattleCombatant[] ____battleCombatants)
            {
                int battleSize = ____missionAgentSpawnLogic.BattleSize;

                int numberOfInvolvedMen = ____battleCombatants[0].NumberOfHealthyMembers;
                int numberOfInvolvedMen2 = ____battleCombatants[1].NumberOfHealthyMembers;
                int defenderInitialSpawn = numberOfInvolvedMen;
                int attackerInitialSpawn = numberOfInvolvedMen2;

                int totalBattleSize = defenderInitialSpawn + attackerInitialSpawn;

                if (totalBattleSize > battleSize)
                {

                    float defenderAdvantage = (float)battleSize / ((float)defenderInitialSpawn * ((battleSize * 2f) / (totalBattleSize)));
                    if (defenderInitialSpawn < (battleSize / 2f))
                    {
                        defenderAdvantage = (float)totalBattleSize / (float)battleSize;
                    }
                    ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
                    ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);

                    MissionSpawnSettings spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                    spawnSettings.DefenderAdvantageFactor = defenderAdvantage;

                    ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(CustomBattleMissionSpawnHandler))]
        public class OverrideAfterStartCustomBattleMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            public static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic, ref CustomBattleCombatant ____defenderParty, ref CustomBattleCombatant ____attackerParty)
            {

                int battleSize = ____missionAgentSpawnLogic.BattleSize;

                int numberOfHealthyMembers = ____defenderParty.NumberOfHealthyMembers;
                int numberOfHealthyMembers2 = ____attackerParty.NumberOfHealthyMembers;
                int defenderInitialSpawn = numberOfHealthyMembers;
                int attackerInitialSpawn = numberOfHealthyMembers2;

                int totalBattleSize = defenderInitialSpawn + attackerInitialSpawn;

                if (totalBattleSize > battleSize)
                {

                    float defenderAdvantage = (float)battleSize / ((float)defenderInitialSpawn * ((battleSize * 2f) / (totalBattleSize)));
                    if (defenderInitialSpawn < (battleSize / 2f))
                    {
                        defenderAdvantage = (float)totalBattleSize / (float)battleSize;
                    }
                    ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, !Mission.Current.IsSiegeBattle);
                    ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, !Mission.Current.IsSiegeBattle);

                    MissionSpawnSettings spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                    spawnSettings.DefenderAdvantageFactor = defenderAdvantage;
                    spawnSettings.ReinforcementBatchPercentage = 0.25f;
                    spawnSettings.DesiredReinforcementPercentage = 0.5f;
                    spawnSettings.ReinforcementTroopsSpawnMethod = MissionSpawnSettings.ReinforcementSpawnMethod.Fixed;

                    ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfHealthyMembers, numberOfHealthyMembers2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SandBoxBattleMissionSpawnHandler))]
        public class OverrideAfterStartSandBoxBattleMissionSpawnHandler
        {
            [HarmonyPrefix]
            [HarmonyPatch("AfterStart")]
            public static bool PrefixAfterStart(ref MissionAgentSpawnLogic ____missionAgentSpawnLogic, ref MapEvent ____mapEvent)
            {
                if (____mapEvent != null)
                {
                    int battleSize = ____missionAgentSpawnLogic.BattleSize;

                    int numberOfInvolvedMen = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
                    int numberOfInvolvedMen2 = ____mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
                    int defenderInitialSpawn = numberOfInvolvedMen;
                    int attackerInitialSpawn = numberOfInvolvedMen2;

                    int totalBattleSize = defenderInitialSpawn + attackerInitialSpawn;

                    if (totalBattleSize > battleSize)
                    {

                        float defenderAdvantage = (float)battleSize / ((float)defenderInitialSpawn * ((battleSize * 2f) / (totalBattleSize)));
                        if (defenderInitialSpawn < (battleSize / 2f))
                        {
                            defenderAdvantage = (float)totalBattleSize / (float)battleSize;
                        }
                        ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, !Mission.Current.IsSiegeBattle);
                        ____missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, !Mission.Current.IsSiegeBattle);

                        MissionSpawnSettings spawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
                        spawnSettings.DefenderAdvantageFactor = defenderAdvantage;
                        spawnSettings.ReinforcementBatchPercentage = 0.25f;
                        spawnSettings.DesiredReinforcementPercentage = 0.5f;
                        spawnSettings.ReinforcementTroopsSpawnMethod = MissionSpawnSettings.ReinforcementSpawnMethod.Fixed;

                        ____missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: true, spawnAttackers: true, in spawnSettings);
                        return false;
                    }
                    return true;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MissionAgentSpawnLogic))]
        public class OverrideBattleSizeSpawnTick
        {
            private static int numOfDefWhenSpawning = -1;
            private static int numOfAttWhenSpawning = -1;

            public class SpawnPhase
            {
                public int InitialSpawnedNumber;

                public int InitialSpawnNumber;

                public int RemainingSpawnNumber;

                public void OnInitialTroopsSpawned()
                {
                    InitialSpawnedNumber = InitialSpawnNumber;
                    InitialSpawnNumber = 0;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("CheckReinforcementBatch")]
            public static bool PrefixBattleSizeSpawnTick(ref MissionAgentSpawnLogic __instance, ref bool ____reinforcementSpawnEnabled, ref int ____battleSize, ref List<SpawnPhase>[] ____phases, ref MissionSpawnSettings ____spawnSettings)
            {
                if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
                {
                    return true;
                }
                int numberOfTroops = __instance.NumberOfAgents;
                for (int i = 0; i < 2; i++)
                {
                    int numberOfTroopsCanBeSpawned = ____phases[i][0].RemainingSpawnNumber;
                    if (numberOfTroops > 0 && numberOfTroopsCanBeSpawned > 0)
                    {
                        if (__instance.NumberOfRemainingTroops <= 0 || numberOfTroopsCanBeSpawned <= 0)
                        {
                            return true;
                        }
                        int activeAtt = __instance.NumberOfActiveAttackerTroops;
                        int activeDef = __instance.NumberOfActiveDefenderTroops;

                        float num4 = (float)(____phases[0][0].InitialSpawnedNumber - __instance.NumberOfActiveDefenderTroops) / (float)____phases[0][0].InitialSpawnedNumber;
                        float num5 = (float)(____phases[1][0].InitialSpawnedNumber - __instance.NumberOfActiveAttackerTroops) / (float)____phases[1][0].InitialSpawnedNumber;
                        if ((____battleSize * 0.4f > __instance.NumberOfActiveDefenderTroops + __instance.NumberOfActiveAttackerTroops) || num4 >= 0.6f || num5 >= 0.6f)
                        {
                            ____reinforcementSpawnEnabled = true;
                            numOfDefWhenSpawning = __instance.NumberOfActiveDefenderTroops;
                            numOfAttWhenSpawning = __instance.NumberOfActiveAttackerTroops;

                            int numberOfInvolvedMen = __instance.GetTotalNumberOfTroopsForSide(BattleSideEnum.Defender);
                            int numberOfInvolvedMen2 = __instance.GetTotalNumberOfTroopsForSide(BattleSideEnum.Attacker);

                            ____spawnSettings.DefenderReinforcementBatchPercentage = (____battleSize * 0.5f - numOfDefWhenSpawning) / (numberOfInvolvedMen + numberOfInvolvedMen2);
                            ____spawnSettings.AttackerReinforcementBatchPercentage = (____battleSize * 0.5f - numOfAttWhenSpawning) / (numberOfInvolvedMen + numberOfInvolvedMen2);
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PlayerEncounter))]
        [HarmonyPatch("CheckIfBattleShouldContinueAfterBattleMission")]
        public class SetRoutedPatch
        {
            public static bool Prefix(ref PlayerEncounter __instance, ref MapEvent ____mapEvent, ref CampaignBattleResult ____campaignBattleResult, ref bool __result)
            {
                if (____campaignBattleResult != null && ____campaignBattleResult.PlayerVictory && ____campaignBattleResult.BattleResolved)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}