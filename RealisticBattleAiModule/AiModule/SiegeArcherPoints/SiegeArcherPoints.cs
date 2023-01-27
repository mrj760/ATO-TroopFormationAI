// ScatterAroundExpanded.ScatterAroundExpanded

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

namespace RBMAI.AiModule.SiegeArcherPoints
{
    public class SiegeArcherPoints : MissionView
    {
        public static bool devmode = false;
        public static bool isEditingXml = true;
        public bool editingWarningDisplayed = false;

        public bool firstTime = true;
        public bool isFirstTimeLoading = true;

        public override void OnMissionScreenTick(float dt)
        {
            if (Mission.Current == null || !Mission.Current.IsSiegeBattle) return;

            var xmlName = Utilities.GetSiegeArcherPointsPath() + Mission.Current.Scene.GetName() + "_" + Mission.Current.Scene.GetUpgradeLevelMask() + ".xml";

            List<GameEntity> gameEntities;
            XmlDocument xmlDocument;

            if (isFirstTimeLoading)
            {
                xmlDocument = new XmlDocument();

                if (File.Exists(xmlName))
                {
                    xmlDocument.Load(xmlName);
                }
                else
                {
                    isFirstTimeLoading = false;
                    return;
                }

                gameEntities = new List<GameEntity>();
                Mission.Current.Scene.GetEntities(ref gameEntities);

                StrategicArea strategicArea;

                var _strategicAreas =
                    from amo in Mission.Current.ActiveMissionObjects
                    where (strategicArea = amo as StrategicArea) != null
                          && strategicArea.IsActive
                          && strategicArea.IsUsableBy(BattleSideEnum.Defender)
                          && (strategicArea.GameEntity.GetOldPrefabName().Contains("archer_position")
                              || strategicArea.GameEntity.GetOldPrefabName().Contains("strategic_archer_point"))
                    select amo as StrategicArea;

                foreach (var _strategicArea in _strategicAreas)
                {
                    Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(_strategicArea);
                    _strategicArea.GameEntity.RemoveAllChildren();
                    _strategicArea.GameEntity.Remove(1);
                }

                foreach (var g2 in gameEntities)
                {
                    if (g2.HasScriptOfType<StrategicArea>() &&
                        !g2.HasTag("PlayerStratPoint") & !g2.HasTag("BeerMarkerPlayer") &&
                        g2.GetOldPrefabName() == "strategic_archer_point")
                        if (g2.GetFirstScriptOfType<StrategicArea>().IsUsableBy(BattleSideEnum.Defender))
                        {
                            Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(
                                g2.GetFirstScriptOfType<StrategicArea>());
                            g2.RemoveAllChildren();
                            g2.Remove(1);
                        }
                }

                var ListBase = Mission.Current.Scene.FindEntitiesWithTag("BeerMarkerBase");
                foreach (var b in ListBase)
                {
                    b.RemoveAllChildren();
                    b.Remove(1);
                }

                var ListG = Mission.Current.Scene.FindEntitiesWithTag("PlayerStratPoint");
                var ListArrow = Mission.Current.Scene.FindEntitiesWithTag("BeerMarkerPlayer");
                foreach (var g in ListG)
                {
                    Mission.Current.Teams.Defender.TeamAI.RemoveStrategicArea(
                        g.GetFirstScriptOfType<StrategicArea>());
                    g.RemoveAllChildren();
                    g.Remove(1);
                }

                foreach (var h in ListArrow)
                {
                    h.RemoveAllChildren();
                    h.Remove(1);
                }

                foreach (XmlNode pointNode in xmlDocument.SelectSingleNode("/points").ChildNodes)
                {
                    var parsed =
                        Array.ConvertAll(
                            pointNode.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                            double.Parse);

                    var matFrame = new MatrixFrame((float)parsed[0], (float)parsed[1], (float)parsed[2],
                        (float)parsed[3],
                        (float)parsed[4], (float)parsed[5], (float)parsed[6], (float)parsed[7], (float)parsed[8],
                        (float)parsed[9], (float)parsed[10], (float)parsed[11]);

                    var gameEntity =
                        GameEntity.Instantiate(Mission.Current.Scene, "strategic_archer_point", matFrame);
                    gameEntity.SetMobility(GameEntity.Mobility.dynamic);
                    gameEntity.AddTag("PlayerStratPoint");
                    gameEntity.SetVisibilityExcludeParents(true);
                    var firstScriptOfType = gameEntity.GetFirstScriptOfType<StrategicArea>();
                    firstScriptOfType.InitializeAutogenerated(1f, 1, Mission.Current.Teams.Defender.Side);

                    var BeerMark = GameEntity.Instantiate(Mission.Current.Scene, "arrow_new_icon", matFrame);
                    BeerMark.AddTag("BeerMarkerPlayer");
                    BeerMark.SetVisibilityExcludeParents(false);
                    BeerMark.GetGlobalScale().Normalize();
                    BeerMark.SetMobility(GameEntity.Mobility.dynamic);
                    foreach (var team in Mission.Current.Teams)
                        if (team.IsDefender)
                            team.TeamAI.AddStrategicArea(firstScriptOfType);
                }

                isFirstTimeLoading = false;
                return;
            }

            if (!firstTime || !Mission.Current.PlayerTeam.IsDefender || Mission.Current.Mode == MissionMode.Deployment) 
                return;

            gameEntities = new List<GameEntity>();
            Mission.Current.Scene.GetEntities(ref gameEntities);

            xmlDocument = new XmlDocument();
            if (File.Exists(xmlName))
            {
                xmlDocument.Load(xmlName);
            }
            else
            {
                var xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);

                var root = xmlDocument.DocumentElement;
                xmlDocument.InsertBefore(xmlDeclaration, root);
                xmlDocument.AppendChild(xmlDocument.CreateElement(string.Empty, "points", string.Empty));
            }

