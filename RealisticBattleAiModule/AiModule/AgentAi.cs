using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace RBMAI
{
    //class AgentAi
    //{
    //    [HarmonyPatch(typeof(AgentStatCalculateModel))]
    //    [HarmonyPatch("SetAiRelatedProperties")]
    //    class OverrideSetAiRelatedProperties
    //    {
    //        static void Postfix(Agent agent, ref AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem, AgentStatCalculateModel __instance)
    //        {
    //            bool agentHasShield = false;
    //            if (agent.GetWieldedItemIndex(Agent.HandIndex.OffHand) != EquipmentIndex.None)
    //            {
    //                if (agent.Equipment[agent.GetWieldedItemIndex(Agent.HandIndex.OffHand)].CurrentUsageItem.WeaponClass == WeaponClass.SmallShield ||
    //                    agent.Equipment[agent.GetWieldedItemIndex(Agent.HandIndex.OffHand)].CurrentUsageItem.WeaponClass == WeaponClass.LargeShield)
    //                {
    //                    agentHasShield = true;
    //                }
    //            }

    //            MethodInfo method = typeof(AgentStatCalculateModel).GetMethod("GetMeleeSkill", BindingFlags.NonPublic | BindingFlags.Instance);
    //            method.DeclaringType.GetMethod("GetMeleeSkill");

    //            //int meleeSkill = RBMAI.Utilities.GetMeleeSkill(agent, equippedItem, secondaryItem);
    //            //int effectiveSkill = RBMAI.Utilities.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);

    //            SkillObject skill = (equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
    //            int meleeSkill = (int)method.Invoke(__instance, new object[] { agent, equippedItem, secondaryItem });
    //            int effectiveSkill = __instance.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
    //            float meleeLevel = RBMAI.Utilities.CalculateAILevel(agent, meleeSkill);                 //num
    //            float effectiveSkillLevel = RBMAI.Utilities.CalculateAILevel(agent, effectiveSkill);    //num2
    //            float meleeDefensivness = meleeLevel + agent.Defensiveness;             //num3


    //            agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 3.5f;

    //            agentDrivenProperties.AiRangedHorsebackMissileRange = 0.35f; // percentage of maximum range is used, range of HA circle
    //            agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.90f;
    //            //agentDrivenProperties.AiFlyingMissileCheckRadius = 250f;

    //            float num4 = 1f - effectiveSkillLevel;
    //            if (!agent.WieldedWeapon.IsEmpty && agent.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Crossbow)
    //            {
    //                agentDrivenProperties.AiShooterError = 0.030f - (0.007f * effectiveSkillLevel);
    //            }
    //            else
    //            {
    //                agentDrivenProperties.AiShooterError = 0.025f - (0.020f * effectiveSkillLevel);
    //            }

    //            if (agent.IsAIControlled)
    //            {
    //                agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 0.33f;
    //                agentDrivenProperties.WeaponBestAccuracyWaitTime = 1.33f;

    //            }

    //            agentDrivenProperties.AiRangerLeadErrorMin = (float)((0.0 - (double)num4) * 0.349999994039536) + 0.3f;
    //            agentDrivenProperties.AiRangerLeadErrorMax = num4 * 0.2f + 0.3f;

    //            if (equippedItem != null && equippedItem.RelevantSkill == DefaultSkills.Bow)
    //            {
    //                if (agent.MountAgent != null)
    //                {
    //                    //agentDrivenProperties.AiRangerVerticalErrorMultiplier = 0f;//horse archers
    //                    //agentDrivenProperties.AiRangerHorizontalErrorMultiplier = 0f;//horse archers
    //                    agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
    //                    agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
    //                    agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 0.33f;
    //                    agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 0.5f;
    //                    agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.02f;
    //                }
    //                else
    //                {
    //                    agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
    //                    agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
    //                }
    //            }
    //            else if (equippedItem != null && equippedItem.RelevantSkill == DefaultSkills.Crossbow)
    //            {
    //                agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.015f - effectiveSkill * 0.0001f, 0.005f, 0.015f);//crossbow
    //                agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.010f - effectiveSkill * 0.0001f, 0.005f, 0.010f);//crossbow
    //            }
    //            else
    //            {
    //                agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.025f - effectiveSkill * 0.0001f, 0.01f, 0.025f);// javelins and axes etc
    //                agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.025f - effectiveSkill * 0.0001f, 0.01f, 0.025f);// javelins and axes etc
    //            }

    //            agentDrivenProperties.AiShootFreq = MBMath.ClampFloat(effectiveSkillLevel * 1.5f, 0.1f, 0.9f); // when set to 0 AI never shoots
    //                                                                                                           //agentDrivenProperties.AiWaitBeforeShootFactor = 0f;
    //                                                                                                           //agentDrivenProperties.AiMinimumDistanceToContinueFactor = 5f; //2f + 0.3f * (3f - meleeSkill);
    //                                                                                                           //agentDrivenProperties.AIHoldingReadyMaxDuration = 0.1f; //MBMath.Lerp(0.25f, 0f, MBMath.Min(1f, num * 1.2f));
    //                                                                                                           //agentDrivenProperties.AIHoldingReadyVariationPercentage = //num;

    //            //agentDrivenProperties.ReloadSpeed = 0.19f; //0.12 for heavy crossbows, 0.19f for light crossbows, composite bows and longbows.

    //            //                GetEffectiveSkill

    //            if (agent.Formation != null && agent.Formation.QuerySystem.IsInfantryFormation && !agent.IsRangedCached)
    //            {
    //                agentDrivenProperties.ReloadMovementPenaltyFactor = 0.1f;
    //            }

    //            if (agent.IsRangedCached)
    //            {
    //                //agent.SetScriptedCombatFlags(Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation);
    //                agent.SetScriptedCombatFlags(agent.GetScriptedCombatFlags() | Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation);
    //                //agent.ResetAiWaitBeforeShootFactor();
    //            }
    //            agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, 1f);
    //            //agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, 0f);
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(SandboxAgentStatCalculateModel))]
    [HarmonyPatch("GetSkillEffectsOnAgent")]
    class GetSkillEffectsOnAgentPatch
    {
        static bool Prefix(ref SandboxAgentStatCalculateModel __instance, ref Agent agent, ref AgentDrivenProperties agentDrivenProperties, WeaponComponentData rightHandEquippedItem)
        {
            float swingSpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
            float thrustOrRangedReadySpeedMultiplier = agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier;
            float reloadSpeed = agentDrivenProperties.ReloadSpeed;
            if (agent.Character is CharacterObject characterObject && rightHandEquippedItem != null)
            {
                int effectiveSkill = __instance.GetEffectiveSkill(characterObject, agent.Origin, agent.Formation, rightHandEquippedItem.RelevantSkill);
                ExplainedNumber stat = new ExplainedNumber(swingSpeedMultiplier);
                ExplainedNumber stat2 = new ExplainedNumber(thrustOrRangedReadySpeedMultiplier);
                ExplainedNumber stat3 = new ExplainedNumber(reloadSpeed);
                if (rightHandEquippedItem.RelevantSkill == DefaultSkills.OneHanded)
                {
                    if (effectiveSkill > 150)
                    {
                        effectiveSkill = 150;
                    }
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.OneHanded, DefaultSkillEffects.OneHandedSpeed, characterObject, ref stat, effectiveSkill);
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.OneHanded, DefaultSkillEffects.OneHandedSpeed, characterObject, ref stat2, effectiveSkill);
                }
                else if (rightHandEquippedItem.RelevantSkill == DefaultSkills.TwoHanded)
                {
                    if (effectiveSkill > 150)
                    {
                        effectiveSkill = 150;
                    }
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.TwoHanded, DefaultSkillEffects.TwoHandedSpeed, characterObject, ref stat, effectiveSkill);
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.TwoHanded, DefaultSkillEffects.TwoHandedSpeed, characterObject, ref stat2, effectiveSkill);
                }
                else if (rightHandEquippedItem.RelevantSkill == DefaultSkills.Polearm)
                {
                    if (effectiveSkill > 150)
                    {
                        effectiveSkill = 150;
                    }
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.Polearm, DefaultSkillEffects.PolearmSpeed, characterObject, ref stat, effectiveSkill);
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.Polearm, DefaultSkillEffects.PolearmSpeed, characterObject, ref stat2, effectiveSkill);
                }
                else if (rightHandEquippedItem.RelevantSkill == DefaultSkills.Crossbow)
                {
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.Crossbow, DefaultSkillEffects.CrossbowReloadSpeed, characterObject, ref stat3, effectiveSkill);
                }
                else if (rightHandEquippedItem.RelevantSkill == DefaultSkills.Throwing)
                {
                    SkillHelper.AddSkillBonusForCharacter(DefaultSkills.Throwing, DefaultSkillEffects.ThrowingSpeed, characterObject, ref stat2, effectiveSkill);
                }
                if (agent.HasMount)
                {
                    int effectiveSkill2 = __instance.GetEffectiveSkill(characterObject, agent.Origin, agent.Formation, DefaultSkills.Riding);
                    float value = -0.01f * MathF.Max(0f, DefaultSkillEffects.HorseWeaponSpeedPenalty.GetPrimaryValue(effectiveSkill2));
                    stat.AddFactor(value);
                    stat2.AddFactor(value);
                    stat3.AddFactor(value);

                }
                agentDrivenProperties.SwingSpeedMultiplier = stat.ResultNumber;
                agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = stat2.ResultNumber;
                agentDrivenProperties.ReloadSpeed = stat3.ResultNumber;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ArrangementOrder))]
    [HarmonyPatch("GetShieldDirectionOfUnit")]
    class HoldTheDoor
    {
        static void Postfix(ref Agent.UsageDirection __result, Formation formation, Agent unit, ArrangementOrderEnum orderEnum)
        {
            if (unit.IsDetachedFromFormation)
            {
                __result = Agent.UsageDirection.None;
                return;
            }
            bool test = true;
            switch (orderEnum)
            {
                case ArrangementOrderEnum.ShieldWall:
                    if (unit.Formation.FiringOrder.OrderEnum != FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire)
                    {
                        bool hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        bool hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);
                        if (hasRanged || hasTwoHanded)
                        {
                            test = false;
                        }
                    }
                    if (test)
                    {
                        if (((IFormationUnit)unit).FormationRankIndex == 0)
                        {
                            __result = Agent.UsageDirection.DefendDown;
                            return;
                        }
                        if (formation.Arrangement.GetNeighborUnitOfLeftSide(unit) == null)
                        {
                            __result = Agent.UsageDirection.DefendLeft;
                            return;
                        }
                        if (formation.Arrangement.GetNeighborUnitOfRightSide(unit) == null)
                        {
                            __result = Agent.UsageDirection.DefendRight;
                            return;
                        }
                        __result = Agent.UsageDirection.AttackEnd;
                        return;
                    }
                    __result = Agent.UsageDirection.None;
                    return;
                case ArrangementOrderEnum.Circle:
                case ArrangementOrderEnum.Square:
                    if (unit.Formation.FiringOrder.OrderEnum != FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire)
                    {
                        bool hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        bool hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);
                        if (hasRanged || hasTwoHanded)
                        {
                            test = false;
                        }
                    }
                    if (test)
                    {
                        if (((IFormationUnit)unit).FormationRankIndex == 0)
                        {
                            __result = Agent.UsageDirection.DefendDown;
                            return;
                        }
                        __result = Agent.UsageDirection.AttackEnd;
                        return;
                    }
                    __result = Agent.UsageDirection.None;
                    return;
                default:
                    __result = Agent.UsageDirection.None;
                    return;
            }
        }
    }

    [HarmonyPatch(typeof(Agent))]
    [HarmonyPatch("UpdateLastAttackAndHitTimes")]
    class UpdateLastAttackAndHitTimesFix
    {

        static bool Prefix(ref Agent __instance, Agent attackerAgent, bool isMissile)
        {
            PropertyInfo LastRangedHitTime = typeof(Agent).GetProperty("LastRangedHitTime");
            LastRangedHitTime.DeclaringType.GetProperty("LastRangedHitTime");

            PropertyInfo LastRangedAttackTime = typeof(Agent).GetProperty("LastRangedAttackTime");
            LastRangedAttackTime.DeclaringType.GetProperty("LastRangedAttackTime");

            PropertyInfo LastMeleeHitTime = typeof(Agent).GetProperty("LastMeleeHitTime");
            LastMeleeHitTime.DeclaringType.GetProperty("LastMeleeHitTime");

            PropertyInfo LastMeleeAttackTime = typeof(Agent).GetProperty("LastMeleeAttackTime");
            LastMeleeAttackTime.DeclaringType.GetProperty("LastMeleeAttackTime");

            float currentTime = MBCommon.GetTotalMissionTime();
            if (isMissile)
            {
                //__instance.LastRangedHitTime = currentTime;
                LastRangedHitTime.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }
            else
            {
                //LastMeleeHitTime = currentTime;
                LastMeleeHitTime.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }
            if (attackerAgent != __instance && attackerAgent != null)
            {
                if (isMissile)
                {
                    //attackerAgent.LastRangedAttackTime = currentTime;
                    LastRangedAttackTime.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                }
                else
                {
                    //attackerAgent.LastMeleeAttackTime = currentTime;
                    LastMeleeAttackTime.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                }
            }

            if (!__instance.IsHuman)
            {
                if (__instance.RiderAgent != null)
                {
                    if (isMissile)
                    {
                        //__instance.LastRangedHitTime = currentTime;
                        LastRangedHitTime.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                    }
                    else
                    {
                        //LastMeleeHitTime = currentTime;
                        LastMeleeHitTime.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HumanAIComponent))]
    [HarmonyPatch("OnTickAsAI")]
    class OnTickAsAIPatch
    {

        public static Dictionary<Agent, float> itemPickupDistanceStorage = new Dictionary<Agent, float> { };

        static void Postfix(ref SpawnedItemEntity ____itemToPickUp, ref Agent ___Agent)
        {
            if (____itemToPickUp != null && (___Agent.AIStateFlags & Agent.AIStateFlag.UseObjectMoving) != 0)
            {
                float num = MissionGameModels.Current.AgentStatCalculateModel.GetInteractionDistance(___Agent) * 3f;
                WorldFrame userFrameForAgent = ____itemToPickUp.GetUserFrameForAgent(___Agent);
                ref WorldPosition origin = ref userFrameForAgent.Origin;
                Vec3 targetPoint = ___Agent.Position;
                float distanceSq = origin.DistanceSquaredWithLimit(in targetPoint, num * num + 1E-05f);
                float newDist;
                itemPickupDistanceStorage.TryGetValue(___Agent, out newDist);
                if (newDist == 0f)
                {
                    itemPickupDistanceStorage[___Agent] = distanceSq;
                }
                else
                {
                    if (distanceSq == newDist)
                    {
                        ___Agent.StopUsingGameObject(isSuccessful: false);
                        itemPickupDistanceStorage.Remove(___Agent);

                    }
                    itemPickupDistanceStorage[___Agent] = distanceSq;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentShootMissile")]
    [UsedImplicitly]
    [MBCallback]
    class OverrideOnAgentShootMissile
    {

        //private static int _oldMissileSpeed;
        static bool Prefix(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position, ref Vec3 velocity, Mat3 orientation, bool hasRigidBody, bool isPrimaryWeaponShot, int forcedMissileIndex)
        {
            MissionWeapon missionWeapon = shooterAgent.Equipment[weaponIndex];
            WeaponStatsData[] wsd = missionWeapon.GetWeaponStatsData();

            int weapClass = wsd[0].WeaponClass;

            if (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle
                && !shooterAgent.IsMainAgent
                && (weapClass == (int)WeaponClass.Javelin || weapClass == (int)WeaponClass.ThrowingAxe || weapClass == (int)WeaponClass.Bow))
            {
                if (!shooterAgent.HasMount)
                {
                    velocity.z -= 1.4f;
                }
                else
                {
                    velocity.z -= 2f;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentDismount")]
    class OnAgentDismountPatch
    {
        static void Postfix(Agent agent)
        {
            if (!agent.IsPlayerControlled && agent.Formation != null && Mission.Current != null && Mission.Current.IsFieldBattle)
            {
                bool isInfFormationActive = agent.Team.GetFormation(FormationClass.Infantry) != null && agent.Team.GetFormation(FormationClass.Infantry).CountOfUnits > 0;
                bool isArcFormationActive = agent.Team.GetFormation(FormationClass.Ranged) != null && agent.Team.GetFormation(FormationClass.Ranged).CountOfUnits > 0;
                if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) || agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
                {
                    float distanceToInf = -1f;
                    float distanceToArc = -1f;

                    if (agent.Formation != null && isInfFormationActive)
                    {
                        distanceToInf = agent.Team.GetFormation(FormationClass.Infantry).QuerySystem.MedianPosition.AsVec2.Distance(agent.Formation.QuerySystem.MedianPosition.AsVec2);
                    }
                    if (agent.Formation != null && isArcFormationActive)
                    {
                        distanceToArc = agent.Team.GetFormation(FormationClass.Ranged).QuerySystem.MedianPosition.AsVec2.Distance(agent.Formation.QuerySystem.MedianPosition.AsVec2);
                    }
                    if (distanceToArc > 0f && distanceToArc < distanceToInf)
                    {
                        agent.Formation = agent.Team.GetFormation(FormationClass.Ranged);
                        agent.DisableScriptedMovement();
                        return;
                    }
                    else if (distanceToInf > 0f && distanceToInf < distanceToArc)
                    {
                        agent.Formation = agent.Team.GetFormation(FormationClass.Infantry);
                        agent.DisableScriptedMovement();
                        return;
                    }
                    else
                    {
                        if (distanceToInf > 0f)
                        {
                            agent.Formation = agent.Team.GetFormation(FormationClass.Infantry);
                            agent.DisableScriptedMovement();
                            return;
                        }
                        else if (distanceToArc > 0f)
                        {
                            agent.Formation = agent.Team.GetFormation(FormationClass.Ranged);
                            agent.DisableScriptedMovement();
                            return;
                        }
                    }
                }
                else
                {
                    if (agent.Formation != null && isInfFormationActive)
                    {
                        agent.Formation = agent.Team.GetFormation(FormationClass.Infantry);
                        agent.DisableScriptedMovement();
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentMount")]
    class OnAgentMountPatch
    {
        static void Postfix(Agent agent)
        {
            if (!agent.IsPlayerControlled && agent.Formation != null && Mission.Current != null && Mission.Current.IsFieldBattle)
            {
                bool isCavFormationActive = agent.Team.GetFormation(FormationClass.Cavalry) != null && agent.Team.GetFormation(FormationClass.Cavalry).CountOfUnits > 0;
                bool isHaFormationActive = agent.Team.GetFormation(FormationClass.HorseArcher) != null && agent.Team.GetFormation(FormationClass.HorseArcher).CountOfUnits > 0;
                if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) || agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
                {
                    if (agent.Formation != null && isHaFormationActive)
                    {
                        agent.Formation = agent.Team.GetFormation(FormationClass.HorseArcher);
                        agent.DisableScriptedMovement();
                        return;
                    }
                }
                else
                {
                    if (agent.Formation != null && isCavFormationActive)
                    {
                        agent.Formation = agent.Team.GetFormation(FormationClass.Cavalry);
                        agent.DisableScriptedMovement();
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Formation))]
    [HarmonyPatch("ApplyActionOnEachUnit", new Type[] { typeof(Action<Agent>) })]
    class ApplyActionOnEachUnitPatch
    {
        static bool Prefix(ref Action<Agent> action, ref Formation __instance)
        {
            try
            {
                __instance.ApplyActionOnEachUnitViaBackupList(action);
                return false;
            }
            catch (Exception)
            {
                {
                    return true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MissionAgentLabelView))]
    [HarmonyPatch("SetHighlightForAgents")]
    class SetHighlightForAgentsPatch
    {
        static bool Prefix(bool highlight, ref bool useSiegeMachineUsers, ref bool useAllTeamAgents, Dictionary<Agent, MetaMesh> ____agentMeshes, MissionAgentLabelView __instance)
        {

            if (__instance.Mission.PlayerTeam?.PlayerOrderController == null)
            {
                bool flag = __instance.Mission.PlayerTeam == null;
                Debug.Print($"PlayerOrderController is null and playerTeamIsNull: {flag}", 0, Debug.DebugColor.White, 17179869184uL);
            }
            if (useSiegeMachineUsers)
            {
                foreach (TaleWorlds.MountAndBlade.SiegeWeapon selectedWeapon in __instance.Mission.PlayerTeam?.PlayerOrderController.SiegeWeaponController.SelectedWeapons)
                {
                    foreach (Agent user in selectedWeapon.Users)
                    {
                        MetaMesh agentMesh;
                        if (____agentMeshes.TryGetValue(user, out agentMesh))
                        {
                            MethodInfo method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
                            method.DeclaringType.GetMethod("UpdateSelectionVisibility");
                            method.Invoke(__instance, new object[] { user, agentMesh, highlight });
                        }

                    }
                }
                return false;
            }
            if (useAllTeamAgents)
            {
                if (__instance.Mission.PlayerTeam?.PlayerOrderController.Owner == null)
                {
                    return false;
                }
                foreach (Agent activeAgent in __instance.Mission.PlayerTeam?.PlayerOrderController.Owner.Team.ActiveAgents)
                {
                    MetaMesh agentMesh;
                    if (____agentMeshes.TryGetValue(activeAgent, out agentMesh))
                    {
                        MethodInfo method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.DeclaringType.GetMethod("UpdateSelectionVisibility");
                        method.Invoke(__instance, new object[] { activeAgent, agentMesh, highlight });
                    }
                }
                return false;
            }
            foreach (Formation selectedFormation in __instance.Mission.PlayerTeam?.PlayerOrderController.SelectedFormations)
            {
                selectedFormation.ApplyActionOnEachUnit(delegate (Agent agent)
                {
                    MetaMesh agentMesh;
                    if (____agentMeshes.TryGetValue(agent, out agentMesh))
                    {
                        MethodInfo method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.DeclaringType.GetMethod("UpdateSelectionVisibility");
                        method.Invoke(__instance, new object[] { agent, agentMesh, highlight });
                    }
                });
            }
            return false;
        }
    }
}
