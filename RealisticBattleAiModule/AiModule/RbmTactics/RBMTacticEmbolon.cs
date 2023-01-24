using System.Linq;
using RBMAI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

public class RBMTacticEmbolon : TacticComponent
{
    private Formation _cavalry;

    private bool _hasBattleBeenJoined;

    public RBMTacticEmbolon(Team team)
        : base(team)
    {
    }

    protected override void ManageFormationCounts()
    {
        ManageFormationCounts(1, 1, 1, 1);
        _mainInfantry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsInfantryFormation,
            f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
        if (_mainInfantry != null) _mainInfantry.AI.IsMainFormation = true;
        _archers = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedFormation, f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower).FirstOrDefault();
        _cavalry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsCavalryFormation, f => f.IsAIControlled,
            f => f.QuerySystem.FormationPower).FirstOrDefault();
        _rangedCavalry = ChooseAndSortByPriority(Formations, f => f.QuerySystem.IsRangedCavalryFormation,
            f => f.IsAIControlled, f => f.QuerySystem.FormationPower).FirstOrDefault();
    }

    private void Advance()
    {
        if (team.IsPlayerTeam && !team.IsPlayerGeneral && team.IsPlayerSergeant) SoundTacticalHorn(MoveHornSoundIndex);
        if (_mainInfantry != null)
        {
            _mainInfantry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_mainInfantry);
            _mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
        }

        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_archers);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
        }

        if (_cavalry != null)
        {
            _cavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_cavalry);
            _cavalry.AI.SetBehaviorWeight<RBMBehaviorEmbolon>(1.5f);
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
        Utilities.FixCharge(ref _mainInfantry);
        if (_archers != null)
        {
            _archers.AI.ResetBehaviorWeights();
            _archers.AI.SetBehaviorWeight<RBMBehaviorArcherSkirmish>(1f);
            _archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(0f);
            _archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0f);
        }

        if (_cavalry != null)
        {
            _cavalry.AI.ResetBehaviorWeights();
            SetDefaultBehaviorWeights(_cavalry);
            _cavalry.AI.SetBehaviorWeight<RBMBehaviorCavalryCharge>(1f);
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
        }

        if (!num2)
        {
            if ((_mainInfantry == null ||
                 (_mainInfantry.CountOfUnits != 0 && _mainInfantry.QuerySystem.IsInfantryFormation)) &&
                (_archers == null || (_archers.CountOfUnits != 0 && _archers.QuerySystem.IsRangedFormation)) &&
                (_cavalry == null || (_cavalry.CountOfUnits != 0 && _cavalry.QuerySystem.IsCavalryFormation)))
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
        float heavyCavCount = 0;
        float cavCount = 0;
        foreach (var agent in team?.ActiveAgents?.ToList())
            if (agent.Formation != null && agent.Formation.QuerySystem.IsCavalryFormation)
                if (agent.HasMount)
                {
                    if (agent.Character?.Level >= 21)
                        if (agent.MountAgent != null)
                        {
                            var equipmentElement = agent.SpawnEquipment[EquipmentIndex.HorseHarness];
                            if (equipmentElement.Item != null &&
                                equipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HorseHarness)
                                if (equipmentElement.GetModifiedArmArmor() > 40f)
                                    heavyCavCount++;
                        }

                    cavCount++;
                }

        if (team.QuerySystem.CavalryRatio > 0.2f && heavyCavCount / cavCount >= 0.6f)
            return 5f;
        return 0.01f;
        //float num = team.QuerySystem.RangedCavalryRatio * (float)team.QuerySystem.MemberCount;
        //return team.QuerySystem.CavalryRatio * (float)team.QuerySystem.MemberCount / ((float)team.QuerySystem.MemberCount - num) * MathF.Sqrt(team.QuerySystem.RemainingPowerRatio);
    }
}