            var node = xmlDocument.SelectSingleNode("/points") ?? xmlDocument.CreateElement(string.Empty, "points", string.Empty);

            node.RemoveAll();

            foreach (var g in gameEntities)
                if (g.HasScriptOfType<StrategicArea>() &&
                    g.GetFirstScriptOfType<StrategicArea>().IsUsableBy(BattleSideEnum.Defender) &&
                    g.GetOldPrefabName() == "strategic_archer_point")
                {

                    var newPointNode = xmlDocument.CreateElement(string.Empty, "point", string.Empty);

                    var grot = g.GetGlobalFrame().rotation;
                    var gorig = g.GetGlobalFrame().origin;

                    newPointNode.InnerText =
                        grot.s.x + "," + g.GetGlobalFrame().rotation.s.y + "," + grot.s.z + ","
                        + grot.f.x + "," + g.GetGlobalFrame().rotation.f.y + "," + grot.f.z + ","
                        + grot.u.x + "," + g.GetGlobalFrame().rotation.u.y + "," + grot.u.z + ","
                        + gorig.x + "," + gorig.y + "," + gorig.z;

                    node.AppendChild(newPointNode);
                }

            xmlDocument.Save(xmlName);
            firstTime = false;

        }


        [HarmonyPatch(typeof(ArrangementOrder))]
        [HarmonyPatch("GetCloseStrategicAreas")]
        private class GetCloseStrategicAreasPatch
        {
            private static bool Prefix(ref IEnumerable<StrategicArea> __result, Formation formation,
                ref ArrangementOrder __instance)
            {
                if (formation.Team?.TeamAI == null)
                {
                    __result = new List<StrategicArea>();
                    return false;
                }

                __result = formation.Team.TeamAI.GetStrategicAreas().Where(delegate(StrategicArea sa)
                {
                    const float customDistanceToCheck = 150f;

                    if (sa == null || sa.GameEntity == null || !sa.IsUsableBy(formation.Team.Side)) 
                        return false;

                    if (sa.IgnoreHeight)
                    {
                        if (MathF.Abs(sa.GameEntity.GlobalPosition.x - formation.OrderPosition.X) <=
                            customDistanceToCheck)
                            return MathF.Abs(sa.GameEntity.GlobalPosition.y - formation.OrderPosition.Y) <=
                                   customDistanceToCheck;
                        return false;
                    }

                    var worldPosition =
                        formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
                    var targetPoint = sa.GameEntity.GlobalPosition;

                    return worldPosition.DistanceSquaredWithLimit(
                        in targetPoint, customDistanceToCheck * customDistanceToCheck + 1E-05f) 
                           < customDistanceToCheck * customDistanceToCheck;

                });
                return false;
            }
        }
    }
}