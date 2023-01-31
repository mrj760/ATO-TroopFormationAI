using HarmonyLib;
using RBMAI.AiModule;
using RBMAI.AiModule.RbmSieges;
using RBMAI.AiModule.RbmTactics;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace RBMAI
{
    public class RBMAIPatcher : MBSubModuleBase
    {
        public static Harmony harmony = new Harmony("rbmai.ato");
        public static bool patched;

        protected override void OnSubModuleLoad()
        {
            DoPatching();
        }


        public static void DoPatching()
        {
            if (patched) return;
            FirstPatch(ref harmony);
            harmony.PatchAll();
            patched = true;
        }

        public static void FirstPatch(ref Harmony rbmaiHarmony)
        {
            harmony = rbmaiHarmony;
            var original = AccessTools.Method(typeof(MissionCombatantsLogic), "EarlyStart");
            var postfix = AccessTools.Method(typeof(Tactics.EarlyStartPatch), nameof(Tactics.EarlyStartPatch.Postfix));
            harmony.Patch(original, null, new HarmonyMethod(postfix));
            var original2 = AccessTools.Method(typeof(CampaignMissionComponent), "EarlyStart");
            var postfix2 = AccessTools.Method(typeof(Tactics.CampaignMissionComponentPatch), nameof(Tactics.CampaignMissionComponentPatch.Postfix));
            harmony.Patch(original2, null, new HarmonyMethod(postfix2));

            //harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            if (mission.GetMissionBehavior<SiegeArcherPoints>() != null)
            {
                mission.RemoveMissionBehavior(mission.GetMissionBehavior<SiegeArcherPoints>());
            }

            base.OnMissionBehaviorInitialize(mission);
        }
    }
}