using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Utilities = RBMAI.Utilities;

public class RBMBehaviorCavalryCharge : BehaviorComponent
{
    private Vec2 _bracePosition = Vec2.Invalid;

    private ChargeState _chargeState;

    private Timer _chargeTimer;

    private Timer _chargingPastTimer;

    private float _desiredChargeStopDistance;

    private Vec2 _initialChargeDirection;

    private WorldPosition _lastReformDestination;

    private FormationQuerySystem _lastTarget;

    private Timer _reformTimer;

    public bool ChargeArchers = true;
    public bool ChargeCavalry = false;
    public bool ChargeHorseArchers = false;
    public bool ChargeInfantry = true;
    public bool isFirstCharge = true;

    public bool newTarget;

    public RBMBehaviorCavalryCharge(Formation formation)
        : base(formation)
    {
        _lastTarget = null;
        CurrentOrder = MovementOrder.MovementOrderCharge;
        CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
        _chargeState = ChargeState.Charging;
        BehaviorCoherence = 0.5f;
        _desiredChargeStopDistance = 110f;
    }

    public override float NavmeshlessTargetPositionPenalty => 1f;

    public override void TickOccasionally()
    {
        base.TickOccasionally();
        if (Formation.AI.ActiveBehavior == this)
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }
    }

    private ChargeState CheckAndChangeState()
    {
        var _currentTacticField =
            typeof(TeamAIComponent).GetField("_currentTactic", BindingFlags.NonPublic | BindingFlags.Instance);
        _currentTacticField.DeclaringType.GetField("_currentTactic");
        //TacticComponent _currentTactic = (TacticComponent);

        if (_currentTacticField.GetValue(Formation?.Team?.TeamAI).ToString().Contains("Embolon"))
        {
            _desiredChargeStopDistance = 50f;
            BehaviorCoherence = 0.85f;
        }

        var result = _chargeState;
        if (Formation.QuerySystem.ClosestEnemyFormation == null)
            result = ChargeState.Undetermined;
        else
            switch (_chargeState)
            {
                case ChargeState.Undetermined:
                {
                    if (Formation.QuerySystem.ClosestEnemyFormation != null &&
                        ((!Formation.QuerySystem.IsCavalryFormation &&
                          !Formation.QuerySystem.IsRangedCavalryFormation) ||
                         Formation.QuerySystem.AveragePosition.Distance(Formation.QuerySystem.ClosestEnemyFormation
                             .MedianPosition.AsVec2) / Formation.QuerySystem.MovementSpeedMaximum <=
                         5f)) result = ChargeState.Charging;
                    break;
                }
                case ChargeState.Charging:
                {
                    if (_lastTarget == null || _lastTarget.Formation.CountOfUnits == 0)
                    {
                        Formation correctEnemy = null;
                        if (ChargeInfantry)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, true, false, false, false, false);
                        else if (ChargeArchers)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, false, true, false, false, false);
                        else if (ChargeCavalry)
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, false, false, true, true, true);
                        else
                            correctEnemy = Utilities.FindSignificantEnemy(Formation, true, true, false, false, false);
                        if (correctEnemy != null)
                            _lastTarget = correctEnemy.QuerySystem;
                        else
                            _lastTarget = Formation.QuerySystem.ClosestEnemyFormation;
                        newTarget = true;
                        _initialChargeDirection =
                            _lastTarget.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition;
                        //result = ChargeState.Undetermined;
                    }
                    else if (_initialChargeDirection.DotProduct(_lastTarget.MedianPosition.AsVec2 -
                                                                Formation.QuerySystem.AveragePosition) <= 0f)
                    {
                        if (_chargeTimer == null) _chargeTimer = new Timer(Mission.Current.CurrentTime, 3f);
                        //result = ChargeState.ChargingPast;
                    }

                    if (Formation.QuerySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents < 5f)
                    {
                        result = ChargeState.ChargingPast;
                        _chargeTimer = null;
                    }

                    if (_chargeTimer != null && _chargeTimer.Check(Mission.Current.CurrentTime))
                    {
                        result = ChargeState.ChargingPast;
                        _chargeTimer = null;
                    }

                    break;
                }
                case ChargeState.ChargingPast:
                {
                    var formationCoherence =
                        (Formation.QuerySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents + 1f) /
                        (Formation.QuerySystem.IdealAverageDisplacement + 1f);
                    if (_chargingPastTimer.Check(Mission.Current.CurrentTime) ||
                        Formation.QuerySystem.AveragePosition.Distance(_lastTarget.MedianPosition.AsVec2) >=
                        _desiredChargeStopDistance + _lastTarget.Formation.Depth)
                    {
                        if (Formation.QuerySystem.AveragePosition.Distance(_lastTarget.MedianPosition.AsVec2) >=
                            _desiredChargeStopDistance + _lastTarget.Formation.Depth)
                            _lastReformDestination = Formation.QuerySystem.MedianPosition;
                        result = ChargeState.Reforming;
                    }

                    break;
                }
                case ChargeState.Reforming:
                {
                    if (_reformTimer.Check(Mission.Current.CurrentTime) ||
                        Formation.QuerySystem.FormationIntegrityData.DeviationOfPositionsExcludeFarAgents < 12f ||
                        Formation.QuerySystem.UnderRangedAttackRatio >
                        0.2f) //|| base.Formation.QuerySystem.AveragePosition.Distance(_lastTarget.MedianPosition.AsVec2) <= 30f)
                    {
                        CheckForNewChargeTarget();
                        result = ChargeState.Charging;
                        if (_lastTarget != null && _lastTarget.Formation != null)
                            Formation.FormOrder = FormOrder.FormOrderCustom(_lastTarget.Formation.Width);
                    }

                    break;
                }
                case ChargeState.Bracing:
                {
                    var flag = false;
                    if (!flag)
                    {
                        _bracePosition = Vec2.Invalid;
                        _chargeState = ChargeState.Charging;
                    }

                    break;
                }
            }

        return result;
    }

    public void CheckForNewChargeTarget()
    {
        var correctEnemy = Utilities.FindSignificantEnemy(Formation, true, true, false, false, false);
        if (correctEnemy != null)
            _lastTarget = correctEnemy.QuerySystem;
        else
            _lastTarget = Formation.QuerySystem.ClosestEnemyFormation;
        newTarget = true;
        _initialChargeDirection = _lastTarget.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition;
    }

    protected override void CalculateCurrentOrder()
    {
        var allyTeam = Formation.Team;
        var shouldSimpleCharge = false;
        //int countOfEnemyTeams = 0;
        //int countOfAllyTeams = 0;
        //int countOfSimpleChargeEnemy = 0;
        //int countOfSimpleChargeAlly = 0;
        //      foreach (Team team in Mission.Current.Teams.ToList())
        //      {
        //          if (team.IsEnemyOf(allyTeam))
        //          {
        //              countOfEnemyTeams++;
        //              if (team.QuerySystem.InfantryRatio <= 0.1f && team.QuerySystem.RangedRatio <= 0.1f)
        //		{
        //                  countOfSimpleChargeEnemy++;

        //              }
        //          }
        //      }
        //      foreach (Team team in Mission.Current.Teams.ToList())
        //      {
        //          if (!team.IsEnemyOf(allyTeam))
        //          {
        //              countOfAllyTeams++;
        //              if (team.QuerySystem.InfantryRatio <= 0.1f && team.QuerySystem.RangedRatio <= 0.1f)
        //              {
        //                  countOfSimpleChargeAlly++;

        //              }
        //          }
        //      }
        //      if (countOfEnemyTeams == countOfSimpleChargeEnemy || countOfAllyTeams == countOfSimpleChargeAlly)
        //{
        //	shouldSimpleCharge = true;
        //      }
        if (Formation.QuerySystem.ClosestEnemyFormation == null || shouldSimpleCharge)
        {
            CurrentOrder = MovementOrder.MovementOrderCharge;
            return;
        }

        var chargeState = CheckAndChangeState();

        if (isFirstCharge)
        {
            var vec4 = (_lastTarget.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized();
            var medianPosition3 = _lastTarget.MedianPosition;
            var vec5 = medianPosition3.AsVec2 + vec4 * (_desiredChargeStopDistance + _lastTarget.Formation.Depth);
            medianPosition3.SetVec2(vec5);
            _lastReformDestination = medianPosition3;
            CurrentOrder = MovementOrder.MovementOrderMove(medianPosition3);
            CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec4);
            isFirstCharge = false;
        }

        if (chargeState != _chargeState || newTarget)
        {
            _chargeState = chargeState;

            switch (_chargeState)
            {
                case ChargeState.Undetermined:
                {
                    CurrentOrder = MovementOrder.MovementOrderCharge;
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;
                }
                case ChargeState.Charging:
                {
                    CheckForNewChargeTarget();
                    var vec4 = (_lastTarget.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized();
                    var medianPosition3 = _lastTarget.MedianPosition;
                    var vec5 = medianPosition3.AsVec2 +
                               vec4 * (_desiredChargeStopDistance + _lastTarget.Formation.Depth);
                    medianPosition3.SetVec2(vec5);
                    _lastReformDestination = medianPosition3;
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition3);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec4);
                    break;
                }
                case ChargeState.ChargingPast:
                {
                    _chargingPastTimer = new Timer(Mission.Current.CurrentTime, 19f);
                    //Vec2 vec2 = (base.Formation.QuerySystem.AveragePosition - _lastTarget.MedianPosition.AsVec2).Normalized();
                    //_lastReformDestination = _lastTarget.MedianPosition;
                    //Vec2 vec3 = _lastTarget.MedianPosition.AsVec2 + vec2 * (_desiredChargeStopDistance + _lastTarget.Formation.Depth);
                    //_lastReformDestination.SetVec2(vec3);
                    CurrentOrder = MovementOrder.MovementOrderMove(_lastReformDestination);
                    //CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(_initialChargeDirection);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;
                }
                case ChargeState.Reforming:
                    _reformTimer = new Timer(Mission.Current.CurrentTime, 10f);
                    CurrentOrder = MovementOrder.MovementOrderMove(_lastReformDestination);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    break;
                case ChargeState.Bracing:
                {
                    var medianPosition = Formation.QuerySystem.MedianPosition;
                    medianPosition.SetVec2(_bracePosition);
                    CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                    break;
                }
            }

            newTarget = false;
        }
    }

    protected override void OnBehaviorActivatedAux()
    {
        CalculateCurrentOrder();
        Formation.SetMovementOrder(CurrentOrder);
        Formation.FacingOrder = CurrentFacingOrder;
        Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
        Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
        Formation.FormOrder = FormOrder.FormOrderWide;
        Formation.WeaponUsageOrder = WeaponUsageOrder.WeaponUsageOrderUseAny;
    }

    public override TextObject GetBehaviorString()
    {
        var behaviorString = base.GetBehaviorString();
        if (Formation.QuerySystem.ClosestEnemyFormation != null)
        {
            behaviorString.SetTextVariable("AI_SIDE",
                GameTexts.FindText("str_formation_ai_side_strings",
                    Formation.QuerySystem.ClosestEnemyFormation.Formation.AI.Side.ToString()));
            behaviorString.SetTextVariable("CLASS",
                GameTexts.FindText("str_formation_class_string",
                    Formation.QuerySystem.ClosestEnemyFormation.Formation.PrimaryClass.GetName()));
        }

        return behaviorString;
    }

    protected override float GetAiWeight()
    {
        var querySystem = Formation.QuerySystem;
        if (querySystem.ClosestEnemyFormation == null) return 0f;
        var num = querySystem.AveragePosition.Distance(querySystem.ClosestEnemyFormation.MedianPosition.AsVec2) /
                  querySystem.MovementSpeedMaximum;
        float num3;
        if (!querySystem.IsCavalryFormation && !querySystem.IsRangedCavalryFormation)
        {
            var num2 = MBMath.ClampFloat(num, 4f, 10f);
            num3 = MBMath.Lerp(0.8f, 1f, 1f - (num2 - 4f) / 6f);
        }
        else if (num <= 4f)
        {
            var num4 = MBMath.ClampFloat(num, 0f, 4f);
            num3 = MBMath.Lerp(0.8f, 1.2f, num4 / 4f);
        }
        else
        {
            var num5 = MBMath.ClampFloat(num, 4f, 10f);
            num3 = MBMath.Lerp(0.8f, 1.2f, 1f - (num5 - 4f) / 6f);
        }

        var medianPosition = querySystem.MedianPosition;
        medianPosition.SetVec2(querySystem.AveragePosition);
        var num6 = medianPosition.GetNavMeshZ() - querySystem.ClosestEnemyFormation.MedianPosition.GetNavMeshZ();
        var num7 = 1f;
        if (num <= 4f)
        {
            var value = num6 / (querySystem.AveragePosition - querySystem.ClosestEnemyFormation.MedianPosition.AsVec2)
                .Length;
            num7 = MBMath.Lerp(0.9f, 1.1f, (MBMath.ClampFloat(value, -0.58f, 0.58f) + 0.58f) / 1.16f);
        }

        var num8 = 1f;
        if (num <= 4f && num >= 1.5f) num8 = 1.2f;
        var num9 = 1f;
        if (num <= 4f && querySystem.ClosestEnemyFormation.ClosestEnemyFormation != querySystem) num9 = 1.2f;
        var num10 = querySystem.GetClassWeightedFactor(1f, 1f, 1.5f, 1.5f) *
                    querySystem.ClosestEnemyFormation.GetClassWeightedFactor(1f, 1f, 0.5f, 0.5f);
        return num3 * num7 * num8 * num9 * num10 * 2f;
    }

    private enum ChargeState
    {
        Undetermined,
        Charging,
        ChargingPast,
        Reforming,
        Bracing
    }
}