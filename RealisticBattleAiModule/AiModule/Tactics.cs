using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RBMAI.AiModule.RbmBehaviors;
using RBMAI.AiModule.RbmTactics;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using static TaleWorlds.Core.ItemObject;

namespace RBMAI.AiModule
{
    public static class Tactics
    {
        public static Dictionary<Agent, AIDecision> aiDecisionCooldownDict = new Dictionary<Agent, AIDecision>();
        public static Dictionary<Agent, AgentDamageDone> agentDamage = new Dictionary<Agent, AgentDamageDone>();

        public class AIDecision
        {
            public enum AIDecisionType
            {
                None,
                FrontlineBackStep,
                FlankAllyLeft,
                FlankAllyRight
            }

            public int cooldown = 0;
            public int customMaxCoolDown = -1;
            public AIDecisionType decisionType = AIDecisionType.None;
            public WorldPosition position = WorldPosition.Invalid;
        }

        public class AgentDamageDone
        {
            public float damageDone;
            public FormationClass initialClass = FormationClass.Unset;
            public bool isAttacker;
        }


        [HarmonyPatch(typeof(Mission))]
        [HarmonyPatch("OnAgentHit")]
        private class CustomBattleAgentLogicOnAgentHitPatch
        {
            private static void Postfix(Agent affectedAgent, Agent affectorAgent, in Blow b,
                in AttackCollisionData collisionData, bool isBlocked, float damagedHp)
            {
                if (affectedAgent == null || affectorAgent == null || !affectedAgent.IsActive() ||
                    !affectedAgent.IsHuman || collisionData.AttackBlockedWithShield) 
                    return;

                if (!affectorAgent.IsHuman && affectorAgent.RiderAgent != null)
                    affectorAgent = affectorAgent.RiderAgent;

                if (affectorAgent?.Team == null) 
                    return;

                if (agentDamage.TryGetValue(affectorAgent, out _))
                {
                    agentDamage[affectorAgent].damageDone += damagedHp;
                }
                else
                {
                    var add = new AgentDamageDone();
                    switch (affectorAgent.IsRangedCached)
                    {
                        case true when !affectorAgent.HasMount:
                            add.initialClass = FormationClass.Ranged;
                            break;
                        case true when affectorAgent.HasMount:
                            add.initialClass = FormationClass.HorseArcher;
                            break;
                        case false when affectorAgent.HasMount:
                            add.initialClass = FormationClass.Cavalry;
                            break;
                        case false when !affectorAgent.HasMount && affectorAgent.IsHuman:
                            add.initialClass = FormationClass.Infantry;
                            break;
                    }
                    add.isAttacker = affectorAgent.Team.IsAttacker;
                    add.damageDone = damagedHp;
                    agentDamage[affectorAgent] = add;
                }
            }
        }
        
        [HarmonyPatch(typeof(TeamAIGeneral))]
        private class OverrideTeamAIGeneral
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnUnitAddedToFormationForTheFirstTime")]
            private static void PostfixOnUnitAddedToFormationForTheFirstTime(Formation formation)
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
        private class OverrideRefresh
        {
            private static string chooseIcon(Formation formation)
            {
                if (formation == null) 
                    return TargetIconType.None.ToString();

                if (formation.QuerySystem.IsInfantryFormation) return TargetIconType.Special_Swordsman.ToString();
                if (formation.QuerySystem.IsRangedFormation) return TargetIconType.Archer_Heavy.ToString();
                if (formation.QuerySystem.IsRangedCavalryFormation)
                    return TargetIconType.HorseArcher_Light.ToString();
                switch (formation.QuerySystem.IsCavalryFormation)
                {
                    case true when !Utilities.CheckIfMountedSkirmishFormation(formation, 0.6f):
                        return TargetIconType.Cavalry_Light.ToString();
                    case true when Utilities.CheckIfMountedSkirmishFormation(formation, 0.6f):
                        return TargetIconType.Special_JavelinThrower.ToString();
                    default:
                        return TargetIconType.None.ToString();
                }
            }

            private static void Postfix(MissionFormationMarkerTargetVM __instance)
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
                RBMAiPatcher.DoPatching();
                agentDamage.Clear();
            }
        }
        
        [HarmonyPatch(typeof(MissionCombatantsLogic))]
        [HarmonyPatch("EarlyStart")]
        public class EarlyStartPatch
        {
            public static void Postfix(ref IBattleCombatant ____attackerLeaderBattleCombatant,
                ref IBattleCombatant ____defenderLeaderBattleCombatant)
            {
                aiDecisionCooldownDict.Clear();
                agentDamage.Clear();
                RBMAiPatcher.DoPatching();
                OnTickAsAIPatch.itemPickupDistanceStorage.Clear();
                if (!Mission.Current.Teams.Any()) 
                    return;

                if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle) 
                    return;

