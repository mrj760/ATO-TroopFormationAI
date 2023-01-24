using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Core.ItemObject;

namespace RBMAI
{
    public static class Utilities
    {
        public static bool HasBattleBeenJoined(Formation mainInfantry, bool hasBattleBeenJoined,
            float battleJoinRange = 75f)
        {
            var isOnlyCavReamining = CheckIfOnlyCavRemaining(mainInfantry);
            if (isOnlyCavReamining) return true;

            if (mainInfantry == null) return true;


            if (FormationFightingInMelee(mainInfantry, 0.35f)) return true;

            if (mainInfantry.CountOfUnits <= 0
                || mainInfantry.QuerySystem.ClosestEnemyFormation?.Formation == null)
                return true;

            var enemyForamtion = FindSignificantEnemy(mainInfantry, true, true, false, false, false);

            if (enemyForamtion == null) return true;

            var distance =
                mainInfantry.QuerySystem.MedianPosition.AsVec2.Distance(
                    enemyForamtion.QuerySystem.MedianPosition.AsVec2) + mainInfantry.Depth / 2f +
                enemyForamtion.Depth / 2f;
            return distance <= battleJoinRange + (hasBattleBeenJoined ? 5f : 0f);
        }

        public static void FixCharge(ref Formation formation)
        {
            if (formation == null) return;

            formation.AI.ResetBehaviorWeights();
            formation.AI.SetBehaviorWeight<BehaviorCharge>(1f);
        }

        public static bool CheckIfMountedSkirmishFormation(Formation formation, float desiredRatio)
        {
            if (formation == null || !formation.QuerySystem.IsCavalryFormation)
                return false;

            var ratio = 0f;
            var mountedSkirmishersCount = 0;
            var countedUnits = 0;
            formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                var ismountedSkrimisher = false;
                if (ratio <= desiredRatio && countedUnits / (float)formation.CountOfUnits <= desiredRatio)
                    for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                         equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                         equipmentIndex++)
                    {
                        if (agent.Equipment == null || agent.Equipment[equipmentIndex].IsEmpty)
                            continue;

                        if (agent.MountAgent != null
                            && agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown
                            && agent.Equipment[equipmentIndex].Amount > 0)
                        {
                            ismountedSkrimisher = true;
                            break;
                        }
                    }

