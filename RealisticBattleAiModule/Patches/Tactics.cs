using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using static TaleWorlds.Core.ItemObject;

namespace RBMAI.Patches
{
    [HarmonyPatch]
    public class Tactics
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
        public class CustomBattleAgentLogicOnAgentHitPatch
        {
            public static void Postfix(Agent affectedAgent, Agent affectorAgent, in Blow b, in AttackCollisionData collisionData, bool isBlocked, float damagedHp)
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
        public class OverrideTeamAIGeneral
        {

            [HarmonyPostfix]
            [HarmonyPatch("OnUnitAddedToFormationForTheFirstTime")]
            public static void PostfixOnUnitAddedToFormationForTheFirstTime(Formation formation)
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
        public class OverrideRefresh
        {
            public static string chooseIcon(Formation formation)
            {
                if (formation != null)
                {
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
                }
                return TargetIconType.None.ToString();
            }

            public static void Postfix(MissionFormationMarkerTargetVM __instance)
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
                //RBMAiPatcher.DoPatching();
                agentDamage.Clear();
            }
        }

        [HarmonyPatch(typeof(MissionCombatantsLogic))]
        [HarmonyPatch("EarlyStart")]
        public class EarlyStartPatch
        {
            public static void Postfix(ref IBattleCombatant ____attackerLeaderBattleCombatant, ref IBattleCombatant ____defenderLeaderBattleCombatant)
            {
                aiDecisionCooldownDict.Clear();
                agentDamage.Clear();
                //RBMAiPatcher.DoPatching();
                OnTickAsAIPatch.itemPickupDistanceStorage.Clear();

                if (!Mission.Current.Teams.Any()
                    || Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle)
                    return;

                foreach (Team team in Mission.Current.Teams.Where((Team t) => t.HasTeamAi).ToList())
                {
                    team.ClearTacticOptions();
                    team.AddTacticOption(new TacticFullScaleAttack(team));
                    team.AddTacticOption(new TacticCoordinatedRetreat(team));

                    CultureCode? culturecode;

                    if (team.Side == BattleSideEnum.Attacker)
                    {
                        culturecode = ____attackerLeaderBattleCombatant?.BasicCulture?.GetCultureCode();
                        switch (culturecode)
                        {
                            case CultureCode.Empire:
                                team.AddTacticOption(new RBMTacticEmbolon(team));
                                break;

                            case CultureCode.Aserai:
                            case CultureCode.Darshi:
                                team.AddTacticOption(new RBMTacticAttackSplitSkirmishers(team));
                                break;

                            case CultureCode.Sturgia:
                            case CultureCode.Nord:
                                team.AddTacticOption(new RBMTacticAttackSplitInfantry(team));
                                break;

                            case CultureCode.Battania:
                                team.AddTacticOption(new RBMTacticAttackSplitArchers(team));
                                break;
                        }
                    }
                    else if (team.Side == BattleSideEnum.Defender)
                    {
                        culturecode = ____defenderLeaderBattleCombatant?.BasicCulture?.GetCultureCode();
                        team.AddTacticOption(new TacticDefensiveEngagement(team));
                        team.AddTacticOption(new TacticDefensiveLine(team));

                        switch (culturecode)
                        {
                            case CultureCode.Battania:
                                team.AddTacticOption(new RBMTacticDefendSplitArchers(team));
                                break;
                            case CultureCode.Sturgia:
                                team.AddTacticOption(new RBMTacticDefendSplitInfantry(team));
                                break;
                        }
                    }
                }

            }
        }

        [HarmonyPatch(typeof(TacticCoordinatedRetreat))]
        public class OverrideTacticCoordinatedRetreat
        {
            [HarmonyPrefix]
            [HarmonyPatch("GetTacticWeight")]
            public static bool PrefixGetTacticWeight(ref TacticCoordinatedRetreat __instance, ref Team ___team, ref float __result)
            {
                if (___team.QuerySystem.InfantryRatio <= 0.1f && ___team.QuerySystem.RangedRatio <= 0.1f)
                {
                    float power = ___team.QuerySystem.TeamPower;
                    float enemyPower = ___team.QuerySystem.EnemyTeams.Sum((TeamQuerySystem et) => et.TeamPower);
                    if (power / enemyPower <= 0.15f)
                    {
                        foreach (Formation formation in ___team.Formations.ToList())
                        {
                            formation.AI.ResetBehaviorWeights();
                            formation.AI.SetBehaviorWeight<BehaviorRetreat>(100f);
                        }
                        __result = 1000f;
                        return false;
                    }
                    else
                    {
                        __result = 0f;
                    }
                }
                else
                {
                    __result = 0f;
                }
                return false;
            }
        }


