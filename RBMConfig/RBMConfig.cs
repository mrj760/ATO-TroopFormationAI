using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace RBMConfig
{
    public static class RBMConfig
    {
        public static XmlDocument xmlConfig = new XmlDocument();
        //modules
        public static bool rbmTournamentEnabled = true;
        public static bool rbmAiEnabled = true;
        public static bool rbmCombatEnabled = true;
        public static bool developerMode = false;
        //RBMAI
        public static bool postureEnabled = true;
        public static float playerPostureMultiplier = 1f;
        public static bool postureGUIEnabled = true;
        public static bool vanillaCombatAi = false;
        //RBMCombat
        public static bool realisticArrowArc = false;
        public static bool armorStatusUIEnabled = true;
        public static float armorMultiplier = 2f;
        public static bool armorPenetrationMessage = false;
        public static bool betterArrowVisuals = true;
        public static bool passiveShoulderShields = false;
        public static bool troopOverhaulActive = true;
        public static string realisticRangedReload = "1";
        public static float maceBluntModifier = 1f;
        public static float armorThresholdModifier = 1f;
        public static float bluntTraumaBonus = 1f;

        public static void LoadConfig()
        {
            string defaultConfigFilePath = TaleWorlds.Engine.Utilities.GetFullModulePath("RBM") + "DefaultConfigDONOTEDIT.xml";
            string configFolderPath = Utilities.GetConfigFolderPath();
            string configFilePath = Utilities.GetConfigFilePath();

            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }

            if (File.Exists(configFilePath))
            {
                xmlConfig.Load(configFilePath);
            }
            else
            {
                File.Copy(defaultConfigFilePath, configFilePath);
                xmlConfig.Load(configFilePath);
            }

            parseXmlConfig();
        }

        public static void parseXmlConfig()
        {
            if(xmlConfig.SelectSingleNode("/Config/DeveloperMode") != null)
            {
                developerMode = true;
            }
            //modules
            rbmTournamentEnabled = false;
            rbmAiEnabled = true;
            rbmCombatEnabled = false;
            //RBMAI
            postureEnabled = false;
            postureGUIEnabled = false;
            vanillaCombatAi = false;
            //RBMCombat
            if (xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorStatusUIEnabled") != null)
            {
                armorStatusUIEnabled = xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorStatusUIEnabled").InnerText.Equals("1");
            }
            else
            {
                var ArmorStatusUIEnabled = xmlConfig.CreateNode(XmlNodeType.Element, "ArmorStatusUIEnabled", null);
                ArmorStatusUIEnabled.InnerText = "1";
                xmlConfig.SelectSingleNode("/Config/RBMCombat/Global")?.AppendChild(ArmorStatusUIEnabled);
            }

            if (xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/RealisticArrowArc") != null)
            {
                realisticArrowArc = xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/RealisticArrowArc").InnerText.Equals("1");
            }
            else
            {
                var RealisticArrowArc = xmlConfig.CreateNode(XmlNodeType.Element, "RealisticArrowArc", null);
                RealisticArrowArc.InnerText = "0";
                xmlConfig.SelectSingleNode("/Config/RBMCombat/Global")?.AppendChild(RealisticArrowArc);
            }

            saveXmlConfig();
        }

        public static void setInnerTextBoolean(XmlNode xmlConfig, bool value)
        {
            xmlConfig.InnerText = value ? "1" : "0";
        }

        public static void setInnerText(XmlNode xmlConfig, string value)
        {
            xmlConfig.InnerText = value;
        }

        public static void saveXmlConfig()
        {
            //modules
            if (xmlConfig.SelectSingleNode("/Config/DeveloperMode") != null && developerMode)
            {
                setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/DeveloperMode"), developerMode);
            }
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMTournament/Enabled"), rbmTournamentEnabled);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMAI/Enabled"), rbmAiEnabled);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Enabled"), rbmCombatEnabled);
            //RBMAI
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMAI/PostureEnabled"), postureEnabled);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMAI/PostureGUIEnabled"), postureGUIEnabled);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMAI/VanillaCombatAi"), vanillaCombatAi);

            switch (playerPostureMultiplier)
            {
                case 1f:
                    {
                        setInnerText(xmlConfig.SelectSingleNode("/Config/RBMAI/PlayerPostureMultiplier"), "0");
                        break;
                    }
                case 1.5f:
                    {
                        setInnerText(xmlConfig.SelectSingleNode("/Config/RBMAI/PlayerPostureMultiplier"), "1");

                        break;
                    }
                case 2f:
                    {
                        setInnerText(xmlConfig.SelectSingleNode("/Config/RBMAI/PlayerPostureMultiplier"), "2");
                        break;
                    }
            }

            //RBMCombat
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorStatusUIEnabled"), armorStatusUIEnabled);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/RealisticArrowArc"), realisticArrowArc);
            setInnerText(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorMultiplier"), armorMultiplier.ToString());
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorPenetrationMessage"), armorPenetrationMessage);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/BetterArrowVisuals"), betterArrowVisuals);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/PassiveShoulderShields"), passiveShoulderShields);
            setInnerTextBoolean(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/TroopOverhaulActive"), troopOverhaulActive);
            setInnerText(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/RealisticRangedReload"), realisticRangedReload.ToString());
            setInnerText(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/MaceBluntModifier"), maceBluntModifier.ToString());
            setInnerText(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/ArmorThresholdModifier"), armorThresholdModifier.ToString());
            setInnerText(xmlConfig.SelectSingleNode("/Config/RBMCombat/Global/BluntTraumaBonus"), bluntTraumaBonus.ToString());
            

            xmlConfig.Save(Utilities.GetConfigFilePath());

        }

    }
}
