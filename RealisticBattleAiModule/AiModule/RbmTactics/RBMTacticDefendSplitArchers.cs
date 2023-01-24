using System.Linq;
using RBMAI;
using TaleWorlds.MountAndBlade;

public class RBMTacticDefendSplitArchers : TacticComponent
{
    private const float DefendersAdvantage = 1.1f;
    private bool _hasBattleBeenJoined;
    private Formation leftArchers;
    private Formation rightArchers;

    private int waitCountMainFormation;
    private readonly int waitCountMainFormationMax = 25;

    public RBMTacticDefendSplitArchers(Team team)
        : base(team)
    {
    }

    protected void AssignTacticFormations()
    {
        ManageFormationCounts(1, 2, 2, 1);
        _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.IsMainFormation = true;
            _mainInfantry.AI.Side = FormationAI.BehaviorSide.Middle;
        }

        var cavFormationsList = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsCavalryFormation,
            f => f.IsAIControlled, f => f.QuerySystem.FormationPower);
        if (cavFormationsList.Count > 0)
        {
            _leftCavalry = cavFormationsList[0];
            _leftCavalry.AI.Side = FormationAI.BehaviorSide.Left;
            if (cavFormationsList.Count > 1)
            {
                _rightCavalry = cavFormationsList[1];
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
        var archerFormationsList = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedFormation,
            f => f.IsAIControlled, f => f.QuerySystem.FormationPower);
        if (archerFormationsList.Count > 0)
        {
            leftArchers = archerFormationsList[0];
            leftArchers.AI.Side = FormationAI.BehaviorSide.Left;
            if (archerFormationsList.Count > 1)
            {
                rightArchers = archerFormationsList[1];
                rightArchers.AI.Side = FormationAI.BehaviorSide.Right;
            }
            else
            {
                rightArchers = null;
            }
        }
        else
        {
            leftArchers = null;
            rightArchers = null;
        }

        IsTacticReapplyNeeded = true;
    }

    protected override void ManageFormationCounts()
    {
        AssignTacticFormations();
    }

    private void Defend()
    {
        if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant) SoundTacticalHorn(MoveHornSoundIndex);
        if (_mainInfantry != null)
        {
            if (waitCountMainFormation < waitCountMainFormationMax)
            {
                _mainInfantry.AI.ResetBehaviorWeights();
                SetDefaultBehaviorWeights(_mainInfantry);
                _mainInfantry.AI.SetBehaviorWeight<BehaviorRegroup>(1.75f);
                waitCountMainFormation++;
                IsTacticReapplyNeeded = true;
            }
            else
            {
                _mainInfantry.AI.SetBehaviorWeight<BehaviorHoldHighGround>(1f);
                IsTacticReapplyNeeded = false;
            }
        }

        if (leftArchers != null)
        {
            leftArchers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(leftArchers);
            leftArchers.AI.SetBehaviorWeight<RBMBehaviorArcherFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
        }

        if (rightArchers != null)
        {
            rightArchers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(rightArchers);
            rightArchers.AI.SetBehaviorWeight<RBMBehaviorArcherFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
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
        Utilities.FixCharge(ref _mainInfantry);
        if (leftArchers != null)
        {
            leftArchers.AI.ResetBehaviorWeights();
            leftArchers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
        }

        if (rightArchers != null)
        {
            rightArchers.AI.ResetBehaviorWeights();
            rightArchers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
        }

        if (_leftCavalry != null)
        {
            _leftCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_leftCavalry);
            _leftCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            _leftCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
        }

        if (_rightCavalry != null)
        {
            _rightCavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_rightCavalry);
            _rightCavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
            _rightCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
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

        if (!num2)
        {
            if ((_mainInfantry == null ||
                 (_mainInfantry.CountOfUnits != 0 && _mainInfantry.QuerySystem.IsInfantryFormation)) &&
                (leftArchers == null || (leftArchers.CountOfUnits != 0 && leftArchers.QuerySystem.IsRangedFormation)) &&
                (rightArchers == null ||
                 (rightArchers.CountOfUnits != 0 && rightArchers.QuerySystem.IsRangedFormation)) &&
                (_leftCavalry == null ||
                 (_leftCavalry.CountOfUnits != 0 && _leftCavalry.QuerySystem.IsCavalryFormation)) &&
                (_rightCavalry == null ||
                 (_rightCavalry.CountOfUnits != 0 && _rightCavalry.QuerySystem.IsCavalryFormation)))
            {
                if (_rangedCavalry != null)
                {
                    if (_rangedCavalry.CountOfUnits != 0) return !_rangedCavalry.QuerySystem.IsRangedCavalryFormation;
                    return true;
                }

                return false;
            }

            return true;
        }

        return true;
    }

    protected override void TickOccasionally()
    {
        if (!AreFormationsCreated) return;
        var flag = HasBattleBeenJoined();
        if (CheckAndSetAvailableFormationsChanged())
        {
            _hasBattleBeenJoined = flag;
            ManageFormationCounts();
            if (_hasBattleBeenJoined)
                Engage();
            else
                Defend();
            //IsTacticReapplyNeeded = false;
        }

        if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
        {
            _hasBattleBeenJoined = flag;
            if (_hasBattleBeenJoined)
                Engage();
            else
                Defend();
            //IsTacticReapplyNeeded = false;
        }

        base.TickOccasionally();
    }

    protected override float GetTacticWeight()
    {
        if (Mission.Current != null && !Mission.Current.IsTeleportingAgents && team.TeamAI.IsCurrentTactic(this) &&
            team.QuerySystem.RangedRatio > 0.05f) return 10f;
        if (team.QuerySystem.RangedRatio > 0.2f)
            return 10f;
        return 0.2f;
    }
}