using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.ObjectSystem;

namespace RBM
{
    class XmlLoadingPatches
    {

        [HarmonyPatch(typeof(MBObjectManager))]
        [HarmonyPatch("MergeTwoXmls")]
        class MergeTwoXmlsPatch
        {
            static bool Prefix(ref XmlDocument xmlDocument1, ref XmlDocument xmlDocument2, ref XmlDocument __result)
            {
                return true;
                XDocument originalXml = MBObjectManager.ToXDocument(xmlDocument1);
                XDocument mergedXml = MBObjectManager.ToXDocument(xmlDocument2);

                var nodesToRemoveArray = new List<XElement>();
                if(xmlDocument2.BaseURI.Contains("RBMCombat"))
                {
                    __result = MBObjectManager.ToXmlDocument(originalXml);
                    return false;
                }
                if (xmlDocument2.BaseURI.Contains("unit_overhaul"))
                {
                    __result = MBObjectManager.ToXmlDocument(originalXml);
                    return false;
                }

                if (!RBMConfig.RBMConfig.rbmCombatEnabled) return true;

                if (originalXml.Root != null)
                {
                    foreach (XElement origNode in originalXml.Root.Elements())
                    {
                        if (origNode.Name == "ItemModifier" && xmlDocument2.BaseURI.Contains("RBM"))
                        {
                            nodesToRemoveArray.AddRange(
                                from mergedNode 
                                    in mergedXml.Root.Elements() 
                                where mergedNode.Name == "ItemModifier"
                                where origNode.Attribute("id")?.Value == mergedNode.Attribute("id")?.Value 
                                      && origNode.Attribute("name")?.Value ==mergedNode.Attribute("name")?.Value 
                                select origNode);
                        }

                        if (origNode.Name == "CraftedItem" && xmlDocument2.BaseURI.Contains("RBM"))
                        {
                            nodesToRemoveArray.AddRange(
                                from mergedNode 
                                    in mergedXml.Root.Elements() 
                                where mergedNode.Name == "CraftedItem" 
                                where origNode.Attribute("id")?.Value == mergedNode.Attribute("id")?.Value 
                                select origNode);
                        }

                        if (origNode.Name == "Item" && xmlDocument2.BaseURI.Contains("RBM"))
                        {
                            foreach (XElement mergedNode in mergedXml.Root.Elements())
                            {
                                if (mergedNode.Name != "Item") continue;

                                if (origNode.Attribute("id").Value.Equals(mergedNode.Attribute("id").Value))
                                {
                                    nodesToRemoveArray.Add(origNode);
                                }

                                if (RBMConfig.RBMConfig.betterArrowVisuals &&
                                    (mergedNode.Attribute("Type").Value.Equals("Arrows") ||
                                     mergedNode.Attribute("Type").Value.Equals("Bolts")))
                                {
                                    mergedNode.Attribute("flying_mesh").Value =
                                        mergedNode.Attribute("mesh").Value;
                                }
                            }
                        }

                        if (origNode.Name != "NPCCharacter" || !xmlDocument2.BaseURI.Contains("RBM")) continue;
                        {
                            foreach (XElement nodeEquip in origNode.Elements())
                            {
                                if (nodeEquip.Name != "Equipments") continue;

                                foreach (XElement nodeEquipRoster in nodeEquip.Elements())
                                {
                                    if (nodeEquipRoster.Name != "EquipmentRoster") continue;

                                    foreach (XElement mergedNode in mergedXml.Root.Elements())
                                    {
                                        if (origNode.Attribute("id")?.Value != mergedNode.Attribute("id")?.Value) continue;

                                        foreach (XElement mergedNodeEquip in mergedNode.Elements())
                                        {
                                            if (mergedNodeEquip.Name != "Equipments") continue;

                                            foreach (XElement mergedNodeRoster in mergedNodeEquip
                                                         .Elements())
                                            {
                                                if (mergedNodeRoster.Name != "EquipmentRoster") continue;

                                                if (!nodesToRemoveArray.Contains(origNode))
                                                {
                                                    nodesToRemoveArray.Add(origNode);
                                                }

                                                foreach (XElement equipmentNode in
                                                         mergedNodeRoster.Elements())
                                                {
                                                    if (equipmentNode.Name != "equipment") continue;

                                                    if (equipmentNode.Attribute("id") !=
                                                        null && equipmentNode
                                                            .Attribute("id").Value
                                                            .Contains("shield_shoulder") &&
                                                        !RBMConfig.RBMConfig
                                                            .passiveShoulderShields)
                                                    {
                                                        equipmentNode.Attribute("id")
                                                            .Value = equipmentNode
                                                            .Attribute("id").Value
                                                            .Replace("shield_shoulder",
                                                                "shield");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (nodesToRemoveArray.Count > 0)
                    {
                        foreach (XElement node in nodesToRemoveArray)
                        {
                            node.Remove();
                        }
                    }

                    originalXml.Root.Add(mergedXml.Root.Elements());
                }

                __result = MBObjectManager.ToXmlDocument(originalXml);
                return false;
            }
        }

    }
}
