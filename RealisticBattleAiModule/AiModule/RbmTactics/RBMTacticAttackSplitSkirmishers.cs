using System;
using System.Collections.Generic;
using System.Linq;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmTactics
{
    public class RBMTacticAttackSplitSkirmishers : TacticComponent
    {
        private bool _hasBattleBeenJoined;
        protected Formation _skirmishers;
        private readonly int side = MBRandom.RandomInt(2);
        private int waitCountMainFormation;
        private const int waitCountMainFormationMax = 25;

        public RBMTacticAttackSplitSkirmishers(Team team)
            : base(team)
        {
            _hasBattleBeenJoined = false;
        }

        protected void AssignTacticFormations()
        {
            var skirmIndex = -1;
            ManageFormationCounts(2, 1, 2, 1);
            _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation,
                f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.IsMainFormation = true;
                _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;

                var skirmishersList = new List<Agent>();
                var meleeList = new List<Agent>();

                _mainInfantry.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                {
                    var isSkirmisher = Utilities.CheckIfSkirmisherAgent(agent);

                    if (isSkirmisher)
                        skirmishersList.Add(agent);
                    else
                        meleeList.Add(agent);
                });

                var infCount = 0;
                foreach (var formation in Formations)
                {
                    formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                    {
                        if (infCount == 0 || !formation.IsInfantry())
                            return;

                        var isSkirmisher = Utilities.CheckIfSkirmisherAgent(agent, 2);

                        if (isSkirmisher)
                            skirmishersList.Add(agent);
                        else
                            meleeList.Add(agent);
                        skirmIndex = infCount;
                    });
                    infCount++;
                }


                skirmishersList.Sort((x,y) 
                    => x.CharacterPowerCached.CompareTo(y.CharacterPowerCached));
                meleeList.Sort((x, y)
                    => x.CharacterPowerCached.CompareTo(y.CharacterPowerCached));

                if (skirmIndex != -1)
                {
                    var j = 0;
                    //var infCount = Formations.ElementAt(0).CountOfUnits + Formations.ElementAt(skirmIndex).CountOfUnits;
                    foreach (var agent in skirmishersList)
                    {
                        agent.Formation = Formations.ElementAt(j < infCount / 10f ? skirmIndex : 0);
                        j++;
                    }

                    //foreach (var agent in meleeList)
                    //{
                    //    agent.Formation = Formations.ElementAt(j < infCount / 10f ? skirmIndex : 0);
                    //    j++;
                    //}

                    if (Formations.ElementAtOrDefault(skirmIndex) != null)
                    {
                        team.TriggerOnFormationsChanged(Formations.ElementAt(skirmIndex));
                        team.TriggerOnFormationsChanged(Formations.ElementAt(0));
                    }
                }
            }

            _archers = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedFormation, f => f.IsAIControlled,
                f => f.QuerySystem.FormationPower).FirstOrDefault();
            var list = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsCavalryFormation, f => f.IsAIControlled,
                f => f.QuerySystem.FormationPower);
            if (list.Count > 0)
            {
                _leftCavalry = list[0];
                _leftCavalry.AI.Side = FormationAI.BehaviorSide.Left;
                if (list.Count > 1)
                {
                    _rightCavalry = list[1];
                    _rightCavalry.AI.Side = FormationAI.BehaviorSide.Right;
                }
                else
                {
                    _rightCavalry = null;
                }
            }
            else
            {
                _leftCavalry = null;
                _rightCavalry = null;
            }

            _rangedCavalry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedCavalryFormation,
                f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();

            if (skirmIndex != -1 && Formations.Count() > skirmIndex &&
                Formations.ElementAt(skirmIndex).QuerySystem.IsInfantryFormation)
            {
                _skirmishers = Formations.ElementAt(skirmIndex);
                _skirmishers.AI.IsMainFormation = false;
            }

            IsTacticReapplyNeeded = true;
        }

        protected override void ManageFormationCounts()
        {
            AssignTacticFormations();
        }

        private void Advance()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant)
                SoundTacticalHorn(MoveHornSoundIndex);

            FormationAI.BehaviorSide newside;

            if (_skirmishers != null)
            {
                _skirmishers.AI.ResetBehaviorWeights();
                if (side == 0)
                {
                    newside = FormationAI.BehaviorSide.Left;
                    _skirmishers.AI.Side = newside;
                    _skirmishers.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f).FlankSide = newside;
                }
                else
                {
                    newside = FormationAI.BehaviorSide.Right;
                    _skirmishers.AI.Side = newside;
                    _skirmishers.AI.SetBehaviorWeight<RBMBehaviorForwardSkirmish>(1f).FlankSide = newside;
                }
            }

            if (_mainInfantry != null)
            {
                if (waitCountMainFormation < waitCountMainFormationMax)
                {
                    _mainInfantry.AI.ResetBehaviorWeights();
                    SetDefaultBehaviorWeights(_mainInfantry);
                    _mainInfantry.AI.SetBehaviorWeight<BehaviorRegroup>(1.5f);
                    waitCountMainFormation++;
                    IsTacticReapplyNeeded = true;
                }
                else
                {
                    _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
                    IsTacticReapplyNeeded = false;
                }
            }

            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_archers);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmish>(0f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorRegroup>(1.25f);
            }

            if (_leftCavalry != null)
            {
                newside = FormationAI.BehaviorSide.Left;

                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = newside;
                _leftCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            }

            if (_rightCavalry != null)
            {
                newside = FormationAI.BehaviorSide.Right;

                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = newside;
                _rightCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            }

            if (_rangedCavalry != null)
            {
                _rangedCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rangedCavalry);
                _rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            }
        }

        private void Attack()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant)
                SoundTacticalHorn(AttackHornSoundIndex);


            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                _mainInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
            }

            if (_skirmishers != null)
            {
                _skirmishers.AI.ResetBehaviorWeights();
                _skirmishers.AI.SetBehaviorWeight<BehaviorCharge>(1f);
            }

            if (_archers != null)
            {
                _archers.AI.ResetBehaviorWeights();
                _archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
                _archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
                _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
            }

            if (_leftCavalry != null)
            {
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                _leftCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                _rightCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            }

            if (_rangedCavalry != null)
            {
                _rangedCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rangedCavalry);
                _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            }

            IsTacticReapplyNeeded = false;
        }

        private bool HasBattleBeenJoined()
        {
            return Utilities.HasBattleBeenJoined(_mainInfantry, _hasBattleBeenJoined, 85f);
        }

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            var num = Formations.Count(f => f.IsAIControlled);
            var num2 = num != _AIControlledFormationCount;
            if (num2)
            {
                _AIControlledFormationCount = num;
                IsTacticReapplyNeeded = true;
            }

            else
            {
                if ((_mainInfantry == null || (_mainInfantry.CountOfUnits != 0 && _mainInfantry.QuerySystem.IsInfantryFormation)) 
                    && (_archers == null || (_archers.CountOfUnits != 0 && _archers.QuerySystem.IsRangedFormation)) 
                    && (_leftCavalry == null || (_leftCavalry.CountOfUnits != 0 && _leftCavalry.QuerySystem.IsCavalryFormation)) 
                    && (_rightCavalry == null || (_rightCavalry.CountOfUnits != 0 && _rightCavalry.QuerySystem.IsCavalryFormation)))
                {
                    if (_rangedCavalry == null) 
                        return false;

                    if (_rangedCavalry.CountOfUnits != 0) 
                        return !_rangedCavalry.QuerySystem.IsRangedCavalryFormation;

                    return true;

                }

                return true;
            }

            return true;
        }

        protected override void TickOccasionally()
        {
            if (!AreFormationsCreated) return;
            if (CheckAndSetAvailableFormationsChanged())
            {
                ManageFormationCounts();
                if (_hasBattleBeenJoined)
                    Attack();
                else
                    Advance();
            }

            var flag = HasBattleBeenJoined();
            if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                _hasBattleBeenJoined = flag;
                if (_hasBattleBeenJoined)
                    Attack();
                else
                    Advance();
            }

            base.TickOccasionally();
        }

        protected override float GetTacticWeight()
        {
            float skirmisherCount = 0;

            var allyInfatryPower = 0f;
            var enemyInfatryPower = 0f;
            var allyInfCount = 0;

            foreach (var team_ in Mission.Current.Teams)
                if (team_.IsEnemyOf(this.team))
                    foreach (var formation in team_.Formations)
                        if (formation.QuerySystem.IsInfantryFormation)
                            enemyInfatryPower += formation.QuerySystem.FormationPower;
            foreach (var team in Mission.Current.Teams)
                if (!team.IsEnemyOf(this.team))
                    foreach (var formation in team.Formations)
                        if (formation.QuerySystem.IsInfantryFormation)
                        {
                            allyInfatryPower += formation.QuerySystem.FormationPower;
                            allyInfCount += formation.CountOfUnits;
                        }

            if (allyInfatryPower < enemyInfatryPower * 1.25f || allyInfCount < 60) return 0.01f;

            foreach (var agent in team.ActiveAgents)
                if (agent.Formation != null && agent.Formation.QuerySystem.IsInfantryFormation)
                    if (Utilities.CheckIfSkirmisherAgent(agent, 2))
                        skirmisherCount++;

            var num = team.QuerySystem.RangedCavalryRatio * team.QuerySystem.MemberCount;
            var skirmisherRatio = skirmisherCount / allyInfCount;
            if (team.QuerySystem.InfantryRatio > 0.45f)
                return team.QuerySystem.InfantryRatio * skirmisherRatio * 1.7f * team.QuerySystem.MemberCount /
                    (team.QuerySystem.MemberCount - num) * (float)Math.Sqrt(team.QuerySystem.TotalPowerRatio);
            return 0.01f;
        }
    }
}