using System.Collections.Generic;
using System.Linq;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmTactics
{
    public class RBMTacticDefendSplitInfantry : TacticComponent
    {
        protected Formation _flankingInfantry;

        private bool _hasBattleBeenJoined;
        protected Formation _leftFlankingInfantry;
        protected Formation _rightFlankingInfantry;
        private readonly int side = MBRandom.RandomInt(2);

        public RBMTacticDefendSplitInfantry(Team team)
            : base(team)
        {
            _hasBattleBeenJoined = false;
        }

        protected void AssignTacticFormations()
        {
            var leftflankersIndex = -1;
            var rightflankersIndex = -1;

            ManageFormationCounts(3, 1, 2, 1);

            _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation,
                f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();

            if (_mainInfantry != null && _mainInfantry.IsAIControlled)
            {
                _mainInfantry.AI.IsMainFormation = true;
                _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;

                var flankersList = new List<Agent>();

                var i = 0;
                foreach (var formation in Formations)
                {
                    formation.ApplyActionOnEachUnitViaBackupList(delegate(Agent agent)
                    {
                        if (!formation.IsInfantry()) 
                            return;

                        var isFlanker = agent.WieldedWeapon.CurrentUsageItem?.WeaponClass == WeaponClass.TwoHandedAxe ||
                                        agent.WieldedWeapon.CurrentUsageItem?.WeaponClass == WeaponClass.TwoHandedPolearm;

                        if (isFlanker)
                            flankersList.Add(agent);
                    });
                    if (i != 0)
                    {
                        if (leftflankersIndex != -1)
                        {
                            if (formation.QuerySystem.IsInfantryFormation) rightflankersIndex = i;
                        }
                        else
                        {
                            if (formation.QuerySystem.IsInfantryFormation) leftflankersIndex = i;
                        }
                    }

                    i++;
                }

                flankersList.Sort((x,y) 
                    => x.CharacterPowerCached.CompareTo(y.CharacterPowerCached));
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

            if (leftflankersIndex != -1 && Formations.Count() > leftflankersIndex &&
                Formations.ElementAt(leftflankersIndex).QuerySystem.IsInfantryFormation)
            {
                _leftFlankingInfantry = Formations.ElementAt(leftflankersIndex);
                _leftFlankingInfantry.AI.IsMainFormation = false;
            }

            if (rightflankersIndex != -1 && Formations.Count() > rightflankersIndex &&
                Formations.ElementAt(rightflankersIndex).QuerySystem.IsInfantryFormation)
            {
                _rightFlankingInfantry = Formations.ElementAt(rightflankersIndex);
                _rightFlankingInfantry.AI.IsMainFormation = false;
            }

            foreach (var formation in Formations)
            {
                if (formation.CountOfUnits != 1)
                    continue;

                formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                {
                    if ( _mainInfantry != null && !agent.HasMount && !agent.IsRangedCached)
                        agent.Formation = _mainInfantry;
                });
            }

            

            IsTacticReapplyNeeded = true;
        }

        protected override void ManageFormationCounts()
        {
            AssignTacticFormations();
        }

        private void Defend()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant) 
                SoundTacticalHorn(MoveHornSoundIndex);

            if (_flankingInfantry != null)
            {
                _flankingInfantry.AI.ResetBehaviorWeights();
                if (side == 0)
                {
                    _flankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide =
                        FormationAI.BehaviorSide.Left;
                    _flankingInfantry.AI.Side = FormationAI.BehaviorSide.Left;
                }
                else
                {
                    _flankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide =
                        FormationAI.BehaviorSide.Right;
                    _flankingInfantry.AI.Side = FormationAI.BehaviorSide.Right;
                }
            }

            if (_leftFlankingInfantry != null)
            {
                _leftFlankingInfantry.AI.ResetBehaviorWeights();
                _leftFlankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(5f).FlankSide = FormationAI.BehaviorSide.Left;
                _leftFlankingInfantry.AI.Side = FormationAI.BehaviorSide.Left;
            }

            if (_rightFlankingInfantry != null)
            {
                _rightFlankingInfantry.AI.ResetBehaviorWeights();
                _rightFlankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(5f).FlankSide = FormationAI.BehaviorSide.Right;
                _rightFlankingInfantry.AI.Side = FormationAI.BehaviorSide.Right;
            }

            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                _mainInfantry.AI.SetBehaviorWeight<BehaviorHoldHighGround>(1f);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorRegroup>(1.75f);
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
                _leftCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_leftCavalry);
                _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
            }

            if (_rightCavalry != null)
            {
                _rightCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rightCavalry);
                _rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
            }

            if (_rangedCavalry != null)
            {
                _rangedCavalry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_rangedCavalry);
                _rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
                _rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
            }
        }

        private void Engage()
        {
            if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant)
                SoundTacticalHorn(AttackHornSoundIndex);
            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                _mainInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
            }

            if (_leftFlankingInfantry != null)
            {
                _leftFlankingInfantry.AI.ResetBehaviorWeights();
                _leftFlankingInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                _leftFlankingInfantry.AI.Side = FormationAI.BehaviorSide.Left;
            }

            if (_rightFlankingInfantry != null)
            {
                _rightFlankingInfantry.AI.ResetBehaviorWeights();
                _rightFlankingInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                _rightFlankingInfantry.AI.Side = FormationAI.BehaviorSide.Right;
            }

            if (_flankingInfantry != null)
            {
                _flankingInfantry.AI.ResetBehaviorWeights();
                _flankingInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
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
        }

        private bool HasBattleBeenJoined()
        {
            return Utilities.HasBattleBeenJoined(_mainInfantry, _hasBattleBeenJoined);
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
                    && (_leftFlankingInfantry == null || (_leftFlankingInfantry.CountOfUnits != 0 && _leftFlankingInfantry.QuerySystem.IsInfantryFormation)) 
                    && (_rightFlankingInfantry == null || (_rightFlankingInfantry.CountOfUnits != 0 && _rightFlankingInfantry.QuerySystem.IsInfantryFormation)) 
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
                    Engage();
                else
                    Defend();
                IsTacticReapplyNeeded = false;
            }

            var flag = HasBattleBeenJoined();
            if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                _hasBattleBeenJoined = flag;
                if (_hasBattleBeenJoined)
                    Engage();
                else
                    Defend();
                IsTacticReapplyNeeded = false;
            }

            base.TickOccasionally();
        }

        protected override float GetTacticWeight()
        {
            var allyInfatryPower = 0f;
            var enemyInfatryPower = 0f;

            var allyInfCount = 0;

            foreach (var team_ in Mission.Current.Teams)
                if (team_.IsEnemyOf(this.team))
                    foreach (var formation in team_.Formations)
                    {
                        if (formation.QuerySystem.IsInfantryFormation)
                            enemyInfatryPower += formation.QuerySystem.FormationPower;
                        if (formation.QuerySystem.IsRangedFormation)
                        {
                        }
                    }

            foreach (var team_ in Mission.Current.Teams)
                if (!team_.IsEnemyOf(this.team))
                    foreach (var formation in team_.Formations)
                    {
                        if (formation.QuerySystem.IsInfantryFormation)
                        {
                            allyInfatryPower += formation.QuerySystem.FormationPower;
                            allyInfCount += formation.CountOfUnits;
                        }

                        if (formation.QuerySystem.IsCavalryFormation)
                        {
                        }
                    }

            return allyInfatryPower > enemyInfatryPower && allyInfCount > 60 ? 
                5f : 
                0.01f;
        }
    }
}