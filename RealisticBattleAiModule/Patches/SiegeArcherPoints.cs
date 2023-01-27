﻿// ScatterAroundExpanded.ScatterAroundExpanded

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RBMAI.Patches
{
    public class SiegeArcherPoints : MissionView
    {
        public bool firstTime = true;
        public bool xmlExists = false;
        public static bool isEditingXml = true;
        public bool editingWarningDisplayed = false;
        public bool isFirstTimeLoading = true;

        public override void OnMissionScreenTick(float dt)
        {
            //((MissionView)this).OnMissionScreenTick(dt);
            if (isFirstTimeLoading && Mission.Current != null && Mission.Current.IsSiegeBattle)
            {
                XmlDocument xmlDocument = new XmlDocument();
                if (File.Exists(RBMAI.Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml"))
                {
                    xmlDocument.Load(RBMAI.Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml");
                    xmlExists = true;
                }

                if (xmlExists)
                {
                    List<GameEntity> gameEntities = new List<GameEntity>();
                    Mission.Current.Scene.GetEntities(ref gameEntities);
                    StrategicArea strategicArea;
                    List<StrategicArea> _strategicAreas = (from amo in Mission.Current.ActiveMissionObjects
                                                           where (strategicArea = amo as StrategicArea) != null && strategicArea.IsActive && strategicArea.IsUsableBy(BattleSideEnum.Defender)
                                                           && (strategicArea.GameEntity.GetOldPrefabName().Contains("archer_position") || strategicArea.GameEntity.GetOldPrefabName().Contains("strategic_archer_point"))
                                                           select amo as StrategicArea).ToList();
                    foreach (StrategicArea _strategicArea in _strategicAreas)
                    {
                        Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(_strategicArea);
                        _strategicArea.GameEntity.RemoveAllChildren();
                        _strategicArea.GameEntity.Remove(1);
                    }
                    foreach (GameEntity g2 in gameEntities)
                    {
                        if (g2.HasScriptOfType<StrategicArea>() && (!g2.HasTag("PlayerStratPoint") & !g2.HasTag("BeerMarkerPlayer")) && g2.GetOldPrefabName() == "strategic_archer_point")
                        {
                            if (g2.GetFirstScriptOfType<StrategicArea>().IsUsableBy(BattleSideEnum.Defender))
                            {
                                Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(g2.GetFirstScriptOfType<StrategicArea>());
                                g2.RemoveAllChildren();
                                g2.Remove(1);
                            }
                        }
                    }
                    List<GameEntity> ListBase = Mission.Current.Scene.FindEntitiesWithTag("BeerMarkerBase").ToList();
                    foreach (GameEntity b in ListBase)
                    {
                        b.RemoveAllChildren();
                        b.Remove(1);
                    }
                    List<GameEntity> ListG = Mission.Current.Scene.FindEntitiesWithTag("PlayerStratPoint").ToList();
                    List<GameEntity> ListArrow = Mission.Current.Scene.FindEntitiesWithTag("BeerMarkerPlayer").ToList();
                    foreach (GameEntity g in ListG)
                    {
                        Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(g.GetFirstScriptOfType<StrategicArea>());
                        g.RemoveAllChildren();
                        g.Remove(1);
                    }
                    foreach (GameEntity h in ListArrow)
                    {
                        h.RemoveAllChildren();
                        h.Remove(1);
                    }

                    foreach (XmlNode pointNode in xmlDocument.SelectSingleNode("/points").ChildNodes)
                    {
                        double[] parsed = Array.ConvertAll(pointNode.InnerText.Split(new[] { ',', }, StringSplitOptions.RemoveEmptyEntries), Double.Parse);

                        //WorldFrame worldPos = new WorldFrame(new Mat3((float)parsed[0], (float)parsed[1], (float)parsed[2], (float)parsed[3],
                        //	(float)parsed[4], (float)parsed[5], (float)parsed[6], (float)parsed[7], (float)parsed[8]), new WorldPosition(Mission.Current.Scene, new Vec3((float)parsed[9], (float)parsed[10], (float)parsed[11])));

                        MatrixFrame matFrame = new MatrixFrame((float)parsed[0], (float)parsed[1], (float)parsed[2], (float)parsed[3],
                            (float)parsed[4], (float)parsed[5], (float)parsed[6], (float)parsed[7], (float)parsed[8], (float)parsed[9], (float)parsed[10], (float)parsed[11]);

                        GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, "strategic_archer_point", matFrame);
                        gameEntity.SetMobility(GameEntity.Mobility.dynamic);
                        gameEntity.AddTag("PlayerStratPoint");
                        gameEntity.SetVisibilityExcludeParents(visible: true);
                        StrategicArea firstScriptOfType = gameEntity.GetFirstScriptOfType<StrategicArea>();
                        firstScriptOfType.InitializeAutogenerated(1f, 1, Mission.Current.Teams.Defender.Side);

                        GameEntity BeerMark = GameEntity.Instantiate(Mission.Current.Scene, "arrow_new_icon", matFrame);
                        BeerMark.AddTag("BeerMarkerPlayer");
                        BeerMark.SetVisibilityExcludeParents(visible: false);
                        BeerMark.GetGlobalScale().Normalize();
                        BeerMark.SetMobility(GameEntity.Mobility.dynamic);
                        foreach (Team team in Mission.Current.Teams)
                        {
                            if (team.IsDefender)
                            {
                                team.TeamAI.AddStrategicArea(firstScriptOfType);
                            }
                        }

                    }
                }

                isFirstTimeLoading = false;
                return;
            }

            if (firstTime && Mission.Current != null && Mission.Current.IsSiegeBattle && Mission.Current.PlayerTeam.IsDefender && Mission.Current.Mode != MissionMode.Deployment)
            {

                if (firstTime)
                {
                    firstTime = false;
                    return;
                }
                InformationManager.DisplayMessage(new InformationMessage("!!! DEVELOPER MODE, NORMAL USER SHOULDN'T SEE THIS MESSAGE"));
                List<GameEntity> gameEntities = new List<GameEntity>();
                Mission.Current.Scene.GetEntities(ref gameEntities);

                XmlDocument xmlDocument = new XmlDocument();
                if (File.Exists(RBMAI.Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml"))
                {
                    xmlDocument.Load(RBMAI.Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml");
                    xmlExists = true;
                }
                else
                {
                    XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);

                    XmlElement root = xmlDocument.DocumentElement;
                    xmlDocument.InsertBefore(xmlDeclaration, root);
                    xmlDocument.AppendChild(xmlDocument.CreateElement(string.Empty, "points", string.Empty));
                }

                XmlNode pointNode = xmlDocument.SelectSingleNode("/points");
                if (pointNode == null)
                {
                    pointNode = xmlDocument.CreateElement(string.Empty, "points", string.Empty);
                }

                pointNode.RemoveAll();

                foreach (GameEntity g in gameEntities)
                {
                    if (g.HasScriptOfType<StrategicArea>() && g.GetFirstScriptOfType<StrategicArea>().IsUsableBy(BattleSideEnum.Defender) && g.GetOldPrefabName() == "strategic_archer_point")
                    {
                        XmlElement newPointNode = xmlDocument.CreateElement(string.Empty, "point", string.Empty);
                        string stringToBeSaved = "";
                        stringToBeSaved += g.GetGlobalFrame().rotation.s.x + "," + g.GetGlobalFrame().rotation.s.y + "," + g.GetGlobalFrame().rotation.s.z + ",";
                        stringToBeSaved += g.GetGlobalFrame().rotation.f.x + "," + g.GetGlobalFrame().rotation.f.y + "," + g.GetGlobalFrame().rotation.f.z + ",";
                        stringToBeSaved += g.GetGlobalFrame().rotation.u.x + "," + g.GetGlobalFrame().rotation.u.y + "," + g.GetGlobalFrame().rotation.u.z + ",";
                        stringToBeSaved += g.GetGlobalFrame().origin.x + "," + g.GetGlobalFrame().origin.y + "," + g.GetGlobalFrame().origin.z;
                        newPointNode.InnerText = stringToBeSaved;

                        pointNode.AppendChild(newPointNode);
                    }
                }
                xmlDocument.Save(RBMAI.Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml");
                firstTime = false;
            }

        }

        [HarmonyPatch(typeof(ArrangementOrder))]
        [HarmonyPatch("GetCloseStrategicAreas")]
        public class GetCloseStrategicAreasPatch
        {
            public static bool Prefix(ref IEnumerable<StrategicArea> __result, Formation formation, ref ArrangementOrder __instance)
            {
                if (formation.Team?.TeamAI == null)
                {
                    __result = new List<StrategicArea>();
                    return false;
                }
                __result = formation.Team.TeamAI.GetStrategicAreas().Where(delegate (StrategicArea sa)
                {
                    float customDistanceToCheck = 150f;
                    if (sa != null && formation != null && sa.GameEntity != null && sa.GameEntity.GlobalPosition != null && sa.IsUsableBy(formation.Team.Side))
                    {
                        if (sa.IgnoreHeight)
                        {
                            if (MathF.Abs(sa.GameEntity.GlobalPosition.x - formation.OrderPosition.X) <= customDistanceToCheck)
                            {
                                return MathF.Abs(sa.GameEntity.GlobalPosition.y - formation.OrderPosition.Y) <= customDistanceToCheck;
                            }
                            return false;
                        }
                        WorldPosition worldPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
                        Vec3 targetPoint = sa.GameEntity.GlobalPosition;
                        return worldPosition.DistanceSquaredWithLimit(in targetPoint, customDistanceToCheck * customDistanceToCheck + 1E-05f) < customDistanceToCheck * customDistanceToCheck;
                    }

                    return false;
                });

                return false;
            }
        }
    }
}