        [HarmonyPatch(typeof(TacticFrontalCavalryCharge))]
        public class OverrideTacticFrontalCavalryCharge
        {

            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            public static void PostfixAdvance(ref Formation ____cavalry)
            {
                if (____cavalry != null)
                {
                    ____cavalry.AI.SetBehaviorWeight<BehaviorVanguard>(1.5f);
                    ____cavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Attack")]
            public static void PostfixAttack(ref Formation ____mainInfantry, ref Formation ____cavalry, ref Formation ____archers)
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
            public static void PostfixHasBattleBeenJoined(Formation ____cavalry, bool ____hasBattleBeenJoined, ref bool __result)
            {
                __result = RBMAI.Utilities.HasBattleBeenJoined(____cavalry, ____hasBattleBeenJoined, 125f);
            }


            [HarmonyPostfix]
            [HarmonyPatch("GetTacticWeight")]
            public static void PostfixGetTacticWeight(ref float __result)
            {
                __result *= 0.75f;
            }
        }

        [HarmonyPatch(typeof(TacticRangedHarrassmentOffensive))]
        public class OverrideTacticRangedHarrassmentOffensive
        {

            [HarmonyPostfix]
            [HarmonyPatch("Advance")]
            public static void PostfixAdvance(ref Formation ____archers, ref Formation ____mainInfantry, ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
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
                    TacticRangedHarrassmentOffensive.SetDefaultBehaviorWeights(____rangedCavalry);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                    ____rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("Attack")]
            public static void PostfixAttack(ref Formation ____archers, ref Formation ____mainInfantry, ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
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
            public static void PostfixHasBattleBeenJoined(Formation ____mainInfantry, bool ____hasBattleBeenJoined, ref bool __result)
            {
                __result = RBMAI.Utilities.HasBattleBeenJoined(____mainInfantry, ____hasBattleBeenJoined);
            }

            [HarmonyPostfix]
            [HarmonyPatch("ManageFormationCounts")]
            public static void PostfixManageFormationCounts(ref Formation ____leftCavalry, ref Formation ____rightCavalry)
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
                    foreach (Agent agent in mountedSkirmishersList.ToList())
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
                    foreach (Agent agent in mountedMeleeList.ToList())
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
            public static void PostfixGetTacticWeight(ref TacticRangedHarrassmentOffensive __instance, ref Team ___team, ref float __result)
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
        public class OverrideTacticComponent
        {
            [HarmonyPrefix]
            [HarmonyPatch("SetDefaultBehaviorWeights")]
            public static bool PrefixSetDefaultBehaviorWeights(ref Formation f)
            {
                if (f != null)
                {
                    if (f.QuerySystem.IsRangedFormation)
                    {
                        f.AI.SetBehaviorWeight<BehaviorCharge>(0.2f);
                    }
                    else
                    {
                        f.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                    }
                    f.AI.SetBehaviorWeight<BehaviorPullBack>(0f);
                    f.AI.SetBehaviorWeight<BehaviorStop>(0f);
                    f.AI.SetBehaviorWeight<BehaviorReserve>(0f);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(TacticComponent))]
        public class ManageFormationCountsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ManageFormationCounts", new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
            static bool PrefixSetDefaultBehaviorWeights(ref TacticComponent __instance, ref Team ___team, ref int infantryCount, ref int rangedCount, ref int cavalryCount, ref int rangedCavalryCount)
            {
                if (Mission.Current != null && Mission.Current.IsFieldBattle)
                {
                    foreach (Agent agent in ___team.ActiveAgents)
                    {
                        if (agent != null && agent.IsHuman && !agent.IsRunningAway)
                        {
                            bool isRanged = (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) && agent.Equipment.GetAmmoAmount(WeaponClass.Arrow) > 5) || (agent.Equipment.HasRangedWeapon(WeaponClass.Bolt) && agent.Equipment.GetAmmoAmount(WeaponClass.Bolt) > 5);
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

        }

        [HarmonyPatch(typeof(MissionGauntletOrderOfBattleUIHandler))]
        public class MissionGauntletOrderOfBattleUIHandlerPatch
        {

            [HarmonyPostfix]
            [HarmonyPatch("OnDeploymentFinish")]
            public static void PostfixOnDeploymentFinish()
            {
                if (Mission.Current != null)
                {
                    Team ___team = Mission.Current.PlayerTeam;
                    if (Mission.Current != null && Mission.Current.IsSiegeBattle && ___team != null && ___team.IsPlayerTeam)
                    {
                    }
                    if (Mission.Current.MainAgent != null && Mission.Current.PlayerTeam != null && Mission.Current.IsSiegeBattle)
                    {
                        Mission.Current.MainAgent.Formation = Mission.Current.PlayerTeam.GetFormation(FormationClass.Infantry);

                    }
                }
            }
        }

    }
}