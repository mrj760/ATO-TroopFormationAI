using HarmonyLib;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmTactics
{
    [HarmonyPatch(typeof(TacticDefensiveEngagement))]
    internal class TacticDefensiveEngagementPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("HasBattleBeenJoined")]
        private static bool PrefixHasBattleBeenJoined(Formation ____mainInfantry, bool ____hasBattleBeenJoined,
            ref bool __result)
        {
            __result = Utilities.HasBattleBeenJoined(____mainInfantry, ____hasBattleBeenJoined);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Defend")]
        private static void PostfixDefend(ref Formation ____archers, ref Formation ____mainInfantry,
            ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
        {
            ____mainInfantry?.AI.SetBehaviorWeight<BehaviorRegroup>(1.75f);

            FormationAI.BehaviorSide newside;


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
            }

            if (____leftCavalry != null)
            {
                newside = FormationAI.BehaviorSide.Left;

                ____leftCavalry.AI.ResetBehaviorWeights();
                ____leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = newside;
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
        [HarmonyPatch("Engage")]
        private static void PostfixAttack(ref Formation ____archers, ref Formation ____mainInfantry,
                ref Formation ____rightCavalry, ref Formation ____leftCavalry, ref Formation ____rangedCavalry)
        {

            if (____archers != null)
            {
                ____archers.AI.ResetBehaviorWeights();
                ____archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                ____archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                ____archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
            }

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

            Utilities.FixCharge(ref ____mainInfantry);
        }
    }
}