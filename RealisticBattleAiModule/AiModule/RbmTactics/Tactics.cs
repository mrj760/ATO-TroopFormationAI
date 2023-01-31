using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RBMAI.AiModule.AiPatches;
using RBMAI.AiModule.RbmBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using static TaleWorlds.Core.ItemObject;

namespace RBMAI.AiModule.RbmTactics
{
    public static class Tactics
    {

        public class AIDecision
        {
            public int cooldown = 0;
            public WorldPosition position = WorldPosition.Invalid;
            public int customMaxCoolDown = -1;
            public AIDecisionType decisionType = AIDecisionType.None;
            public enum AIDecisionType
            {
                None,
                FrontlineBackStep,
                FlankAllyLeft,
                FlankAllyRight,
            }
        }

        public class AgentDamageDone
        {
            public float damageDone = 0f;
            public FormationClass initialClass = FormationClass.Unset;
            public bool isAttacker = false;
        }

        public static Dictionary<Agent, AIDecision> aiDecisionCooldownDict = new Dictionary<Agent, AIDecision>();
        public static Dictionary<Agent, AgentDamageDone> agentDamage = new Dictionary<Agent, AgentDamageDone>();


        [HarmonyPatch(typeof(Mission))]
        [HarmonyPatch("OnAgentHit")]
        class CustomBattleAgentLogicOnAgentHitPatch
        {
            static void Postfix(Agent affectedAgent, Agent affectorAgent, in Blow b, in AttackCollisionData collisionData, bool isBlocked, float damagedHp)
            {
                if (affectedAgent != null && affectorAgent != null && affectedAgent.IsActive() && affectedAgent.IsHuman && !collisionData.AttackBlockedWithShield)
                {
                    if (!affectorAgent.IsHuman && affectorAgent.RiderAgent != null)
                    {
                        affectorAgent = affectorAgent.RiderAgent;
                    }
                    if (affectorAgent != null && affectorAgent.Team != null)
                    {
                        AgentDamageDone damageDone;
                        if (agentDamage.TryGetValue(affectorAgent, out damageDone))
                        {
                            agentDamage[affectorAgent].damageDone += damagedHp;
                        }
                        else
                        {
                            AgentDamageDone add = new AgentDamageDone();
                            if (affectorAgent.IsRangedCached && !affectorAgent.HasMount)
                            {
                                add.initialClass = FormationClass.Ranged;
                            }
                            else if (affectorAgent.IsRangedCached && affectorAgent.HasMount)
                            {
                                add.initialClass = FormationClass.HorseArcher;
                            }
                            else if (!affectorAgent.IsRangedCached && affectorAgent.HasMount)
                            {
                                add.initialClass = FormationClass.Cavalry;
                            }
                            else if (!affectorAgent.IsRangedCached && !affectorAgent.HasMount && affectorAgent.IsHuman)
                            {
                                add.initialClass = FormationClass.Infantry;
                            }
                            add.isAttacker = affectorAgent.Team.IsAttacker;
                            add.damageDone = damagedHp;
                            agentDamage[affectorAgent] = add;
                        }
                    }

                }
            }
        }
        
        [HarmonyPatch(typeof(TeamAIGeneral))]
        class OverrideTeamAIGeneral
        {

