using HarmonyLib;
using RBMAI;
using System;
using RBMAI.AiModule.SiegeArcherPoints;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace RBM
{
    public static class HarmonyModules
    {
        public static Harmony rbmaiHarmony = new Harmony("com.rbmai");
        public static Harmony rbmHarmony = new Harmony("com.rbmmain");
    }

    public class SubModule : MBSubModuleBase
    {
        public static string ModuleId = "RBM";


        public static void ApplyHarmonyPatches()
        {
            UnpatchAllRBM();
            HarmonyModules.rbmHarmony.PatchAll();

            RBMAiPatcher.FirstPatch(ref HarmonyModules.rbmaiHarmony);

        }

        public static void UnpatchAllRBM()
        {
            RBMAiPatcher.patched = false;
            HarmonyModules.rbmHarmony.UnpatchAll(HarmonyModules.rbmHarmony.Id);
            HarmonyModules.rbmaiHarmony.UnpatchAll(HarmonyModules.rbmaiHarmony.Id);
        }

        protected override void OnSubModuleLoad()
        {
            RBMConfig.RBMConfig.LoadConfig();
            base.OnSubModuleLoad();
        }

        protected override void OnApplicationTick(float dt)
        {
            if (Mission.Current == null)
            {
                return;
            }
            try
            {
                if (ScreenManager.TopScreen != null 
                    && (Mission.Current.IsFieldBattle 
                        || Mission.Current.IsSiegeBattle 
                        || Mission.Current.SceneName.Contains("arena") 
                        || (MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.IsHideoutBattle)))
                {
                    var missionScreen = ScreenManager.TopScreen as MissionScreen;

                    if (missionScreen?.InputManager != null && missionScreen.InputManager.IsControlDown() && missionScreen.InputManager.IsKeyPressed(InputKey.V))
                    {
                        Mission.Current.SetFastForwardingFromUI(!Mission.Current.IsFastForward);
                        InformationManager.DisplayMessage(new InformationMessage("Vroom = " + Mission.Current.IsFastForward, Color.FromUint(4282569842u)));
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            ApplyHarmonyPatches();
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