                if (ismountedSkrimisher) mountedSkirmishersCount++;
                countedUnits++;
                ratio = mountedSkirmishersCount / (float)formation.CountOfUnits;
            });

            return ratio > desiredRatio;
        }

        public static bool CheckIfTwoHandedPolearmInfantry(Agent agent)
        {
            for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                 equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                 equipmentIndex++)
            {
                if (agent.Equipment == null || agent.Equipment[equipmentIndex].IsEmpty)
                    continue;

                switch (agent.Equipment[equipmentIndex].Item.PrimaryWeapon.WeaponClass)
                {
                    case WeaponClass.TwoHandedPolearm:
                    case WeaponClass.TwoHandedMace:
                    case WeaponClass.TwoHandedSword:
                    case WeaponClass.TwoHandedAxe:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        public static int GetHarnessTier(Agent agent)
        {
            var equipmentElement = agent.SpawnEquipment[EquipmentIndex.HorseHarness];

            return agent.MountAgent == null
                   || agent.SpawnEquipment == null
                   || equipmentElement.Item == null
                   || equipmentElement.Item.Effectiveness >= 50f
                ? 10
                : 1;
        }

        public static Agent GetCorrectTarget(Agent agent)
        {
            var formation = agent?.Formation;
            if (formation == null) return null;

            if ((!formation.QuerySystem.IsInfantryFormation && !formation.QuerySystem.IsRangedFormation)
                || formation.GetReadonlyMovementOrderReference().OrderType != OrderType.ChargeWithTarget)
                return null;

            var formations = FindSignificantFormations(formation);
            return formations.Count > 0 ? NearestAgentFromMultipleFormations(agent.Position.AsVec2, formations) : null;
        }


        public static Agent NearestAgentFromFormation(Vec2 unitPosition, Formation targetFormation)
        {
            Agent targetAgent = null;
            var distance = 10000f;
            targetFormation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                var newDist = unitPosition.Distance(agent.GetWorldPosition().AsVec2);
                if (newDist < distance)
                {
                    targetAgent = agent;
                    distance = newDist;
                }
            });
            return targetAgent;
        }

        public static Agent NearestAgentFromMultipleFormations(Vec2 unitPosition, List<Formation> formations)
        {
            Agent targetAgent = null;
            var distance = 10000f;
            foreach (var formation in formations.ToList())
                formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                {
                    if (agent.IsAIControlled)
                    {
                        if (!agent.IsRunningAway)
                        {
                            var newDist = unitPosition.Distance(agent.GetWorldPosition().AsVec2);
                            if (newDist < distance)
                            {
                                targetAgent = agent;
                                distance = newDist;
                            }
                        }
                    }
                    else
                    {
                        var newDist = unitPosition.Distance(agent.GetWorldPosition().AsVec2);
                        if (newDist < distance)
                        {
                            targetAgent = agent;
                            distance = newDist;
                        }
                    }
                });
            return targetAgent;
        }

        public static Agent NearestEnemyAgent(Agent unit)
        {
            Agent targetAgent = null;
            var distance = 10000f;
            var unitPosition = unit.GetWorldPosition().AsVec2;
            foreach (var team in Mission.Current.Teams.ToList())
                if (team.IsEnemyOf(unit.Formation.Team))
                    foreach (var enemyFormation in team.Formations.ToList())
                        enemyFormation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                        {
                            var newDist = unitPosition.Distance(agent.GetWorldPosition().AsVec2);
                            if (newDist < distance)
                            {
                                targetAgent = agent;
                                distance = newDist;
                            }
                        });
            return targetAgent;
        }

        public static float RatioOfCrossbowmen(Formation formation)
        {
            var ratio = 0f;
            var crossCount = 0;
            if (formation != null)
            {
                formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                {
                    var isCrossbowmen = false;
                    for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                         equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                         equipmentIndex++)
                        if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                            if (agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Crossbow)
                            {
                                isCrossbowmen = true;
                                break;
                            }

                    if (isCrossbowmen) crossCount++;
                });
                ratio = crossCount / (float)formation.CountOfUnits;
                return ratio;
            }

            return ratio;
        }

        public static bool IsFormationShooting(Formation formation, float desiredRatio = 0.3f,
            float lastAttackTimeTreshold = 10f)
        {
            var ratio = 0f;
            var countOfShooting = 0;
            if (formation != null && Mission.Current != null)
            {
                float ratioOfCrossbowmen;
                //if (RBMConfig.RBMConfig.rbmCombatEnabled)
                //{
                //    ratioOfCrossbowmen = RatioOfCrossbowmen(formation);
                //}
                //else
                //{
                ratioOfCrossbowmen = 0f;
                //}
                formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                {
                    //float currentTime = agent.Mission.CurrentTime;
                    var currentTime = MBCommon.GetTotalMissionTime();
                    if (agent.LastRangedAttackTime > 0f && currentTime > agent.LastRangedAttackTime &&
                        currentTime - agent.LastRangedAttackTime < lastAttackTimeTreshold + 20f * ratioOfCrossbowmen)
                        countOfShooting++;
                    //else
                    //{
                    //    agent.ClearTargetFrame();
                    //}
                    ratio = countOfShooting / (float)formation.CountOfUnits;
                });
                if (ratio > desiredRatio) return true;
            }

            return false;
        }

        public static bool FormationActiveSkirmishersRatio(Formation formation, float desiredRatio)
        {
            var ratio = 0f;
            var countOfSkirmishers = 0;
            if (formation != null && Mission.Current != null)
            {
                formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                {
                    //float currentTime = MBCommon.TimeType.Mission.GetTime();
                    //if (currentTime - agent.LastRangedAttackTime < 6f)
                    //{
                    //    countOfSkirmishers++;
                    //}
                    var isActiveSkrimisher = false;
                    var countedUnits = 0f;
                    //float currentTime = Mission.Current.CurrentTime;
                    var currentTime = MBCommon.GetTotalMissionTime();
                    if (agent.LastRangedAttackTime > 0f && currentTime - agent.LastRangedAttackTime < 6f &&
                        currentTime > agent.LastRangedAttackTime && ratio <= desiredRatio &&
                        countedUnits / formation.CountOfUnits <= desiredRatio)
                        for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                             equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                             equipmentIndex++)
                            if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                                if (agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown &&
                                    agent.Equipment[equipmentIndex].Amount > 1)
                                {
                                    isActiveSkrimisher = true;
                                    break;
                                }

                    if (isActiveSkrimisher) countOfSkirmishers++;
                    countedUnits++;
                    ratio = countOfSkirmishers / (float)formation.CountOfUnits;
                });
                if (ratio > desiredRatio) return true;
            }

            return false;
        }

        public static bool FormationFightingInMelee(Formation formation, float desiredRatio)
        {
            //float currentTime = Mission.Current.CurrentTime;
            var currentTime = MBCommon.GetTotalMissionTime();
            float countedUnits = 0;
            var ratio = 0f;
            float countOfUnitsFightingInMelee = 0;
            formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                if (agent != null && ratio <= desiredRatio && countedUnits / formation.CountOfUnits <= desiredRatio)
                {
                    var lastMeleeAttackTime = agent.LastMeleeAttackTime;
                    var lastMeleeHitTime = agent.LastMeleeHitTime;
                    if (currentTime - lastMeleeAttackTime < 6f || currentTime - lastMeleeHitTime < 6f)
                        countOfUnitsFightingInMelee++;
                    countedUnits++;
                }
            });
            if (countOfUnitsFightingInMelee / formation.CountOfUnits >= desiredRatio) return true;
            return false;
        }

        public static List<Formation> FindSignificantFormations(Formation formation)
        {
            var formations = new List<Formation>();
            if (formation != null)
                if (formation.QuerySystem.ClosestEnemyFormation != null)
                    foreach (var team in Mission.Current.Teams.ToList())
                        if (team.IsEnemyOf(formation.Team))
                        {
                            if (team.Formations.ToList().Count == 1)
                            {
                                formations.Add(team.Formations.ToList()[0]);
                                return formations;
                            }

                            foreach (var enemyFormation in team.Formations.ToList())
                            {
                                if (formation != null && enemyFormation.CountOfUnits > 0 &&
                                    enemyFormation.QuerySystem.IsInfantryFormation) formations.Add(enemyFormation);
                                if (formation != null && enemyFormation.CountOfUnits > 0 &&
                                    enemyFormation.QuerySystem.IsRangedFormation) formations.Add(enemyFormation);
                            }
                        }

            return formations;
        }

        public static List<Formation> FindSignificantArcherFormations(Formation formation)
        {
            var formations = new List<Formation>();
            if (formation != null)
                if (formation.QuerySystem.ClosestEnemyFormation != null)
                    foreach (var team in Mission.Current.Teams.ToList())
                        if (team.IsEnemyOf(formation.Team))
                        {
                            if (team.Formations.ToList().Count == 1)
                            {
                                formations.Add(team.Formations.ToList()[0]);
                                return formations;
                            }

                            foreach (var enemyFormation in team.Formations.ToList())
                                if (formation != null && enemyFormation.CountOfUnits > 0 &&
                                    enemyFormation.QuerySystem.IsRangedFormation)
                                    formations.Add(enemyFormation);
                        }

            return formations;
        }

        public static Formation FindSignificantEnemyToPosition(Formation formation, WorldPosition position,
            bool includeInfantry, bool includeRanged, bool includeCavalry, bool includeMountedSkirmishers,
            bool includeHorseArchers, bool withSide, bool unitCountMatters = false, float unitCountModifier = 1f)
        {
            Formation significantEnemy = null;
            var significantFormations = new List<Formation>();
            var dist = 10000f;
            var significantTreshold = 0.6f;
            var allEnemyFormations = new List<Formation>();

            if (formation != null)
                if (formation.QuerySystem.ClosestEnemyFormation != null)
                {
                    foreach (var team in Mission.Current.Teams.ToList())
                        if (team.IsEnemyOf(formation.Team))
                            foreach (var enemyFormation in team.Formations.ToList())
                                allEnemyFormations.Add(enemyFormation);

                    if (allEnemyFormations.ToList().Count == 1)
                    {
                        significantEnemy = allEnemyFormations[0];
                        return significantEnemy;
                    }

                    foreach (var enemyFormation in allEnemyFormations.ToList())
                    {
                        if (withSide)
                            if (formation.AI.Side != enemyFormation.AI.Side)
                                continue;
                        if (formation != null && includeInfantry && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsInfantryFormation)
                        {
                            var newDist = position.AsVec2.Distance(enemyFormation.QuerySystem.MedianPosition.AsVec2);
                            if (newDist < dist)
                            {
                                significantEnemy = enemyFormation;
                                dist = newDist;
                            }

                            var newUnitCountRatio =
                                enemyFormation.CountOfUnits * unitCountModifier / formation.CountOfUnits;
                            if (unitCountMatters)
                                if (newUnitCountRatio > significantTreshold)
                                    significantFormations.Add(enemyFormation);
                        }

                        if (formation != null && includeRanged && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsRangedFormation)
                        {
                            var newDist = position.AsVec2.Distance(enemyFormation.QuerySystem.MedianPosition.AsVec2);
                            if (newDist < dist)
                            {
                                significantEnemy = enemyFormation;
                                dist = newDist;
                            }

                            var newUnitCountRatio =
                                enemyFormation.CountOfUnits * unitCountModifier / formation.CountOfUnits;
                            if (unitCountMatters)
                                if (newUnitCountRatio > significantTreshold)
                                    significantFormations.Add(enemyFormation);
                        }
                    }

                    if (unitCountMatters)
                    {
                        if (significantFormations.Count > 0)
                        {
                            dist = 10000f;
                            foreach (var significantFormation in significantFormations)
                            {
                                var newDist =
                                    position.AsVec2.Distance(significantFormation.QuerySystem.MedianPosition.AsVec2);
                                if (newDist < dist)
                                {
                                    significantEnemy = significantFormation;
                                    dist = newDist;
                                }
                            }
                        }
                        else
                        {
                            dist = 10000f;
                            foreach (var significantFormation in allEnemyFormations)
                            {
                                var newDist =
                                    position.AsVec2.Distance(significantFormation.QuerySystem.MedianPosition.AsVec2);
                                if (newDist < dist)
                                {
                                    significantEnemy = significantFormation;
                                    dist = newDist;
                                }
                            }
                        }
                    }

                    if (significantEnemy == null)
                    {
                        dist = 10000f;
                        var unitCountRatio = 0f;
                        foreach (var enemyFormation in allEnemyFormations)
                        {
                            var newUnitCountRatio = enemyFormation.CountOfUnits / (float)formation.CountOfUnits;
                            var newDist =
                                formation.QuerySystem.MedianPosition.AsVec2.Distance(enemyFormation.QuerySystem
                                    .MedianPosition.AsVec2);
                            if (newDist < dist * newUnitCountRatio * 1.5f)
                            {
                                significantEnemy = enemyFormation;
                                unitCountRatio = newUnitCountRatio;
                                dist = newDist;
                            }
                        }
                    }
                }

            return significantEnemy;
        }

        public static Formation FindSignificantEnemy(Formation formation, bool includeInfantry, bool includeRanged,
            bool includeCavalry, bool includeMountedSkirmishers, bool includeHorseArchers, bool unitCountMatters = true)
        {
            unitCountMatters = true;
            Formation significantEnemy = null;
            var significantFormations = new List<Formation>();
            var dist = 10000f;
            var significantTreshold = 0.6f;
            var allEnemyFormations = new List<Formation>();

            if (formation != null)
                if (formation.QuerySystem.ClosestEnemyFormation != null)
                {
                    foreach (var team in Mission.Current.Teams.ToList())
                        if (team.IsEnemyOf(formation.Team))
                            foreach (var enemyFormation in team.Formations.ToList())
                                allEnemyFormations.Add(enemyFormation);

                    if (allEnemyFormations.ToList().Count == 1)
                    {
                        significantEnemy = allEnemyFormations[0];
                        return significantEnemy;
                    }

                    foreach (var enemyFormation in allEnemyFormations.ToList())
                    {
                        if (formation != null && includeInfantry && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsInfantryFormation)
                            if (unitCountMatters)
                                significantFormations.Add(enemyFormation);
                        if (formation != null && includeRanged && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsRangedFormation)
                            if (unitCountMatters)
                                significantFormations.Add(enemyFormation);
                        if (formation != null && includeCavalry && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsCavalryFormation &&
                            !enemyFormation.QuerySystem.IsRangedCavalryFormation)
                            if (unitCountMatters)
                                significantFormations.Add(enemyFormation);
                        if (formation != null && includeHorseArchers && enemyFormation.CountOfUnits > 0 &&
                            enemyFormation.QuerySystem.IsRangedCavalryFormation)
                            if (unitCountMatters)
                                significantFormations.Add(enemyFormation);
                    }

                    if (unitCountMatters)
                    {
                        if (significantFormations.Count > 0)
                        {
                            //float unitCount = 0;
                            var formationWeight = 10000f;
                            foreach (var significantFormation in significantFormations)
                            {
                                var isMain = false;
                                if (significantFormation.AI != null) isMain = significantFormation.AI.IsMainFormation;
                                float unitCount = formation.CountOfUnits;
                                var distance =
                                    formation.QuerySystem.MedianPosition.AsVec2.Distance(significantFormation
                                        .QuerySystem.MedianPosition.AsVec2);
                                var newFormationWeight = distance / unitCount / (isMain ? 1.5f : 1f);

                                if (newFormationWeight < formationWeight)
                                {
                                    significantEnemy = significantFormation;
                                    formationWeight = newFormationWeight;
                                }
                            }
                        }
                        else
                        {
                            dist = 10000f;
                            foreach (var enemyFormation in allEnemyFormations)
                            {
                                var newUnitCountRatio = enemyFormation.CountOfUnits / (float)formation.CountOfUnits;
                                var newDist =
                                    formation.QuerySystem.MedianPosition.AsVec2.Distance(enemyFormation.QuerySystem
                                        .MedianPosition.AsVec2);

                                if (newDist >= dist * newUnitCountRatio * 1.5f || enemyFormation.CountOfUnits <= 0)
                                    continue;

                                if (includeInfantry && enemyFormation.QuerySystem.IsInfantryFormation)
                                {
                                    significantEnemy = enemyFormation;
                                    dist = newDist;
                                }

                                if (includeRanged && enemyFormation.QuerySystem.IsRangedFormation)
                                {
                                    significantEnemy = enemyFormation;
                                    dist = newDist;
                                }

                                if (includeCavalry
                                    && enemyFormation.QuerySystem.IsCavalryFormation
                                    && !enemyFormation.QuerySystem.IsRangedCavalryFormation)
                                {
                                    significantEnemy = enemyFormation;
                                    dist = newDist;
                                }

                                if (includeHorseArchers
                                    && enemyFormation.QuerySystem.IsRangedCavalryFormation
                                    && !enemyFormation.QuerySystem.IsCavalryFormation)
                                {
                                    significantEnemy = enemyFormation;
                                    dist = newDist;
                                }
                            }
                        }

                        if (significantEnemy == null)
                        {
                            dist = 10000f;
                            foreach (var enemyFormation in allEnemyFormations)
                            {
                                var newUnitCountRatio = enemyFormation.CountOfUnits / (float)formation.CountOfUnits;
                                var newDist =
                                    formation.QuerySystem.MedianPosition.AsVec2.Distance(enemyFormation.QuerySystem
                                        .MedianPosition.AsVec2);
                                if (newDist < dist * newUnitCountRatio * 1.5f)
                                {
                                    significantEnemy = enemyFormation;
                                    dist = newDist;
                                }
                            }
                        }
                    }
                }

            return significantEnemy;
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool CheckIfOnlyCavRemaining(Formation formation)
        {
            var allEnemyFormations = new List<Formation>();
            var result = true;
            try
            {
                if (formation != null)
                    if (formation.QuerySystem.ClosestEnemyFormation != null)
                    {
                        foreach (var team in Mission.Current.Teams.ToList())
                            if (team.IsEnemyOf(formation.Team))
                                foreach (var enemyFormation in team.Formations.ToList())
                                    allEnemyFormations.Add(enemyFormation);

                        foreach (var enemyFormation in allEnemyFormations.ToList())
                            if (!enemyFormation.QuerySystem.IsCavalryFormation &&
                                !enemyFormation.QuerySystem.IsRangedCavalryFormation)
                                result = false;
                    }
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }

        public static Formation FindSignificantAlly(Formation formation, bool includeInfantry, bool includeRanged,
            bool includeCavalry, bool includeMountedSkirmishers, bool includeHorseArchers,
            bool unitCountMatters = false)
        {
            Formation significantAlly = null;
            var dist = 10000f;
            var significantFormations = new List<Formation>();
            if (formation == null) return significantAlly;

            if (formation.QuerySystem.ClosestEnemyFormation == null) return significantAlly;

            foreach (var team in Mission.Current.Teams.ToList())
            {
                if (team.IsEnemyOf(formation.Team)) continue;

                if (team.Formations.ToList().Count == 1)
                {
                    significantAlly = team.Formations.ToList()[0];
                    return significantAlly;
                }

                if (unitCountMatters)
                {
                    var unitCount = -1;
                    foreach (var allyFormation in team.Formations.ToList())
                    {
                        if (includeInfantry && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsInfantryFormation && allyFormation.CountOfUnits > unitCount)
                        {
                            significantAlly = allyFormation;
                            unitCount = allyFormation.CountOfUnits;
                        }

                        if (includeRanged && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsRangedFormation && allyFormation.CountOfUnits > unitCount)
                        {
                            significantAlly = allyFormation;
                            unitCount = allyFormation.CountOfUnits;
                        }

                        if (includeCavalry && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsCavalryFormation
                            && !allyFormation.QuerySystem.IsRangedCavalryFormation &&
                            allyFormation.CountOfUnits > unitCount)
                        {
                            significantAlly = allyFormation;
                            unitCount = allyFormation.CountOfUnits;
                        }
                    }
                }
                else //unitcount doesn't matter
                {
                    foreach (var allyFormation in team.Formations.ToList())
                    {
                        if (includeInfantry && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsInfantryFormation)
                        {
                            var newDist =
                                formation.QuerySystem.MedianPosition.AsVec2.Distance(allyFormation.QuerySystem
                                    .MedianPosition.AsVec2);
                            if (newDist < dist)
                            {
                                significantAlly = allyFormation;
                                dist = newDist;
                            }
                        }

                        if (includeRanged && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsRangedFormation)
                        {
                            var newDist =
                                formation.QuerySystem.MedianPosition.AsVec2.Distance(allyFormation.QuerySystem
                                    .MedianPosition.AsVec2);
                            if (newDist < dist)
                            {
                                significantAlly = allyFormation;
                                dist = newDist;
                            }
                        }

                        if (includeCavalry && allyFormation.CountOfUnits > 0 &&
                            allyFormation.QuerySystem.IsCavalryFormation
                            && !allyFormation.QuerySystem.IsRangedCavalryFormation)
                        {
                            var newDist =
                                formation.QuerySystem.MedianPosition.AsVec2.Distance(allyFormation.QuerySystem
                                    .MedianPosition.AsVec2);
                            if (newDist < dist)
                            {
                                significantAlly = allyFormation;
                                dist = newDist;
                            }
                        }
                    }
                }
            }

            return significantAlly;
        }

        public static float GetCombatAIDifficultyMultiplier()
        {
            if (Game.Current.GameStateManager.ActiveState is MissionState missionState)
            {
                if (missionState.MissionName.Equals("EnhancedBattleTestFieldBattle") ||
                    missionState.MissionName.Equals("EnhancedBattleTestSiegeBattle"))
                    return 1.0f;

                switch (CampaignOptions.CombatAIDifficulty)
                {
                    case CampaignOptions.Difficulty.VeryEasy:
                        return 0.70f;
                    case CampaignOptions.Difficulty.Easy:
                        return 0.85f;
                    case CampaignOptions.Difficulty.Realistic:
                        return 1.0f;
                    default:
                        return 1.0f;
                }
            }

            switch (CampaignOptions.CombatAIDifficulty)
            {
                case CampaignOptions.Difficulty.VeryEasy:
                    return 0.1f;
                case CampaignOptions.Difficulty.Easy:
                    return 0.32f;
                case CampaignOptions.Difficulty.Realistic:
                    return 0.96f;
                default:
                    return 0.5f;
            }
        }


        public static float CalculateAILevel(Agent agent, int relevantSkillLevel)
        {
            var difficultyModifier = GetCombatAIDifficultyMultiplier();
            //float difficultyModifier = 1.0f; // v enhanced battle test je difficulty very easy
            return MBMath.ClampFloat(relevantSkillLevel / 260f * difficultyModifier, 0f, 1f);
        }

        public static float sign(Vec2 p1, Vec2 p2, Vec2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        public static bool PointInTriangle(Vec2 pt, Vec2 v1, Vec2 v2, Vec2 v3)
        {
            float d1, d2, d3;
            bool has_neg, has_pos;

            d1 = sign(pt, v1, v2);
            d2 = sign(pt, v2, v3);
            d3 = sign(pt, v3, v1);

            has_neg = d1 < 0 || d2 < 0 || d3 < 0;
            has_pos = d1 > 0 || d2 > 0 || d3 > 0;

            return !(has_neg && has_pos);
        }

        public static bool CheckIfSkirmisherAgent(Agent agent, float ammoAmout = 0)
        {
            //CharacterObject characterObject = agent.Character as CharacterObject;
            //if (characterObject != null && characterObject.Tier > 3)
            //{
            //    return false;
            //}
            for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                 equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                 equipmentIndex++)
            {
                if (agent.Equipment == null
                    || agent.Equipment[equipmentIndex].IsEmpty
                    || agent.Equipment[equipmentIndex].Amount <= ammoAmout)
                    continue;

                var wsd = agent.Equipment[equipmentIndex].GetWeaponStatsData();

                if (wsd[0].WeaponClass == (int)WeaponClass.Javelin
                    || wsd[0].WeaponClass == (int)WeaponClass.ThrowingAxe
                    || wsd[0].WeaponClass == (int)WeaponClass.ThrowingKnife)
                    return true;
            }

            return false;
        }

        public static bool CheckIfCanBrace(Agent agent)
        {
            for (var equipmentIndex = EquipmentIndex.WeaponItemBeginSlot;
                 equipmentIndex < EquipmentIndex.NumAllWeaponSlots;
                 equipmentIndex++)
                if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                {
                    var weapon = agent.Equipment[equipmentIndex];
                    if (weapon.IsEmpty) return false;
                    foreach (var weapon2 in weapon.Item.Weapons)
                    {
                        var weaponUsageId = weapon2.WeaponDescriptionId;
                        if (weaponUsageId != null &&
                            weaponUsageId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    }

                    return false;
                }

            return false;
        }

        public static bool ShouldFormationCopyShieldWall(Formation formation, float haveShieldThreshold = 0.6f)
        {
            var countAll = 0;
            var countHasShield = 0;

            if (formation.Team.HasTeamAi)
            {
                var field = typeof(TeamAIComponent).GetField("_currentTactic",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                field.DeclaringType.GetField("_currentTactic");
                var currentTactic = (TacticComponent)field.GetValue(formation.Team.TeamAI);

                if ((currentTactic != null && currentTactic.GetType() == typeof(RBMTacticAttackSplitInfantry)) ||
                    currentTactic.GetType() == typeof(RBMTacticAttackSplitInfantry)) return false;
            }

            formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
            {
                if (agent != null)
                {
                    if (agent.HasShieldCached) countHasShield++;
                    countAll++;
                }
            });

            if (countHasShield / countAll >= haveShieldThreshold)
                return true;
            return false;
        }

        public static IEnumerable<Agent> CountSoldiersInPolygon(Formation formation, Vec2[] polygon)
        {
            var enemyAgents = new List<Agent>();
            var result = 0;
            foreach (var team in Mission.Current.Teams.ToList())
                if (team.IsEnemyOf(formation.Team))
                    foreach (var enemyFormation in team.Formations.ToList())
                        formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                        {
                            if (IsPointInPolygon(polygon, agent.Position.AsVec2))
                            {
                                result++;
                                enemyAgents.Add(agent);
                            }
                        });
            return enemyAgents;
        }

        public static bool IsPointInPolygon(Vec2[] polygon, Vec2 testPoint)
        {
            //bool result = false;
            //int j = polygon.Count() - 1;
            //for (int i = 0; i < polygon.Count(); i++)
            //{
            //    if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y && polygon[i].Y >= testPoint.Y)
            //    {
            //        if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) * (polygon[j].X - polygon[i].X) < testPoint.X)
            //        {
            //            result = !result;
            //        }
            //    }
            //    j = i;
            //}
            //return result;
            Vec2 p1, p2;
            var inside = false;

            if (polygon.Length < 3) return inside;

            var oldPoint = new Vec2(
                polygon[polygon.Length - 1].X, polygon[polygon.Length - 1].Y);

            for (var i = 0; i < polygon.Length; i++)
            {
                var newPoint = new Vec2(polygon[i].X, polygon[i].Y);

                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if (newPoint.X < testPoint.X == testPoint.X <= oldPoint.X
                    && (testPoint.Y - (long)p1.Y) * (p2.X - p1.X)
                    < (p2.Y - (long)p1.Y) * (testPoint.X - p1.X))
                    inside = !inside;

                oldPoint = newPoint;
            }

            return inside;
        }

        public static float GetPowerOfAgentsSum(IEnumerable<Agent> agents)
        {
            var result = 0f;
            foreach (var agent in agents)
                result += MBMath.ClampInt((int)Math.Floor(agent.CharacterPowerCached * 65), 75, 200);
            return result;
        }

        public static string GetSiegeArcherPointsPath()
        {
            return BasePath.Name + "Modules/RBM/ModuleData/scene_positions/";
        }

        private static float GetPowerOriginal(int tier, bool isHero = false, bool isMounted = false)
        {
            return (2 + tier) * (8 + tier) * 0.02f * (isHero ? 1.5f : isMounted ? 1.2f : 1f);
        }
    }
}