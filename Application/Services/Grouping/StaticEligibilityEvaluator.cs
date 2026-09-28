// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a client can in principle take a shift in the analysed period: there must be one run
/// day on which the contract is active and allows the weekday, the shift-work rule of the wizard holds,
/// no mandatory qualification gap of severity Error exists, the shift is not blacklisted and the client
/// is not marked unavailable for the shift's hours. The scan stops at the first eligible day. Per run day
/// the first violated condition is the day's reason; for an ineligible pair the reason that blocks on the
/// most run days is kept, a tie going to the lower enum value (the condition order).
/// Contracts are indexed per client and day offset once. A client without any active contract day in the
/// period is NoActiveContract for every shift without a per-day scan, and per-client weekday masks (a union
/// over all days, so contract changes inside the period are respected) reject pairs that cannot have an
/// eligible run day before any per-day work. Masks only conclude "ineligible"; the reason of such a pair
/// still comes from the per-day count. Verdicts and weekday sets are cached per pair for one analysis.
/// HasActiveContractInPeriod tells whether a client has at least one active contract day in the period.
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

    private static readonly EligibilityVerdict NoContractVerdict =
        EligibilityVerdict.Ineligible(GroupingIneligibilityReason.NoActiveContract);

    private readonly GroupingEligibilityContext _context;
    private readonly DateOnly _firstContractDay;
    private readonly int _contractDaySpan;
    private readonly Dictionary<Guid, EffectiveContractData?[]> _contractsByClient = new();
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
        if (!_contractsByClient.ContainsKey(clientId))
        {
            return NoContractVerdict;
        }

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

    public bool HasActiveContractInPeriod(Guid clientId) => _contractsByClient.ContainsKey(clientId);

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
        var contract = ContractOn(clientId, day);
        if (contract is null || !contract.HasActiveContract)
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
                .Any(gap => gap.Severity == QualificationGapSeverity.Error);
            if (blocking)
            {
                return GroupingIneligibilityReason.MandatoryQualificationMissing;
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
        var clientsWithActiveDay = new HashSet<Guid>();
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
                if (!contract.HasActiveContract)
                {
                    continue;
                }

                clientsWithActiveDay.Add(clientId);
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

        foreach (var clientId in _contractsByClient.Keys.Where(clientId => !clientsWithActiveDay.Contains(clientId)).ToList())
        {
            _contractsByClient.Remove(clientId);
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
            if (ShiftTypeInference.FromSpan(shift.Start, shift.End) != ShiftTypeInference.EarlyIndex)
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