                foreach (var team in Mission.Current.Teams.Where(t => t.HasTeamAi))
                {
                    if (team.Side == BattleSideEnum.Attacker)
                    {
                        team.ClearTacticOptions();
                        if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Empire)
                        {
                            team.AddTacticOption(new RBMTacticEmbolon(team));
                        }

                        if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Aserai ||
                            ____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Darshi) team.AddTacticOption(new RBMTacticAttackSplitSkirmishers(team));
                        if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Sturgia ||
                            ____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Nord) team.AddTacticOption(new RBMTacticAttackSplitInfantry(team));
                        if (____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                            CultureCode.Battania) team.AddTacticOption(new RBMTacticAttackSplitArchers(team));
                        team.AddTacticOption(new TacticFullScaleAttack(team));
                        team.AddTacticOption(new TacticCoordinatedRetreat(team));
                    }

                    if (team.Side != BattleSideEnum.Defender) 
                        continue;

                    team.ClearTacticOptions();
                    if (____defenderLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                        CultureCode.Battania) team.AddTacticOption(new RBMTacticDefendSplitArchers(team));
                    team.AddTacticOption(new TacticDefensiveEngagement(team));
                    team.AddTacticOption(new TacticDefensiveLine(team));
                    if (____defenderLeaderBattleCombatant?.BasicCulture?.GetCultureCode() ==
                        CultureCode.Sturgia) team.AddTacticOption(new RBMTacticDefendSplitInfantry(team));
                    team.AddTacticOption(new TacticFullScaleAttack(team));
                    team.AddTacticOption(new TacticCoordinatedRetreat(team));
                }
            }
        }

        [HarmonyPatch(typeof(TacticCoordinatedRetreat))]
        private class OverrideTacticCoordinatedRetreat
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetTacticWeight")]
            private static bool PrefixGetTacticWeight(ref TacticCoordinatedRetreat __instance, ref Team ___team,
                ref float __result)
            {
                var tqs = ___team.QuerySystem;
                if (tqs.InfantryRatio <= 0.1f && tqs.RangedRatio <= 0.1f)
                {
                    var power = tqs.TeamPower;
                    var enemyPower = tqs.EnemyTeams.Sum(et => et.TeamPower);
                    if (power / enemyPower <= 0.15f)
                    {
                        foreach (var formation in ___team.Formations)
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

        [HarmonyPatch(typeof(TacticFrontalCavalryCharge))]
        private class OverrideTacticFrontalCavalryCharge
        {
            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            private static void PostfixAdvance(ref Formation ____cavalry)
            {
                if (____cavalry == null) 
                    return;

                ____cavalry.AI.SetBehaviorWeight<BehaviorVanguard>(1.5f);
                ____cavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f);
            }

            [HarmonyPostfix]
            [HarmonyPatch("Attack")]
            private static void PostfixAttack(ref Formation ____mainInfantry, ref Formation ____cavalry,
                ref Formation ____archers)
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

                Utilities.FixCharge(ref ____mainInfantry);
            }

            [HarmonyPostfix]
            [HarmonyPatch("HasBattleBeenJoined")]
            private static void PostfixHasBattleBeenJoined(Formation ____cavalry, bool ____hasBattleBeenJoined,
                ref bool __result)
            {
                __result = Utilities.HasBattleBeenJoined(____cavalry, ____hasBattleBeenJoined, 125f);
            }

            [HarmonyPostfix]
            [HarmonyPatch("GetTacticWeight")]
            private static void PostfixGetTacticWeight(ref float __result)
            {
                __result *= 0.75f;
            }
        }

        [HarmonyPatch(typeof(TacticRangedHarrassmentOffensive))]
        private class OverrideTacticRangedHarrassmentOffensive
        {
            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            private static void PostfixAdvance(ref Formation ____archers, ref Formation ____mainInfantry,
                ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
            {
                if (____rightCavalry != null)
                {
                    ____rightCavalry.AI.ResetBehaviorWeights();
                    ____rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide =
                        FormationAI.BehaviorSide.Right;
                }

                if (____leftCavalry != null)
                {
                    ____leftCavalry.AI.ResetBehaviorWeights();
                    ____leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide =
                        FormationAI.BehaviorSide.Left;
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
            private static void PostfixAttack(ref Formation ____archers, ref Formation ____mainInfantry,
                ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
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
                    TacticComponent.SetDefaultBehaviorWeights(____rangedCavalry);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                }

                if (____archers != null)
                {
                    ____archers.AI.ResetBehaviorWeights();
                    ____archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                    ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                    ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
                }

                Utilities.FixCharge(ref ____mainInfantry);
            }

            [HarmonyPostfix]
            [HarmonyPatch("HasBattleBeenJoined")]
            private static void PostfixHasBattleBeenJoined(Formation ____mainInfantry, bool ____hasBattleBeenJoined,
                ref bool __result)
            {
                __result = Utilities.HasBattleBeenJoined(____mainInfantry, ____hasBattleBeenJoined);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ManageFormationCounts")]
            private static void PostfixManageFormationCounts(ref Formation ____leftCavalry,
                ref Formation ____rightCavalry)
            {
                if (____leftCavalry == null 
                    || ____rightCavalry == null 
                    || !____leftCavalry.IsAIControlled 
                    || !____rightCavalry.IsAIControlled)
                {
                    return;
                }

                var mountedSkirmishersList = new List<Agent>();
                var mountedMeleeList = new List<Agent>();
                ____leftCavalry.ApplyActionOnEachUnitViaBackupList(
                    delegate(Agent agent)
                {
                    var ismountedSkrimisher = false;
                    for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                         equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                         equipmentIndex++)
                        if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty &&
                            agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown &&
                            agent.MountAgent != null)
                        {
                            ismountedSkrimisher = true;
                            break;
                        }

                    if (ismountedSkrimisher)
                        mountedSkirmishersList.Add(agent);
                    else
                        mountedMeleeList.Add(agent);
                });

                ____rightCavalry.ApplyActionOnEachUnitViaBackupList(
                    delegate(Agent agent)
                {
                    var ismountedSkrimisher = false;
                    for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                         equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                         equipmentIndex++)
                        if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty &&
                            agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown &&
                            agent.MountAgent != null)
                        {
                            ismountedSkrimisher = true;
                            break;
                        }

                    if (ismountedSkrimisher)
                        mountedSkirmishersList.Add(agent);
                    else
                        mountedMeleeList.Add(agent);
                });
                var j = 0;
                var cavalryCount = ____leftCavalry.CountOfUnits + ____rightCavalry.CountOfUnits;
                foreach (var agent in mountedSkirmishersList)
                {
                    agent.Formation = 
                        j < cavalryCount / 2 ? 
                            ____leftCavalry : 
                            ____rightCavalry;
                    j++;
                }

                foreach (var agent in mountedMeleeList)
                {
                    agent.Formation = 
                        j < cavalryCount / 2 ? 
                            ____leftCavalry : 
                            ____rightCavalry;
                    j++;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("GetTacticWeight")]
            private static void PostfixGetTacticWeight(ref TacticRangedHarrassmentOffensive __instance,
                ref Team ___team, ref float __result)
            {
                if (___team?.Leader?.Character?.Culture?.GetCultureCode() == CultureCode.Khuzait)
                    __result *= 1.1f;
                else
                    __result *= 0.6f;
            }
        }

        [HarmonyPatch(typeof(TacticComponent))]
        private class OverrideTacticComponent
        {
            [HarmonyPrefix]
            [HarmonyPatch("SetDefaultBehaviorWeights")]
            private static bool PrefixSetDefaultBehaviorWeights(ref Formation f)
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
        private class ManageFormationCountsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ManageFormationCounts", typeof(int), typeof(int), typeof(int), typeof(int))]
            private static bool PrefixSetDefaultBehaviorWeights(ref TacticComponent __instance, ref Team ___team,
                ref int infantryCount, ref int rangedCount, ref int cavalryCount, ref int rangedCavalryCount)
            {

                if (Mission.Current != null && Mission.Current.IsFieldBattle)
                    foreach (var agent in ___team.ActiveAgents)
                        if (agent != null && agent.IsHuman && !agent.IsRunningAway)
                        {
                            var isRanged =
                                (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) &&
                                 agent.Equipment.GetAmmoAmount(WeaponClass.Arrow) > 5) ||
                                (agent.Equipment.HasRangedWeapon(WeaponClass.Bolt) &&
                                 agent.Equipment.GetAmmoAmount(WeaponClass.Bolt) > 5);

                            if (agent.HasMount)
                            {
                                switch (isRanged)
                                {
                                    case true when ___team.GetFormation(FormationClass.HorseArcher) != null &&
                                                   ___team.GetFormation(FormationClass.HorseArcher).IsAIControlled &&
                                                   agent.Formation != null && agent.Formation.IsAIControlled:
                                        agent.Formation = ___team.GetFormation(FormationClass.HorseArcher);
                                        break;

                                    case false when ___team.GetFormation(FormationClass.Cavalry) != null &&
                                                    ___team.GetFormation(FormationClass.Cavalry).IsAIControlled &&
                                                    agent.Formation != null && agent.Formation.IsAIControlled:
                                        agent.Formation = ___team.GetFormation(FormationClass.Cavalry);
                                        break;
                                }
                            }
                            else // not mounted
                            {
                                switch (isRanged)
                                {
                                    case true when ___team.GetFormation(FormationClass.Ranged) != null &&
                                                   ___team.GetFormation(FormationClass.Ranged).IsAIControlled &&
                                                   agent.Formation != null && agent.Formation.IsAIControlled:
                                        agent.Formation = ___team.GetFormation(FormationClass.Ranged);
                                        break;

                                    case false when ___team.GetFormation(FormationClass.Infantry) != null &&
                                                    ___team.GetFormation(FormationClass.Infantry).IsAIControlled &&
                                                    agent.Formation != null && agent.Formation.IsAIControlled:
                                        agent.Formation = ___team.GetFormation(FormationClass.Infantry);
                                        break;
                                }
                            }
                        }

                if (Mission.Current?.MainAgent != null 
                    && Mission.Current.PlayerTeam != null 
                    && Mission.Current.IsSiegeBattle)
                {
                    Mission.Current.MainAgent.Formation =
                        Mission.Current.PlayerTeam.GetFormation(FormationClass.Infantry);
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