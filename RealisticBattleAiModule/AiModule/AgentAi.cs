using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Helpers;
using JetBrains.Annotations;
using RBMConfig;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace RBMAI.AiModule
{
    internal class AgentAi
    {
        [HarmonyPatch(typeof(AgentStatCalculateModel))]
        [HarmonyPatch("SetAiRelatedProperties")]
        class OverrideSetAiRelatedProperties
        {
            static void Postfix(Agent agent, ref AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem, AgentStatCalculateModel __instance)
            {
                // ReSharper disable once ReplaceWithSingleAssignment.False
                var agentHasShield = false;

                //if (agent.GetWieldedItemIndex(Agent.HandIndex.OffHand) != EquipmentIndex.None)
                //{
                //if (agent.Equipment[agent.GetWieldedItemIndex(Agent.HandIndex.OffHand)].CurrentUsageItem.WeaponClass == WeaponClass.SmallShield ||
                //    agent.Equipment[agent.GetWieldedItemIndex(Agent.HandIndex.OffHand)].CurrentUsageItem.WeaponClass == WeaponClass.LargeShield)

                // ReSharper disable once ConvertIfToOrExpression
                if (secondaryItem?.WeaponClass == WeaponClass.SmallShield || secondaryItem?.WeaponClass == WeaponClass.LargeShield)
                {
                    agentHasShield = true;
                }

                //}
                var method = typeof(AgentStatCalculateModel).GetMethod("GetMeleeSkill", BindingFlags.NonPublic | BindingFlags.Instance);
                var _ = method?.DeclaringType?.GetMethod("GetMeleeSkill");
                //int meleeSkill = RBMAI.Utilities.GetMeleeSkill(agent, equippedItem, secondaryItem);
                //int effectiveSkill = RBMAI.Utilities.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
                //SkillObject skill = (equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
                var meleeSkill = (int)method.Invoke(__instance, new object[] { agent, equippedItem, secondaryItem });
                //int effectiveSkill = __instance.GetEffectiveSkill(agent.Character, agent.Origin, agent.Formation, skill);
                var meleeLevel = RBMAI.Utilities.CalculateAILevel(agent, meleeSkill);                 //num
                //float effectiveSkillLevel = RBMAI.Utilities.CalculateAILevel(agent, effectiveSkill);    //num2
                //float meleeDefensivness = meleeLevel + agent.Defensiveness;             //num3

                //if (RBMConfig.RBMConfig.rbmCombatEnabled)
                //{
                //    agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 7f;
                //}
                //else
                //{
                //    agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 3.5f;
                //}
                //if (!RBMConfig.RBMConfig.vanillaCombatAi)
                //{
                //if (RBMConfig.RBMConfig.postureEnabled)
                //{
                agentDrivenProperties.AIBlockOnDecideAbility = MBMath.ClampFloat(meleeLevel * 2f, 0.3f, 1f);// chance for directed blocking
                if (agentHasShield)
                {
                    agentDrivenProperties.AIParryOnDecideAbility = MBMath.ClampFloat(meleeLevel * 0.5f, 0f, 0.6f);// chance for parry and perfect block, can be wrong side
                    agentDrivenProperties.AIAttackOnDecideChance = MBMath.ClampFloat(meleeLevel * 0.3f, 0.1f, 0.15f);//MBMath.ClampFloat(0.23f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f); //0.05-1f, 0.66-line, 0.44 - shield wall - aggressiveness / chance of attack instead of anything else / when set to 0 AI never attacks on its own
                    agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = MBMath.ClampFloat(meleeLevel * 0.3f, 0f, 0.2f);//chance to fix wrong side parry
                    agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = MBMath.ClampFloat(meleeLevel * 0.46f, 0f, 0.35f);// chance to break own attack to do something else (LIKE CHANGING DIRECTION) - fainting
                    agentDrivenProperties.AIAttackOnParryChance = MBMath.ClampFloat(meleeLevel * 0.3f, 0.05f, 0.2f);//0.3f - 0.1f * agent.Defensiveness; //0.2-0.3f // chance to break own parry guard - 0 constant parry in reaction to enemy, 1 constant breaking of parry
                }
                else
                {
                    agentDrivenProperties.AIParryOnDecideAbility = MBMath.ClampFloat(meleeLevel, 0.1f, 0.6f);// chance for parry, can be wrong side
                    agentDrivenProperties.AIAttackOnDecideChance = 0.15f;//MBMath.ClampFloat(0.23f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f); //0.05-1f, 0.66-line, 0.44 - shield wall - aggressiveness / chance of attack instead of anything else / when set to 0 AI never attacks on its own
                    agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = MBMath.ClampFloat(meleeLevel * 0.8f, 0.05f, 0.5f);
                    agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = MBMath.ClampFloat(meleeLevel * 0.46f, 0f, 0.35f);
                    agentDrivenProperties.AIAttackOnParryChance = MBMath.ClampFloat(meleeLevel * 0.45f, 0.2f, 0.4f); //0.3f - 0.1f * agent.Defensiveness; //0.2-0.3f // chance to break own parry guard - 0 constant parry in reaction to enemy, 1 constant breaking of parry
                }
                //if (agent.HasMount)
                //{
                //    agentDrivenProperties.AIAttackOnDecideChance = 0.3f;
                //}
                agentDrivenProperties.AIDecideOnAttackChance = 0.5f;//MBMath.ClampFloat(meleeLevel*0.3f, 0.15f, 0.5f); //0.15f * agent.Defensiveness; //0-0.15f -esentailly ability to reconsider attack, how often is direction changed (or swtich to parry) when preparing for attack
                agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = 1f;//MBMath.ClampFloat(1f - (meleeLevel * 1f), 0.1f, 1.0f);//MBMath.ClampMin(1f, 0.2f + 0.5f * num + 0.2f * num3); 0.599-0.799 = 200 skill line/wall - chance for passive constant block, seems to trigger if you are prepared to attack AI for long enough
                agentDrivenProperties.AiAttackCalculationMaxTimeFactor = meleeLevel; //how long does AI prepare for an attack
                agentDrivenProperties.AiRaiseShieldDelayTimeBase = MBMath.ClampFloat(-0.25f + (meleeLevel * 0.6f), -0.25f, -0.05f); //MBMath.ClampFloat(-0.5f + (meleeLevel * 1.25f), -0.5f, 0f); //-0.75f + 0.5f * meleeLevel; delay between block decision and actual block for AI
                agentDrivenProperties.AiAttackingShieldDefenseChance = 1f;//MBMath.ClampFloat(meleeLevel * 2f, 0.1f, 1.0f); ; //0.2f + 0.3f * meleeLevel;
                agentDrivenProperties.AiAttackingShieldDefenseTimer = MBMath.ClampFloat(-0.3f + (meleeLevel * 0.6f), -0.3f, 0f);  //-0.3f + 0.3f * meleeLevel; Delay between deciding to swith from attack to defense
                //}
                //else
                //{
                //    agentDrivenProperties.AIBlockOnDecideAbility = MBMath.ClampFloat(0.1f + meleeLevel * 0.6f, 0.2f, 0.45f); // chance for directed blocking
                //    agentDrivenProperties.AIParryOnDecideAbility = MBMath.ClampFloat((meleeLevel * 0.30f) + 0.15f, 0.1f, 0.45f);
                //    agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = MBMath.ClampFloat((meleeLevel * 0.3f) - 0.05f, 0.01f, 0.25f);
                //    agentDrivenProperties.AIDecideOnAttackChance = MBMath.ClampFloat(meleeLevel + 0.1f, 0f, 0.95f);
                //    agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = MBMath.ClampFloat(meleeLevel + 0.1f, 0f, 0.95f);
                //    agentDrivenProperties.AIAttackOnParryChance = MBMath.ClampFloat(meleeLevel * 0.4f, 0.1f, 0.30f); //0.3f - 0.1f * agent.Defensiveness; //0.2-0.3f // chance to break own parry guard - 0 constant parry in reaction to enemy, 1 constant breaking of parry
                //    agentDrivenProperties.AIDecideOnAttackChance = MBMath.ClampFloat(meleeLevel * 0.3f, 0.15f, 0.5f); //0.15f * agent.Defensiveness; //0-0.15f - how often is direction changed (or swtich to parry) when preparing for attack
                //    agentDrivenProperties.AIAttackOnDecideChance = 0.5f;//MBMath.ClampFloat(0.23f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f); //0.05-1f, 0.66-line, 0.44 - shield wall - aggressiveness / chance of attack instead of anything else / when set to 0 AI never attacks on its own
                //}
                //}
                //if (RBMConfig.RBMConfig.rbmCombatEnabled)
                //{
                //    agentDrivenProperties.AiRangedHorsebackMissileRange = 0.35f; // percentage of maximum range is used, range of HA circle
                //}
                //else
                //{
                //    agentDrivenProperties.AiRangedHorsebackMissileRange = 0.235f; // percentage of maximum range is used, range of HA circle
                //}
                //agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.95f;
                //agentDrivenProperties.AiFlyingMissileCheckRadius = 250f;
                //float num4 = 1f - effectiveSkillLevel;
                //if(!agent.WieldedWeapon.IsEmpty && agent.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Crossbow)
                //{
                //    agentDrivenProperties.AiShooterError = 0.030f - (0.007f * effectiveSkillLevel);
                //}
                //else
                //{
                //    agentDrivenProperties.AiShooterError = 0.025f - (0.020f * effectiveSkillLevel);
                //}
                //if(agent.IsAIControlled)
                //{
                //    agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 0.33f;
                //    agentDrivenProperties.WeaponBestAccuracyWaitTime = 1.33f;
                //}
                //agentDrivenProperties.AiRangerLeadErrorMin = (float)((0.0 - (double)num4) * 0.349999994039536) + 0.3f;
                //agentDrivenProperties.AiRangerLeadErrorMax = num4 * 0.2f + 0.3f;
                //if (equippedItem != null && equippedItem.RelevantSkill == DefaultSkills.Bow)
                //{
                //    if(agent.MountAgent != null)
                //    {
                //        //agentDrivenProperties.AiRangerVerticalErrorMultiplier = 0f;//horse archers
                //        //agentDrivenProperties.AiRangerHorizontalErrorMultiplier = 0f;//horse archers
                //        agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
                //        agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
                //        agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 0.33f;
                //        agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 0.5f;
                //        agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.02f;
                //    }
                //    else
                //    {
                //        agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
                //        agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.020f - effectiveSkill * 0.0001f, 0.01f, 0.020f);//bow
                //    }
                //}
                //else if(equippedItem != null && equippedItem.RelevantSkill == DefaultSkills.Crossbow)
                //{
                //    agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.015f - effectiveSkill * 0.0001f, 0.005f, 0.015f);//crossbow
                //    agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.010f - effectiveSkill * 0.0001f, 0.005f, 0.010f);//crossbow
                //}
                //else
                //{
                //    agentDrivenProperties.AiRangerVerticalErrorMultiplier = MBMath.ClampFloat(0.025f - effectiveSkill * 0.0001f, 0.01f, 0.025f);// javelins and axes etc
                //    agentDrivenProperties.AiRangerHorizontalErrorMultiplier = MBMath.ClampFloat(0.025f - effectiveSkill * 0.0001f, 0.01f, 0.025f);// javelins and axes etc
                //}
                //agentDrivenProperties.AiShootFreq = MBMath.ClampFloat(effectiveSkillLevel * 1.5f, 0.1f, 0.9f); // when set to 0 AI never shoots
                //agentDrivenProperties.AiWaitBeforeShootFactor = 0f;
                //agentDrivenProperties.AiMinimumDistanceToContinueFactor = 5f; //2f + 0.3f * (3f - meleeSkill);
                //agentDrivenProperties.AIHoldingReadyMaxDuration = 0.1f; //MBMath.Lerp(0.25f, 0f, MBMath.Min(1f, num * 1.2f));
                //agentDrivenProperties.AIHoldingReadyVariationPercentage = //num;
                //agentDrivenProperties.ReloadSpeed = 0.19f; //0.12 for heavy crossbows, 0.19f for light crossbows, composite bows and longbows.
                //                GetEffectiveSkill
                //if (agent.Formation != null && agent.Formation.QuerySystem.IsInfantryFormation && !agent.IsRangedCached)
                //{
                //    agentDrivenProperties.ReloadMovementPenaltyFactor = 0.1f;
                //}
                if (agent.IsRangedCached)
                {
                    //agent.SetScriptedCombatFlags(Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation);
                    agent.SetScriptedCombatFlags(agent.GetScriptedCombatFlags() | Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation);
                    //agent.ResetAiWaitBeforeShootFactor();
                }
                //agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, 1f);
                //agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, 0f);
            }
        }
    }

    [HarmonyPatch(typeof(ArrangementOrder))]
    [HarmonyPatch("GetShieldDirectionOfUnit")]
    internal class HoldTheDoor
    {
        private static void Postfix(ref Agent.UsageDirection __result, Formation formation, Agent unit,
            ArrangementOrderEnum orderEnum)
        {
            if (unit.IsDetachedFromFormation)
            {
                __result = Agent.UsageDirection.None;
                return;
            }

            var test = true;
            switch (orderEnum)
            {
                case ArrangementOrderEnum.ShieldWall:

                    if (unit.Formation.FiringOrder.OrderEnum != FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire)
                    {
                        var hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        var hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);

                        if (hasRanged || hasTwoHanded)
                            test = false;
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
                        var hasRanged = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.HasString);
                        var hasTwoHanded = unit.Equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand);

                        if (hasRanged || hasTwoHanded)
                            test = false;
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
    internal class UpdateLastAttackAndHitTimesFix
    {
        private static bool Prefix(ref Agent __instance, Agent attackerAgent, bool isMissile)
        {
            var LastRangedHitTime = typeof(Agent).GetProperty("LastRangedHitTime");
            var _ = LastRangedHitTime?.DeclaringType?.GetProperty("LastRangedHitTime");

            var LastRangedAttackTime = typeof(Agent).GetProperty("LastRangedAttackTime");
            _ = LastRangedAttackTime?.DeclaringType?.GetProperty("LastRangedAttackTime");

            var LastMeleeHitTime = typeof(Agent).GetProperty("LastMeleeHitTime");
            _ = LastMeleeHitTime?.DeclaringType?.GetProperty("LastMeleeHitTime");

            var LastMeleeAttackTime = typeof(Agent).GetProperty("LastMeleeAttackTime");
            _ = LastMeleeAttackTime?.DeclaringType?.GetProperty("LastMeleeAttackTime");

            var currentTime = MBCommon.GetTotalMissionTime();



            if (attackerAgent != __instance && attackerAgent != null)
            {
                if (isMissile)
                    LastRangedAttackTime?.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                else
                    LastMeleeAttackTime?.SetValue(attackerAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }
            else
            {
                if (isMissile)
                    LastRangedHitTime?.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
                else
                    LastMeleeHitTime?.SetValue(__instance, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            }

            if (__instance.IsHuman || __instance.RiderAgent == null)
                return false;

            if (isMissile)
                LastRangedHitTime?.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);
            else
                LastMeleeHitTime?.SetValue(__instance.RiderAgent, currentTime, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);

            return false;
        }
    }

    [HarmonyPatch(typeof(HumanAIComponent))]
    [HarmonyPatch("OnTickAsAI")]
    internal class OnTickAsAIPatch
    {
        public static Dictionary<Agent, float> itemPickupDistanceStorage = new Dictionary<Agent, float>();

        private static void Postfix(ref SpawnedItemEntity ____itemToPickUp, ref Agent ___Agent)
        {
            if (____itemToPickUp == null || (___Agent.AIStateFlags & Agent.AIStateFlag.UseObjectMoving) == 0)
                return;

            var num = MissionGameModels.Current.AgentStatCalculateModel.GetInteractionDistance(___Agent) * 3f;
            var userFrameForAgent = ____itemToPickUp.GetUserFrameForAgent(___Agent);
            ref var origin = ref userFrameForAgent.Origin;
            var targetPoint = ___Agent.Position;
            var distanceSq = origin.DistanceSquaredWithLimit(in targetPoint, num * num + 1E-05f);

            itemPickupDistanceStorage.TryGetValue(___Agent, out var newDist);

            if (Math.Abs(0f - newDist) < .01f)
            {
                itemPickupDistanceStorage[___Agent] = distanceSq;
            }
            else
            {
                if (Math.Abs(distanceSq - newDist) < .01f)
                {
                    ___Agent.StopUsingGameObject(false);
                    itemPickupDistanceStorage.Remove(___Agent);
                }

                itemPickupDistanceStorage[___Agent] = distanceSq;
            }
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentDismount")]
    internal class OnAgentDismountPatch
    {
        private static void Postfix(Agent agent, Mission __instance)
        {
            if (agent.IsPlayerControlled || Mission.Current == null || !Mission.Current.IsFieldBattle || agent.Formation == null)
                return;

            var inf = agent.Team.GetFormation(FormationClass.Infantry);
            var rang = agent.Team.GetFormation(FormationClass.Ranged);
            var agentform = agent.Formation;

            var isInfFormationActive = inf != null && inf.CountOfUnits > 0;
            var isArcFormationActive = rang != null && rang.CountOfUnits > 0;

            if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) || agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
            {
                var distanceToInf = -1f;
                var distanceToArc = -1f;

                if (agentform != null)
                {
                    if (isInfFormationActive)
                        distanceToInf = inf.QuerySystem.MedianPosition.AsVec2.Distance(agentform.QuerySystem.MedianPosition.AsVec2);

                    if (isArcFormationActive)
                        distanceToArc = rang.QuerySystem.MedianPosition.AsVec2.Distance(agentform.QuerySystem.MedianPosition.AsVec2);
                }

                if (distanceToArc > 0f && distanceToArc < distanceToInf)
                {
                    agentform = rang;
                }
                else if (distanceToInf > 0f && distanceToInf < distanceToArc)
                {
                    agentform = inf;
                }
                else if (distanceToInf > 0f)
                {
                    agentform = inf;
                }
                else if (distanceToArc > 0f)
                {
                    agentform = rang;
                }

                agent.DisableScriptedMovement();
                agent.Formation = agentform;
            }
            else
            {
                if (agentform == null || !isInfFormationActive)
                    return;

                agent.Formation = inf;
                agent.DisableScriptedMovement();
            }
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("OnAgentMount")]
    internal class OnAgentMountPatch
    {
        private static void Postfix(Agent agent, Mission __instance)
        {
            if (Mission.Current == null || !Mission.Current.IsFieldBattle || agent.IsPlayerControlled || agent.Formation == null)
                return;

            var isCavFormationActive = agent.Team.GetFormation(FormationClass.Cavalry) != null &&
                                       agent.Team.GetFormation(FormationClass.Cavalry).CountOfUnits > 0;
            var isHaFormationActive = agent.Team.GetFormation(FormationClass.HorseArcher) != null &&
                                      agent.Team.GetFormation(FormationClass.HorseArcher).CountOfUnits > 0;

            if (agent.Equipment.HasRangedWeapon(WeaponClass.Arrow) ||
                agent.Equipment.HasRangedWeapon(WeaponClass.Bolt))
            {
                if (!isHaFormationActive || agent.Formation == null)
                    return;

                agent.Formation = agent.Team.GetFormation(FormationClass.HorseArcher);
                agent.DisableScriptedMovement();
            }
            else
            {
                if (!isCavFormationActive || agent.Formation == null)
                    return;

                agent.Formation = agent.Team.GetFormation(FormationClass.Cavalry);
                agent.DisableScriptedMovement();
            }
        }
    }

    [HarmonyPatch(typeof(Formation))]
    [HarmonyPatch("ApplyActionOnEachUnit", typeof(Action<Agent>))]
    internal class ApplyActionOnEachUnitPatch
    {
        private static bool Prefix(ref Action<Agent> action, ref Formation __instance)
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
    internal class SetHighlightForAgentsPatch
    {
        private static bool Prefix(bool highlight, ref bool useSiegeMachineUsers, ref bool useAllTeamAgents,
            Dictionary<Agent, MetaMesh> ____agentMeshes, MissionAgentLabelView __instance)
        {
            if (__instance.Mission == null)
                return true;

            var pordercont = __instance.Mission.PlayerTeam?.PlayerOrderController;

            if (pordercont == null)
            {
                var flag = __instance.Mission.PlayerTeam == null;
                Debug.Print($"PlayerOrderController is null and playerTeamIsNull: {flag}", 0, Debug.DebugColor.White, 17179869184uL);
                return true;
            }

            if (useSiegeMachineUsers)
            {
                foreach (var selectedWeapon in pordercont.SiegeWeaponController.SelectedWeapons)
                    foreach (var user in selectedWeapon.Users)
                    {
                        if (!____agentMeshes.TryGetValue(user, out var agentMesh))
                            continue;

                        var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                        method?.Invoke(__instance, new object[] { user, agentMesh, highlight });
                    }

                return false;
            }

            if (useAllTeamAgents)
            {
                foreach (var activeAgent in pordercont.Owner.Team.ActiveAgents)
                {
                    if (!____agentMeshes.TryGetValue(activeAgent, out var agentMesh))
                        continue;

                    var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                    method?.Invoke(__instance, new object[] { activeAgent, agentMesh, highlight });
                }

                return false;
            }

            foreach (var selectedFormation in pordercont.SelectedFormations)
                selectedFormation.ApplyActionOnEachUnit(delegate (Agent agent)
                {
                    if (!____agentMeshes.TryGetValue(agent, out var agentMesh))
                        return;

                    var method = typeof(MissionAgentLabelView).GetMethod("UpdateSelectionVisibility",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var _ = method?.DeclaringType?.GetMethod("UpdateSelectionVisibility");
                    method?.Invoke(__instance, new object[] { agent, agentMesh, highlight });
                });
            return false;
        }
    }
}