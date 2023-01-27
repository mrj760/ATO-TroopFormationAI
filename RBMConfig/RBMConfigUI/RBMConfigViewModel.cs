// CunningLords.Interaction.CunningLordsMenuViewModel

using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RBMConfig.RBMConfigUI
{
    internal class RBMConfigViewModel : ViewModel
    {
        public TextViewModel ArmorStatusUIEnabledText { get; }
        public SelectorVM<SelectorItemVM> ArmorStatusUIEnabled { get; }

        public TextViewModel RealisticArrowArcText { get; }
        public SelectorVM<SelectorItemVM> RealisticArrowArc { get; }

        public TextViewModel PostureSystemEnabledText { get; }
        public SelectorVM<SelectorItemVM> PostureSystemEnabled { get; }

        public TextViewModel PlayerPostureMultiplierText { get; }
        public SelectorVM<SelectorItemVM> PlayerPostureMultiplier { get; }

        public TextViewModel PostureGUIEnabledText { get; }
        public SelectorVM<SelectorItemVM> PostureGUIEnabled { get; }

        public TextViewModel VanillaCombatAiText { get; }
        public SelectorVM<SelectorItemVM> VanillaCombatAi { get; }

        public TextViewModel ActiveTroopOverhaulText { get; }
        public SelectorVM<SelectorItemVM> ActiveTroopOverhaul { get; }

        public TextViewModel RangedReloadSpeedText { get; }
        public SelectorVM<SelectorItemVM> RangedReloadSpeed { get; }

        public TextViewModel PassiveShoulderShieldsText { get; }
        public SelectorVM<SelectorItemVM> PassiveShoulderShields { get; }

        public TextViewModel BetterArrowVisualsText { get; }
        public SelectorVM<SelectorItemVM> BetterArrowVisuals { get; }

        public SelectorVM<SelectorItemVM> RBMCombatEnabled { get; }

        public SelectorVM<SelectorItemVM> RBMAIEnabled { get; }

        public SelectorVM<SelectorItemVM> RBMTournamentEnabled { get; }


        public RBMConfigViewModel()
        {
            base.RefreshValues();

            var troopOverhaulOnOff = new List<string> { "Inactive", "Active (Recommended)", };
            ActiveTroopOverhaulText = new TextViewModel(new TextObject("Troop Overhaul"));
            ActiveTroopOverhaul = new SelectorVM<SelectorItemVM>(troopOverhaulOnOff, 0, null);

            var rangedReloadSpeed = new List<string> { "Vanilla", "Realistic", "Semi-realistic (Default)" };
            RangedReloadSpeedText = new TextViewModel(new TextObject("Ranged reload speed"));
            RangedReloadSpeed = new SelectorVM<SelectorItemVM>(rangedReloadSpeed, 0, null);

            var passiveShoulderShields = new List<string> { "Disabled (Default)", "Enabled" };
            PassiveShoulderShieldsText = new TextViewModel(new TextObject("Passive Shoulder Shields"));
            PassiveShoulderShields = new SelectorVM<SelectorItemVM>(passiveShoulderShields, 0, null);

            var betterArrowVisuals = new List<string> { "Disabled", "Enabled (Default)" };
            BetterArrowVisualsText = new TextViewModel(new TextObject("Better Arrow Visuals"));
            BetterArrowVisuals = new SelectorVM<SelectorItemVM>(betterArrowVisuals, 0, null);

            var armorStatusUIEnabled = new List<string> { "Disabled", "Enabled (Default)", };
            ArmorStatusUIEnabledText = new TextViewModel(new TextObject("Armor Status GUI"));
            ArmorStatusUIEnabled = new SelectorVM<SelectorItemVM>(armorStatusUIEnabled, 0, null);

            var realisticArrowArc = new List<string> { "Disabled (Default)", "Enabled", };
            RealisticArrowArcText = new TextViewModel(new TextObject("Realistic Arrow Arc"));
            RealisticArrowArc = new SelectorVM<SelectorItemVM>(realisticArrowArc, 0, null);

            ActiveTroopOverhaul.SelectedIndex = RBMConfig.troopOverhaulActive ? 1 : 0;

            switch (RBMConfig.realisticRangedReload)
            {
                case "0":
                    RangedReloadSpeed.SelectedIndex = 0;
                    break;
                case "1":
                    RangedReloadSpeed.SelectedIndex = 1;
                    break;
                case "2":
                    RangedReloadSpeed.SelectedIndex = 2;
                    break;
            }

            PassiveShoulderShields.SelectedIndex = RBMConfig.passiveShoulderShields ? 1 : 0;

            BetterArrowVisuals.SelectedIndex = RBMConfig.betterArrowVisuals ? 1 : 0;

            ArmorStatusUIEnabled.SelectedIndex = RBMConfig.armorStatusUIEnabled ? 1 : 0;

            RealisticArrowArc.SelectedIndex = RBMConfig.realisticArrowArc ? 1 : 0;

            var postureOptions = new List<string> { "Disabled", "Enabled (Default)" };
            PostureSystemEnabledText = new TextViewModel(new TextObject("Posture System"));
            PostureSystemEnabled = new SelectorVM<SelectorItemVM>(postureOptions, 0, null);

            var playerPostureMultiplierOptions = new List<string> { "1x (Default)", "1.5x", "2x" };
            PlayerPostureMultiplierText = new TextViewModel(new TextObject("Player Posture Multiplier"));
            PlayerPostureMultiplier = new SelectorVM<SelectorItemVM>(playerPostureMultiplierOptions, 0, null);

            var postureGUIOptions = new List<string> { "Disabled", "Enabled (Default)" };
            PostureGUIEnabledText = new TextViewModel(new TextObject("Posture GUI"));
            PostureGUIEnabled = new SelectorVM<SelectorItemVM>(postureGUIOptions, 0, null);

            var vanillaCombatAiOptions = new List<string> { "Disabled (Default)", "Enabled" };
            VanillaCombatAiText = new TextViewModel(new TextObject("Vanilla AI Block/Parry/Attack"));
            VanillaCombatAi = new SelectorVM<SelectorItemVM>(vanillaCombatAiOptions, 0, null);

            switch (RBMConfig.playerPostureMultiplier)
            {
                case 1f:
                    PlayerPostureMultiplier.SelectedIndex = 0;
                    break;
                case 1.5f:
                    PlayerPostureMultiplier.SelectedIndex = 1;
                    break;
                case 2f:
                    PlayerPostureMultiplier.SelectedIndex = 2;
                    break;
            }

            PostureSystemEnabled.SelectedIndex = RBMConfig.postureEnabled ? 1 : 0;

            PostureGUIEnabled.SelectedIndex = RBMConfig.postureGUIEnabled ? 1 : 0;

            VanillaCombatAi.SelectedIndex = RBMConfig.vanillaCombatAi ? 1 : 0;

            var rbmCombatEnabledOptions = new List<string> { "Disabled", "Enabled (Default)" };
            RBMCombatEnabled = new SelectorVM<SelectorItemVM>(rbmCombatEnabledOptions, 0, null);

            var rbmAiEnabledOptions = new List<string> { "Disabled", "Enabled (Default)" };
            RBMAIEnabled = new SelectorVM<SelectorItemVM>(rbmAiEnabledOptions, 0, null);

            var rbmTournamentEnabledOptions = new List<string> { "Disabled", "Enabled (Default)" };
            RBMTournamentEnabled = new SelectorVM<SelectorItemVM>(rbmTournamentEnabledOptions, 0, null);

            RBMCombatEnabled.SelectedIndex = RBMConfig.rbmCombatEnabled ? 1 : 0;

            RBMAIEnabled.SelectedIndex = RBMConfig.rbmAiEnabled ? 1 : 0;

            RBMTournamentEnabled.SelectedIndex = RBMConfig.rbmTournamentEnabled ? 1 : 0;

        }
    }
}
