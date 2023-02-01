using HarmonyLib;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace RBMAI
{
    public static class RBMAIPatcher
    {
        public static Harmony harmony;
        public static bool patched;

        public static void DoPatching()
        {
            if (patched) return;

            harmony.PatchAll();
            patched = true;
        }

        public static void FirstPatch(ref Harmony rbmaiHarmony)
        {
            harmony = rbmaiHarmony;
            var original = AccessTools.Method(typeof(MissionCombatantsLogic), "EarlyStart");
            var postfix = AccessTools.Method(typeof(Tactics.EarlyStartPatch), nameof(Tactics.EarlyStartPatch.Postfix));
            rbmaiHarmony.Patch(original, null, new HarmonyMethod(postfix));
            var original2 = AccessTools.Method(typeof(CampaignMissionComponent), "EarlyStart");
            var postfix2 = AccessTools.Method(typeof(Tactics.CampaignMissionComponentPatch),
                nameof(Tactics.CampaignMissionComponentPatch.Postfix));
            rbmaiHarmony.Patch(original2, null, new HarmonyMethod(postfix2));
        }
    }
}