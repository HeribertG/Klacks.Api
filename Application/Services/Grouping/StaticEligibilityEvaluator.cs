// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a client can in principle take a shift in the analysed period: there must be one run
/// day inside the client's company membership (entry and exit inclusive; a client without a membership window is
/// unrestricted) with contract data (an active contract or the settings defaults the contract provider returns for a
/// day without one) whose weekday flags allow the weekday, the shift-work rule of the wizard holds,
/// no mandatory qualification gap of severity Error exists (MandatoryQualificationLevelTooLow when every such gap is
/// only a held level below a binding minimum, otherwise MandatoryQualificationMissing), the shift is not blacklisted and the client
/// is not marked unavailable for the shift's hours. A run day outside the membership is OutsideMembership (checked
/// first), a member day without any contract data is NoActiveContract. The scan stops at the first eligible day.
/// Per run day the first violated condition is the day's reason; for an ineligible pair the reason that blocks on
/// the most run days is kept, a tie going to the lower enum value (OutsideMembership, appended last, loses ties).
/// Contract data is indexed per client and day offset once. Per-client weekday masks (a union over all member
/// days with contract data, so contract changes and entry or exit inside the period are respected) reject pairs
/// that cannot have an eligible run day before any per-day work. Masks only conclude "ineligible"; the reason of such a pair
/// still comes from the per-day count. Verdicts and weekday sets are cached per pair for one analysis.
/// HasActiveContractInPeriod tells whether a client has at least one member day with an active contract in the
/// period; it is informational and does not influence eligibility.
/// </summary>
/// <param name="context">Pre-indexed inputs of one analysis.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class StaticEligibilityEvaluator : IGroupingEligibilityOracle
{
    private static readonly IReadOnlyList<ShiftRequiredQualification> NoRequirements = [];
    private static readonly IReadOnlyList<ClientQualification> NoQualifications = [];
    private static readonly IReadOnlyList<ClientAvailability> NoAvailability = [];
    private static readonly IReadOnlyList<DateOnly> NoRunDays = [];

    private readonly GroupingEligibilityContext _context;
    private readonly DateOnly _firstContractDay;
    private readonly int _contractDaySpan;
    private readonly Dictionary<Guid, EffectiveContractData?[]> _contractsByClient = new();
    private readonly HashSet<Guid> _clientsWithActiveContract = [];
    private readonly Dictionary<Guid, int> _workableWeekdays = new();
    private readonly Dictionary<Guid, int> _shiftWorkWeekdays = new();
    private readonly Dictionary<Guid, int> _runWeekdays = new();
    private readonly HashSet<Guid> _nonEarlyShifts = [];
    private readonly Dictionary<GroupingEntityPair, EligibilityVerdict> _verdicts = new();
    private readonly Dictionary<GroupingEntityPair, IReadOnlySet<DayOfWeek>> _weekdays = new();

    public StaticEligibilityEvaluator(GroupingEligibilityContext context)
    {
        _context = context;
        if (context.Contracts.Count > 0)
        {
            _firstContractDay = context.Contracts.Keys.Min();
            _contractDaySpan = context.Contracts.Keys.Max().DayNumber - _firstContractDay.DayNumber + 1;
        }

        IndexContracts();
        IndexShifts();
    }

    public EligibilityVerdict Evaluate(Guid clientId, Guid shiftId)
    {
        var key = new GroupingEntityPair(clientId, shiftId);
        if (_verdicts.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var verdict = Compute(clientId, shiftId);
        _verdicts[key] = verdict;
        return verdict;
    }

    public bool IsEligible(Guid clientId, Guid shiftId)
    {
        if (!_contractsByClient.ContainsKey(clientId))
        {
            return false;
        }

        if (_verdicts.TryGetValue(new GroupingEntityPair(clientId, shiftId), out var cached))
        {
            return cached.IsEligible;
        }

        return CandidateWeekdays(clientId, shiftId) != 0 && Evaluate(clientId, shiftId).IsEligible;
    }

    public IReadOnlySet<DayOfWeek> EligibleWeekdays(Guid clientId, Guid shiftId)
    {
        var key = new GroupingEntityPair(clientId, shiftId);
        if (_weekdays.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var result = new HashSet<DayOfWeek>();
        var candidates = CandidateWeekdays(clientId, shiftId);
        foreach (var day in candidates == 0 ? NoRunDays : RunDaysOf(shiftId))
        {
            if ((candidates & Bit(day.DayOfWeek)) == 0 || result.Contains(day.DayOfWeek))
            {
                continue;
            }

            if (FirstFailure(clientId, shiftId, day) is null)
            {
                result.Add(day.DayOfWeek);
                if (result.Count == GroupingFeasibilityDefaults.DaysPerWeek)
                {
                    break;
                }
            }
        }

        _weekdays[key] = result;
        return result;
    }

    public bool HasActiveContractInPeriod(Guid clientId) => _clientsWithActiveContract.Contains(clientId);

    private EligibilityVerdict Compute(Guid clientId, Guid shiftId)
    {
        var blockedDays = new Dictionary<GroupingIneligibilityReason, int>();
        foreach (var day in RunDaysOf(shiftId))
        {
            var failure = FirstFailure(clientId, shiftId, day);
            if (failure is not GroupingIneligibilityReason reason)
            {
                return EligibilityVerdict.Eligible;
            }

            blockedDays[reason] = blockedDays.GetValueOrDefault(reason) + 1;
        }

        return EligibilityVerdict.Ineligible(DominantReason(blockedDays));
    }

    private static GroupingIneligibilityReason DominantReason(Dictionary<GroupingIneligibilityReason, int> blockedDays)
    {
        if (blockedDays.Count == 0)
        {
            return GroupingIneligibilityReason.NoActiveContract;
        }

        return blockedDays
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key)
            .First()
            .Key;
    }

    private GroupingIneligibilityReason? FirstFailure(Guid clientId, Guid shiftId, DateOnly day)
    {
        if (!IsMember(clientId, day))
        {
            return GroupingIneligibilityReason.OutsideMembership;
        }

        var contract = ContractOn(clientId, day);
        if (contract is null)
        {
            return GroupingIneligibilityReason.NoActiveContract;
        }

        if (!WorksOn(contract, day.DayOfWeek))
        {
            return GroupingIneligibilityReason.WeekdayNotAllowed;
        }

        if (!contract.PerformsShiftWork && _nonEarlyShifts.Contains(shiftId))
        {
            return GroupingIneligibilityReason.NotShiftWorker;
        }

        var requirements = _context.RequirementsByShift.GetValueOrDefault(shiftId, NoRequirements);
        if (requirements.Count > 0)
        {
            var held = _context.QualificationsByClient.GetValueOrDefault(clientId, NoQualifications);
            var blocking = EligibilityMatcher
                .FindMandatoryGaps(requirements, held, day, _context.ExpiredMandatoryBlocks)
                .Where(gap => gap.Severity == QualificationGapSeverity.Error)
                .ToList();
            if (blocking.Count > 0)
            {
                return blocking.All(gap => gap.IsBindingLevelBlock)
                    ? GroupingIneligibilityReason.MandatoryQualificationLevelTooLow
                    : GroupingIneligibilityReason.MandatoryQualificationMissing;
            }
        }

        if (_context.Blacklist.Contains(new GroupingEntityPair(clientId, shiftId)))
        {
            return GroupingIneligibilityReason.Blacklisted;
        }

        var shift = _context.Shifts[shiftId];
        var availability = _context.AvailabilityByClientAndDay.GetValueOrDefault((clientId, day), NoAvailability);
        if (AvailabilityMatcher.IsUnavailable(availability, day, shift.Start, shift.End))
        {
            return GroupingIneligibilityReason.Unavailable;
        }

        return null;
    }

    private void IndexContracts()
    {
        foreach (var (day, perClient) in _context.Contracts)
        {
            var offset = day.DayNumber - _firstContractDay.DayNumber;
            foreach (var (clientId, contract) in perClient)
            {
                if (!_contractsByClient.TryGetValue(clientId, out var days))
                {
                    days = new EffectiveContractData?[_contractDaySpan];
                    _contractsByClient[clientId] = days;
                }

                days[offset] = contract;
                if (!IsMember(clientId, day))
                {
                    continue;
                }

                if (contract.HasActiveContract)
                {
                    _clientsWithActiveContract.Add(clientId);
                }

                if (WorksOn(contract, day.DayOfWeek))
                {
                    _workableWeekdays[clientId] = _workableWeekdays.GetValueOrDefault(clientId) | Bit(day.DayOfWeek);
                    if (contract.PerformsShiftWork)
                    {
                        _shiftWorkWeekdays[clientId] = _shiftWorkWeekdays.GetValueOrDefault(clientId) | Bit(day.DayOfWeek);
                    }
                }
            }
        }
    }

    private void IndexShifts()
    {
        foreach (var (shiftId, days) in _context.RunDays)
        {
            _runWeekdays[shiftId] = days.Aggregate(0, (mask, day) => mask | Bit(day.DayOfWeek));
        }

        foreach (var (shiftId, shift) in _context.Shifts)
        {
            if (ShiftWorkRule.RequiresShiftWork(ShiftTypeInference.FromSpan(shift.Start, shift.End)))
            {
                _nonEarlyShifts.Add(shiftId);
            }
        }
    }

    private int CandidateWeekdays(Guid clientId, Guid shiftId)
    {
        var clientMask = _nonEarlyShifts.Contains(shiftId)
            ? _shiftWorkWeekdays.GetValueOrDefault(clientId)
            : _workableWeekdays.GetValueOrDefault(clientId);
        return clientMask & _runWeekdays.GetValueOrDefault(shiftId);
    }

    private EffectiveContractData? ContractOn(Guid clientId, DateOnly day)
    {
        if (!_contractsByClient.TryGetValue(clientId, out var days))
        {
            return null;
        }

        var offset = day.DayNumber - _firstContractDay.DayNumber;
        return offset >= 0 && offset < days.Length ? days[offset] : null;
    }

    private bool IsMember(Guid clientId, DateOnly day) =>
        MembershipWindows.IsMemberOn(_context.MembershipWindows, clientId, day);

    private static int Bit(DayOfWeek day) => 1 << (int)day;

    private IReadOnlyList<DateOnly> RunDaysOf(Guid shiftId) =>
        _context.RunDays.GetValueOrDefault(shiftId, NoRunDays);

    private static bool WorksOn(EffectiveContractData contract, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => contract.WorkOnMonday,
        DayOfWeek.Tuesday => contract.WorkOnTuesday,
        DayOfWeek.Wednesday => contract.WorkOnWednesday,
        DayOfWeek.Thursday => contract.WorkOnThursday,
        DayOfWeek.Friday => contract.WorkOnFriday,
        DayOfWeek.Saturday => contract.WorkOnSaturday,
        DayOfWeek.Sunday => contract.WorkOnSunday,
        _ => false,
    };
}
