using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Core.ItemObject;

namespace RBMAI.AiModule.RbmTactics
{
    [HarmonyPatch(typeof(TacticFullScaleAttack))]
    internal class TacticFullScaleAttackPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Advance")]
        private static void PostfixAdvance(ref Formation ____mainInfantry, ref Formation ____archers,
            ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
        {
            FormationAI.BehaviorSide newside;

            ____mainInfantry?.AI.SetBehaviorWeight<BehaviorRegroup>(1.75f);

            if (____archers != null)
            {
                ____archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0f);
                ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                ____archers.AI.SetBehaviorWeight<BehaviorRegroup>(1.25f);
            }

            if (____rightCavalry != null)
            {
                newside = FormationAI.BehaviorSide.Right;

                ____rightCavalry.AI.ResetBehaviorWeights();
                ____rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = newside;
                ____rightCavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f).FlankSide = newside;
            }

            if (____leftCavalry != null)
            {
                newside = FormationAI.BehaviorSide.Left;

                ____leftCavalry.AI.ResetBehaviorWeights();
                ____leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = newside;
                ____leftCavalry.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f).FlankSide = newside;
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
        private static void PostfixAttack(ref Formation ____mainInfantry, ref Formation ____archers,
            ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
        {

            if (____archers != null)
            {
                ____archers.AI.ResetBehaviorWeights();
                ____archers.AI.AddAiBehavior(new RBMBehaviorArcherSkirmish(____archers));
                ____archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
            }

            if (____rightCavalry != null)
            {
                ____rightCavalry.AI.ResetBehaviorWeights();
                ____rightCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                ____rightCavalry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                ____rightCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            }

            if (____leftCavalry != null)
            {
                ____leftCavalry.AI.ResetBehaviorWeights();
                ____leftCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                ____leftCavalry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                ____leftCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            }

            if (____rangedCavalry != null)
            {
                ____rangedCavalry.AI.ResetBehaviorWeights();
                TacticComponent.SetDefaultBehaviorWeights(____rangedCavalry);
                ____rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
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
        [HarmonyPatch("GetTacticWeight")]
        private static void PostfixGetAiWeight(TacticFullScaleAttack __instance, ref float __result)
        {
            var teamField = typeof(TacticFullScaleAttack).GetField("team", BindingFlags.NonPublic | BindingFlags.Instance);
            var _ = teamField?.DeclaringType?.GetField("team");

            var currentTeam = (Team)teamField?.GetValue(__instance);

            if (currentTeam != null 
                && currentTeam.Side == BattleSideEnum.Defender 
                && currentTeam.QuerySystem.InfantryRatio > 0.9f) 
                __result = 100f;
            
            else if (float.IsNaN(__result)) 
                __result = 0.01f;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ManageFormationCounts")]
        private static void PostfixManageFormationCounts(ref Formation ____leftCavalry, ref Formation ____rightCavalry)
        {
            if (____leftCavalry == null 
                || ____rightCavalry == null 
                || !____leftCavalry.IsAIControlled 
                || !____rightCavalry.IsAIControlled) 
                return;

            var mountedSkirmishersList = new List<Agent>();
            var mountedMeleeList = new List<Agent>();

            ____leftCavalry.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                var ismountedSkrimisher = false;
                for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                     equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                     equipmentIndex++)
                    if (agent.Equipment != null 
                        && !agent.Equipment[equipmentIndex].IsEmpty 
                        && agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown 
                        && agent.MountAgent != null)
                    {
                        ismountedSkrimisher = true;
                        break;
                    }

                if (ismountedSkrimisher)
                    mountedSkirmishersList.Add(agent);
                else
                    mountedMeleeList.Add(agent);
            });

            ____rightCavalry.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                var ismountedSkrimisher = false;
                for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                     equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                     equipmentIndex++)

                    if (agent.Equipment != null 
                        && !agent.Equipment[equipmentIndex].IsEmpty 
                        && agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown 
                        && agent.MountAgent != null)
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
    }
}