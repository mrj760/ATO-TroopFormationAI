using System.Collections.Generic;
using System.Linq;
using RBMAI.AiModule.RbmBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RBMAI.AiModule.RbmTactics
{
    public class RBMTacticAttackSplitInfantry : TacticComponent
    {
        protected Formation _flankingInfantry;

        private bool _hasBattleBeenJoined;
        protected Formation _leftFlankingInfantry;
        protected Formation _rightFlankingInfantry;
        private readonly int side = MBRandom.RandomInt(2);

        public RBMTacticAttackSplitInfantry(Team team)
            : base(team)
        {
            _hasBattleBeenJoined = false;
        }

        protected void AssignTacticFormations()
        {
            var leftflankersIndex = -1;
            var rightflankersIndex = -1;
            var infCount = Formations.Where(formation => formation.IsInfantry()).Sum(formation => formation.CountOfUnits);
            ManageFormationCounts(3, 1, 2, 1);

            if (Formations.Any() && Formations.FirstOrDefault().QuerySystem.IsInfantryFormation)
                _mainInfantry = Formations.FirstOrDefault();
            else
                _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation,
                    f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();

            if (_mainInfantry != null && _mainInfantry.IsAIControlled)
            {
                _mainInfantry.AI.IsMainFormation = true;
                _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;

                var flankersList = new List<Agent>();
                var mainList = new List<Agent>();


                var i = 0;
                foreach (var formation in Formations)
                {
                    var formation1 = formation;
                    formation.ApplyActionOnEachUnitViaBackupList(delegate (Agent agent)
                    {
                        if (!formation1.IsInfantry() || !formation1.IsAIControlled)
                            return;

                        var wepClass = agent.WieldedWeapon.CurrentUsageItem?.WeaponClass;
                        var isFlanker = wepClass == WeaponClass.TwoHandedAxe || wepClass == WeaponClass.TwoHandedPolearm;

                        if (isFlanker)
                            flankersList.Add(agent);
                        else
                            mainList.Add(agent);

                        if (i == 0) return;

                        if (leftflankersIndex != -1)
                            rightflankersIndex = i;
                        else
                            leftflankersIndex = i;
                    });
                    ++i;
                }

                var flankers = flankersList.OrderBy(o => o.CharacterPowerCached);

                var j = 0;

                if (leftflankersIndex > 0 && rightflankersIndex > 0)
                {
                    foreach (var agent in flankers)
                    {
                        if (j < infCount / 8)
                            agent.Formation = Formations.ElementAt(leftflankersIndex);
                        else if (j < infCount / 4)
                            agent.Formation = Formations.ElementAt(rightflankersIndex);
                        else
                            agent.Formation = Formations.FirstOrDefault();

                        ++j;
                    }

                    foreach (var agent in mainList)
                    {
                        if (j < infCount / 8)
                            agent.Formation = Formations.ElementAt(leftflankersIndex);
                        else if (j < infCount / 4)
                            agent.Formation = Formations.ElementAt(rightflankersIndex);
                        else
                            agent.Formation = Formations.FirstOrDefault();

                        ++j;
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

            if (leftflankersIndex != -1
                && Formations.Count() > leftflankersIndex
                && Formations.ElementAt(leftflankersIndex).QuerySystem.IsInfantryFormation)
            {
                _leftFlankingInfantry = Formations.ElementAt(leftflankersIndex);
                _leftFlankingInfantry.AI.IsMainFormation = false;
            }

            if (rightflankersIndex != -1
                && Formations.Count() > rightflankersIndex
                && Formations.ElementAt(rightflankersIndex).QuerySystem.IsInfantryFormation)
            {
                _rightFlankingInfantry = Formations.ElementAt(rightflankersIndex);
                _rightFlankingInfantry.AI.IsMainFormation = false;
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
            {
                SoundTacticalHorn(MoveHornSoundIndex);
            }

            FormationAI.BehaviorSide newside;

            if (_flankingInfantry != null)
            {
                newside = side == 0 ?
                        FormationAI.BehaviorSide.Left :
                        FormationAI.BehaviorSide.Right;

                _flankingInfantry.AI.ResetBehaviorWeights();
                _flankingInfantry.AI.SetBehaviorWeight<RBMBehaviorInfantryAttackFlank>(1f).FlankSide = newside;
                _flankingInfantry.AI.Side = newside;
            }

            if (_leftFlankingInfantry != null)
            {
                newside = FormationAI.BehaviorSide.Left;

                _leftFlankingInfantry.AI.ResetBehaviorWeights();
                _leftFlankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(5f).FlankSide = newside;
                _leftFlankingInfantry.AI.Side = newside;
            }

            if (_rightFlankingInfantry != null)
            {
                newside = FormationAI.BehaviorSide.Right;

                _rightFlankingInfantry.AI.ResetBehaviorWeights();
                _rightFlankingInfantry.AI.SetBehaviorWeight<BehaviorProtectFlank>(5f).FlankSide = newside;
                _rightFlankingInfantry.AI.Side = newside;
            }

            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorRegroup>(1.5f);
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

            FormationAI.BehaviorSide newside;

            if (_mainInfantry != null)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                _mainInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
            }

            if (_leftFlankingInfantry != null)
            {
                newside = FormationAI.BehaviorSide.Left;

                _leftFlankingInfantry.AI.ResetBehaviorWeights();
                _leftFlankingInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                _leftFlankingInfantry.AI.Side = newside;
            }

            if (_rightFlankingInfantry != null)
            {
                newside = FormationAI.BehaviorSide.Right;

                _rightFlankingInfantry.AI.ResetBehaviorWeights();
                _rightFlankingInfantry.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                _rightFlankingInfantry.AI.Side = newside;
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
            return Utilities.HasBattleBeenJoined(_mainInfantry, _hasBattleBeenJoined, 70f);
        }

        protected override bool CheckAndSetAvailableFormationsChanged()
        {
            var num = Formations.Count(f => f.IsAIControlled);
            var num2 = num != _AIControlledFormationCount;
            if (num2)
            {
                _AIControlledFormationCount = num;
                IsTacticReapplyNeeded = true;
                return true;
            }

            if ((_mainInfantry != null && (_mainInfantry.CountOfUnits == 0 || !_mainInfantry.QuerySystem.IsInfantryFormation))
                || (_leftFlankingInfantry != null && (_leftFlankingInfantry.CountOfUnits == 0 || !_leftFlankingInfantry.QuerySystem.IsInfantryFormation))
                || (_rightFlankingInfantry != null && (_rightFlankingInfantry.CountOfUnits == 0 || !_rightFlankingInfantry.QuerySystem.IsInfantryFormation))
                || (_archers != null && (_archers.CountOfUnits == 0 || !_archers.QuerySystem.IsRangedFormation))
                || (_leftCavalry != null && (_leftCavalry.CountOfUnits == 0 || !_leftCavalry.QuerySystem.IsCavalryFormation))
                || (_rightCavalry != null && (_rightCavalry.CountOfUnits == 0 || !_rightCavalry.QuerySystem.IsCavalryFormation)))
                return true;

            if (_rangedCavalry == null)
                return false;

            if (_rangedCavalry.CountOfUnits != 0)
                return !_rangedCavalry.QuerySystem.IsRangedCavalryFormation;

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
                IsTacticReapplyNeeded = false;
            }

            var flag = HasBattleBeenJoined();
            if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
            {
                _hasBattleBeenJoined = flag;
                if (_hasBattleBeenJoined)
                    Attack();
                else
                    Advance();
                IsTacticReapplyNeeded = false;
            }

            base.TickOccasionally();
        }

        protected override float GetTacticWeight()
        {
            var allyInfatryPower = 0f;

            var allyInfCount = 0;

            var enemyInfatryPower =
                Mission.Current.Teams
                    .Where(team_ => team_.IsEnemyOf(this.team))
                    .SelectMany(team_ => team_.Formations)
                    .Where(formation => formation.QuerySystem.IsInfantryFormation)
                    .Sum(formation => formation.QuerySystem.FormationPower);

            foreach (var formation in Mission.Current.Teams
                         .Where(team_ => !team_.IsEnemyOf(this.team))
                         .SelectMany(team_ => team_.Formations))
            {
                if (!formation.QuerySystem.IsInfantryFormation) continue;

                allyInfatryPower += formation.QuerySystem.FormationPower;
                allyInfCount += formation.CountOfUnits;
            }

            return
                allyInfatryPower > enemyInfatryPower * 1.25f
                && allyInfCount > 60
                    ? 10f
                    : 0.01f;
        }
    }
}