            [HarmonyPostfix]
            [HarmonyPatch("OnUnitAddedToFormationForTheFirstTime")]
            static void PostfixOnUnitAddedToFormationForTheFirstTime(Formation formation)
            {
                formation.QuerySystem.Expire();
                formation.AI.AddAiBehavior(new RBMBehaviorArcherSkirmish(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorForwardSkirmish(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorInfantryAttackFlank(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorCavalryCharge(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorEmbolon(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorArcherFlank(formation));
                formation.AI.AddAiBehavior(new RBMBehaviorHorseArcherSkirmish(formation));
            }
        }

        [HarmonyPatch(typeof(MissionFormationMarkerTargetVM))]
        [HarmonyPatch("Refresh")]
        class OverrideRefresh
        {
            private static string chooseIcon(Formation formation)
            {
                if (formation == null)
                    return TargetIconType.None.ToString();

                if (formation.QuerySystem.IsInfantryFormation)
                {
                    return TargetIconType.Special_Swordsman.ToString();
                }
                if (formation.QuerySystem.IsRangedFormation)
                {
                    return TargetIconType.Archer_Heavy.ToString();
                }
                if (formation.QuerySystem.IsRangedCavalryFormation)
                {
                    return TargetIconType.HorseArcher_Light.ToString();
                }
                if (formation.QuerySystem.IsCavalryFormation && !RBMAI.Utilities.CheckIfMountedSkirmishFormation(formation, 0.6f))
                {
                    return TargetIconType.Cavalry_Light.ToString();
                }
                if (formation.QuerySystem.IsCavalryFormation && RBMAI.Utilities.CheckIfMountedSkirmishFormation(formation, 0.6f))
                {
                    return TargetIconType.Special_JavelinThrower.ToString();
                }
                return TargetIconType.None.ToString();
            }

            static void Postfix(MissionFormationMarkerTargetVM __instance)
            {
                __instance.FormationType = chooseIcon(__instance.Formation);
            }
        }

        [HarmonyPatch(typeof(CampaignMissionComponent))]
        [HarmonyPatch("EarlyStart")]
        public class CampaignMissionComponentPatch
        {
            public static void Postfix()
            {
                RBMAIPatcher.DoPatching();
                agentDamage.Clear();
            }
        }


        //[HarmonyPatch(typeof(MissionCombatantsLogic))]
        //[HarmonyPatch("AfterStart")]
        //public class AfterStartPatch
        //{
        //    public static void Postfix(ref IBattleCombatant ____attackerLeaderBattleCombatant)
        //    {
        //        if (Mission.Current.Teams.Any())
        //        {
        //            if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle)
        //            {
        //                foreach (Team team in Mission.Current.Teams.Where((Team t) => t.HasTeamAi))
        //                {
        //                    if (team.Side == BattleSideEnum.Attacker)
        //                    {
        //                        if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Empire)
        //                        {
        //                            team.AddTacticOption(new RBMTacticEmbolon(team));
        //                        }
        //                        else
        //                        {
        //                            team.AddTacticOption(new TacticFrontalCavalryCharge(team));
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


        [HarmonyPatch(typeof(MissionCombatantsLogic))]
        [HarmonyPatch("EarlyStart")]
        public class EarlyStartPatch
        {
            public static void Postfix(ref IBattleCombatant ____attackerLeaderBattleCombatant, ref IBattleCombatant ____defenderLeaderBattleCombatant, ref IEnumerable<IBattleCombatant> ____battleCombatants)
            {
                aiDecisionCooldownDict.Clear();
                agentDamage.Clear();
                RBMAIPatcher.DoPatching();
                OnTickAsAIPatch.itemPickupDistanceStorage.Clear();

                if (Mission.Current == null || !Mission.Current.Teams.Any() ||
                    Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
                    return;

                foreach (var team in Mission.Current.Teams.Where((Team t) => t.HasTeamAi))
                {
                    var battleCombatants = ____battleCombatants.ToList();
                    var partyTacticsSkill = battleCombatants.Where<IBattleCombatant>((Func<IBattleCombatant, bool>)(bc => bc.Side == team.Side)).Max<IBattleCombatant>((Func<IBattleCombatant, int>)(bcs => bcs.GetTacticsSkillAmount()));

                    team.ClearTacticOptions();

                    var over20 = partyTacticsSkill >= 20f;
                    var over50 = partyTacticsSkill >= 50f;

                    // both attacking and defending teams' tactics
                    team.AddTacticOption(new TacticCharge(team));
                    if (over20)
                    {
                        team.AddTacticOption(new RBMTacticEmbolon(team));
                        team.AddTacticOption(new TacticFullScaleAttack(team));
                        if (over50)
                        {
                            team.AddTacticOption(new TacticFrontalCavalryCharge(team));
                            team.AddTacticOption(new TacticCoordinatedRetreat(team));
                        }
                    }

                    // attacking team's tactics
                    if (team.Side == BattleSideEnum.Attacker)
                    {
                        if (over20)
                        {
                            team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
                            if (over50)
                            {
                                team.AddTacticOption(new RBMTacticAttackSplitArchers(team));
                                team.AddTacticOption(new RBMTacticAttackSplitInfantry(team));
                            }
                        }

                        /*
                                //if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Empire)
                                //{
                                //team.AddTacticOption(new RBMTacticEmbolon(team));
                                //}
                                //else
                                //{
                                //team.AddTacticOption(new TacticFrontalCavalryCharge(team));
                                //}
                                //if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Aserai
                                //    || ____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Darshi)
                                //{
                                //team.AddTacticOption(new RBMTacticAttackSplitSkirmishers(team));
                                //}
                                //if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Sturgia 
                                //    || ____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Nord)
                                //{
                                //    team.AddTacticOption(new RBMTacticAttackSplitInfantry(team));
                                //}
                                //if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Battania)
                                //{
                                //team.AddTacticOption(new RBMTacticAttackSplitArchers(team));
                                //}
                                //if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() != CultureCode.Vlandia)
                                //{
                                //    team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
                                //}
                                //team.AddTacticOption(new TacticFullScaleAttack(team));
                                //team.AddTacticOption(new RBMTacticEmbolon(team));
                                //team.AddTacticOption(new TacticCoordinatedRetreat(team));
                                //team.AddTacticOption(new TacticCharge(team));
                                //team.AddTacticOption(new RBMTacticAttackSplitSkirmishers(team));
                                //team.AddTacticOption(new RBMTacticAttackSplitInfantry(team));
                                */
                    }

                    // defending team's tactics
                    else if (team.Side == BattleSideEnum.Defender)
                    {
                        team.AddTacticOption(new TacticDefensiveLine(team));
                        if (over20)
                        {
                            team.AddTacticOption(new TacticDefensiveEngagement(team));
                            team.AddTacticOption(new TacticHoldChokePoint(team));
                            if (over50)
                            {
                                team.AddTacticOption(new TacticDefensiveRing(team));
                                team.AddTacticOption(new RBMTacticDefendSplitArchers(team));
                            }
                        }

                        /*
                                //if (____defenderLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Battania)
                                //{
                                //team.AddTacticOption(new RBMTacticDefendSplitArchers(team));
                                //}
                                //if(____defenderLeaderBattleCombatant?.BasicCulture?.GetCultureCode() == CultureCode.Sturgia)
                                //{
                                //    team.AddTacticOption(new RBMTacticDefendSplitInfantry(team));
                                //}
                                //team.AddTacticOption(new TacticFullScaleAttack(team));
                                //team.AddTacticOption(new TacticCharge(team));
                                //team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
                                //team.AddTacticOption(new TacticHoldTheHill(team));
                                //team.AddTacticOption(new TacticRangedHarrassmentOffensive(team));
                                //team.AddTacticOption(new TacticCoordinatedRetreat(team));
                                //team.AddTacticOption(new TacticFrontalCavalryCharge(team));
                                //team.AddTacticOption(new TacticDefensiveRing(team));
                                //team.AddTacticOption(new TacticArchersOnTheHill(team));
                                */
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TacticCoordinatedRetreat))]
        class OverrideTacticCoordinatedRetreat
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetTacticWeight")]
            static bool PrefixGetTacticWeight(ref TacticCoordinatedRetreat __instance, ref float __result, ref Team ___team)
            {
                //__result = 100f;
                if (___team.QuerySystem.InfantryRatio <= 0.1f && ___team.QuerySystem.RangedRatio <= 0.1f)
                {
                    float power = ___team.QuerySystem.GetLocalAllyPower(___team.QuerySystem.AveragePosition);
                    //float enemyPower = ___team.QuerySystem.EnemyTeams.Sum((TeamQuerySystem et) => et.TeamPower);
                    float enemyPower = ___team.QuerySystem.GetLocalEnemyPower(___team.QuerySystem.AveragePosition);
                    if (power / enemyPower <= 0.15f)
                    {
                        foreach (Formation formation in ___team.Formations.Where((Formation f) => f.CountOfUnits > 0))
                        {
                            formation.AI.ResetBehaviorWeights();
                            formation.AI.SetBehaviorWeight<BehaviorRetreat>(100f);
                        }
                        __result = 1000f;
                        return false;
                    }

                    __result = 0f;
                }
                else
                {
                    __result = 0f;
                }
                return false;
            }
        }

        //[HarmonyPatch(typeof(MissionAgentSpawnLogic))]
        //class OverrideSetSpawnTroops
        //{
        //    [HarmonyPrefix]
        //    [HarmonyPatch("SetSpawnTroops")]
        //    static bool PrefixSetupTeams(ref MissionAgentSpawnLogic __instance,  BattleSideEnum side, bool spawnTroops, bool enforceSpawning = false)
        //    {
        //        return true;
        //    }
        //}

        [HarmonyPatch(typeof(TacticFrontalCavalryCharge))]
        class OverrideTacticFrontalCavalryCharge
        {

            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            static void PostfixAdvance(ref Formation ____cavalry)
            {
                if (____cavalry != null)
                {
                    ____cavalry.AI.SetBehaviorWeight<BehaviorVanguard>(1.5f);
                    ____cavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Attack")]
            static void PostfixAttack(ref Formation ____mainInfantry, ref Formation ____cavalry, ref Formation ____archers)
            {

                if (____cavalry != null)
                {
                    ____cavalry.AI.ResetBehaviorWeights();
                    ____cavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                    ____cavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
                    ____cavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f);
                }
                if (____archers != null)
                {
                    ____archers.AI.ResetBehaviorWeights();
                    ____archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                    ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                    ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
                }
                RBMAI.Utilities.FixCharge(ref ____mainInfantry);
            }

            [HarmonyPostfix]
            [HarmonyPatch("HasBattleBeenJoined")]
            static void PostfixHasBattleBeenJoined(Formation ____cavalry, bool ____hasBattleBeenJoined, ref bool __result)
            {
                __result = RBMAI.Utilities.HasBattleBeenJoined(____cavalry, ____hasBattleBeenJoined, 125f);
            }

            //[HarmonyPostfix]
            //[HarmonyPatch("GetTacticWeight")]
            //static void PostfixGetAiWeight(TacticFrontalCavalryCharge __instance, ref float __result)
            //{
            //    FieldInfo teamField = typeof(TacticFrontalCavalryCharge).GetField("team", BindingFlags.NonPublic | BindingFlags.Instance);
            //    teamField.DeclaringType.GetField("team");
            //    Team currentTeam = (Team)teamField.GetValue(__instance);
            //        if (currentTeam.QuerySystem.CavalryRatio > 0.1f)
            //        {
            //            __result = currentTeam.QuerySystem.CavalryRatio * 4f;
            //        }
            //}

            [HarmonyPostfix]
            [HarmonyPatch("GetTacticWeight")]
            static void PostfixGetTacticWeight(ref float __result)
            {
                __result *= 0.75f;
            }
        }

        [HarmonyPatch(typeof(TacticRangedHarrassmentOffensive))]
        class OverrideTacticRangedHarrassmentOffensive
        {

            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            static void PostfixAdvance(ref Formation ____archers, ref Formation ____mainInfantry, ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
            {
                if (____rightCavalry != null)
                {
                    ____rightCavalry.AI.ResetBehaviorWeights();
                    ____rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
                }
                if (____leftCavalry != null)
                {
                    ____leftCavalry.AI.ResetBehaviorWeights();
                    ____leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
                }
                if (____rangedCavalry != null)
                {
                    ____rangedCavalry.AI.ResetBehaviorWeights();
                    TacticComponent.SetDefaultBehaviorWeights(____rangedCavalry);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Attack")]
            static void PostfixAttack(ref Formation ____archers, ref Formation ____mainInfantry, ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
            {

                if (____rightCavalry != null)
                {
                    ____rightCavalry.AI.ResetBehaviorWeights();
                    TacticComponent.SetDefaultBehaviorWeights(____rightCavalry);
                    ____rightCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                    ____rightCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
                }
                if (____leftCavalry != null)
                {
                    ____leftCavalry.AI.ResetBehaviorWeights();
                    TacticComponent.SetDefaultBehaviorWeights(____leftCavalry);
                    ____leftCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                    ____leftCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
                }
                if (____rangedCavalry != null)
                {
                    ____rangedCavalry.AI.ResetBehaviorWeights();
                    TacticRangedHarrassmentOffensive.SetDefaultBehaviorWeights(____rangedCavalry);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                }
                if (____archers != null)
                {
                    ____archers.AI.ResetBehaviorWeights();
                    ____archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                    ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                    ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
                }
                RBMAI.Utilities.FixCharge(ref ____mainInfantry);
            }

            [HarmonyPostfix]
            [HarmonyPatch("HasBattleBeenJoined")]
            static void PostfixHasBattleBeenJoined(Formation ____mainInfantry, bool ____hasBattleBeenJoined, ref bool __result)
            {
                __result = RBMAI.Utilities.HasBattleBeenJoined(____mainInfantry, ____hasBattleBeenJoined);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ManageFormationCounts")]
            static void PostfixManageFormationCounts(ref Formation ____leftCavalry, ref Formation ____rightCavalry)
            {
                if (____leftCavalry != null && ____rightCavalry != null && ____leftCavalry.IsAIControlled && ____rightCavalry.IsAIControlled)
                {
                    List<Agent> mountedSkirmishersList = new List<Agent>();
                    List<Agent> mountedMeleeList = new List<Agent>();
                    ____leftCavalry.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                    {
                        bool ismountedSkrimisher = false;
                        for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                        {
                            if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                            {
                                if (agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown && agent.MountAgent != null)
                                {
                                    ismountedSkrimisher = true;
                                    break;
                                }
                            }
                        }
                        if (ismountedSkrimisher)
                        {
                            mountedSkirmishersList.Add(agent);
                        }
                        else
                        {
                            mountedMeleeList.Add(agent);
                        }
                    });

                    ____rightCavalry.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                    {
                        bool ismountedSkrimisher = false;
                        for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                        {
                            if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                            {
                                if (agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown && agent.MountAgent != null)
                                {
                                    ismountedSkrimisher = true;
                                    break;
                                }
                            }
                        }
                        if (ismountedSkrimisher)
                        {
                            mountedSkirmishersList.Add(agent);
                        }
                        else
                        {
                            mountedMeleeList.Add(agent);
                        }
                    });
                    int j = 0;
                    int cavalryCount = ____leftCavalry.CountOfUnits + ____rightCavalry.CountOfUnits;
                    foreach (Agent agent in mountedSkirmishersList)
                    {
                        if (j < cavalryCount / 2)
                        {
                            agent.Formation = ____leftCavalry;
                        }
                        else
                        {
                            agent.Formation = ____rightCavalry;
                        }
                        j++;
                    }
                    foreach (Agent agent in mountedMeleeList)
                    {
                        if (j < cavalryCount / 2)
                        {
                            agent.Formation = ____leftCavalry;
                        }
                        else
                        {
                            agent.Formation = ____rightCavalry;
                        }
                        j++;
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("GetTacticWeight")]
            static void PostfixGetTacticWeight(ref TacticRangedHarrassmentOffensive __instance, ref float __result, ref Team ___team)
            {
                if (___team?.Leader?.Character?.Culture?.GetCultureCode() == CultureCode.Khuzait)
                {
                    __result *= 1.1f;
                }
                else
                {
                    __result *= 0.6f;
                }
            }
        }

        [HarmonyPatch(typeof(TacticComponent))]
        class OverrideTacticComponent
        {
            [HarmonyPrefix]
            [HarmonyPatch("SetDefaultBehaviorWeights")]
            static bool PrefixSetDefaultBehaviorWeights(ref Formation f)
            {
                if (f == null) 
                    return false;

                f.AI.SetBehaviorWeight<BehaviorCharge>(f.QuerySystem.IsRangedFormation ? 0.2f : 1f);
                f.AI.SetBehaviorWeight<BehaviorPullBack>(0f);
                f.AI.SetBehaviorWeight<BehaviorStop>(0f);
                f.AI.SetBehaviorWeight<BehaviorReserve>(0f);
                return false;
            }
        }

        [HarmonyPatch(typeof(TacticComponent))]
        class ManageFormationCountsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ManageFormationCounts", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
            static bool PrefixSetDefaultBehaviorWeights(ref TacticComponent __instance, ref int infantryCount, ref int rangedCount, ref int cavalryCount, ref int rangedCavalryCount, ref Team ___team)
            {
                if (Mission.Current != null && Mission.Current.IsFieldBattle)
                {
                    foreach (Agent agent in ___team.ActiveAgents)
                    {
                        if (agent != null && agent.IsHuman && !agent.IsRunningAway)
                        {
                            bool isRanged = (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) && agent.Equipment.GetAmmoAmount(WeaponClass.Arrow) > 5)
                                            || (agent.Equipment.HasRangedWeapon(WeaponClass.Bolt) && agent.Equipment.GetAmmoAmount(WeaponClass.Bolt) > 5);
                            if (agent.HasMount && isRanged)
                            {
                                if (___team.GetFormation(FormationClass.HorseArcher) != null && ___team.GetFormation(FormationClass.HorseArcher).IsAIControlled && agent.Formation != null && agent.Formation.IsAIControlled)
                                {
                                    agent.Formation = ___team.GetFormation(FormationClass.HorseArcher);
                                }
                            }
                            if (agent.HasMount && !isRanged)
                            {
                                if (___team.GetFormation(FormationClass.Cavalry) != null && ___team.GetFormation(FormationClass.Cavalry).IsAIControlled && agent.Formation != null && agent.Formation.IsAIControlled)
                                {
                                    agent.Formation = ___team.GetFormation(FormationClass.Cavalry);
                                }
                            }
                            if (!agent.HasMount && isRanged)
                            {
                                if (___team.GetFormation(FormationClass.Ranged) != null && ___team.GetFormation(FormationClass.Ranged).IsAIControlled && agent.Formation != null && agent.Formation.IsAIControlled)
                                {
                                    agent.Formation = ___team.GetFormation(FormationClass.Ranged);
                                }
                            }
                            if (!agent.HasMount && !isRanged)
                            {
                                if (___team.GetFormation(FormationClass.Infantry) != null && ___team.GetFormation(FormationClass.Infantry).IsAIControlled && agent.Formation != null && agent.Formation.IsAIControlled)
                                {
                                    agent.Formation = ___team.GetFormation(FormationClass.Infantry);
                                }
                            }
                        }
                    }

                }
                if (Mission.Current.MainAgent != null && Mission.Current.PlayerTeam != null && Mission.Current.IsSiegeBattle)
                {
                    Mission.Current.MainAgent.Formation = Mission.Current.PlayerTeam.GetFormation(FormationClass.Infantry);
                }
                return true;
            }

            [HarmonyPatch(typeof(MissionGauntletOrderOfBattleUIHandler))]
            private class MissionGauntletOrderOfBattleUIHandlerPatch
            {
                [HarmonyPostfix]
                [HarmonyPatch("OnDeploymentFinish")]
                private static void PostfixOnDeploymentFinish()
                {
                    if (Mission.Current == null)
                        return;

                    if (Mission.Current.MainAgent != null
                        && Mission.Current.PlayerTeam != null
                        && Mission.Current.IsSiegeBattle)
                    {
                        Mission.Current.MainAgent.Formation = Mission.Current.PlayerTeam.GetFormation(FormationClass.Infantry);
                    }
                }
            }
        }

    }
}