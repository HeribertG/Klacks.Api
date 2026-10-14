// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IBulkWorkDeletionPlanner. Resolves the employee (id or name, group-scoped so a hidden person is unknown),
/// reads the main-plan container parents in the date range through the work repository, and refuses the whole plan
/// when nothing matches, more than BulkWorkDeletionDefaults.MaxWorksPerCall match, any matching Work is locked
/// (confirmed, approved or in a closed period) or any of its days is sealed for the employee. It never truncates a
/// match to fit the cap and never reads or returns details of a Work the caller may not see: the employee is resolved
/// through the visibility-scoped search or guard before any Work is loaded. A usable plan also names the live container
/// children (child Works and Breaks) of the planned Works, read through the cascade service the delete itself uses, so a
/// confirmation can be bound to everything the delete would remove.
/// </summary>
/// <param name="workRepository">Source of the employee's Works in the range</param>
/// <param name="clientRepository">Loads the resolved employee</param>
/// <param name="searchRepository">Group-scoped name search</param>
/// <param name="clientVisibilityGuard">Decides whether a client id the model supplied may be seen</param>
/// <param name="sealedDayRepository">Tells which of the employee's days are sealed</param>
/// <param name="shiftRepository">Source of the shift names listed in the plan</param>
/// <param name="companyClock">Anchor for relative day words in the dates</param>
/// <param name="cascadeService">Names the container children that the delete of the planned Works takes with it</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class BulkWorkDeletionPlanner : IBulkWorkDeletionPlanner
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string WorkAction = "deleted";
    private const int MaxListedProblems = 5;

    private const string MissingEmployeeMessage =
        "Say which employee's works to delete: give firstName and lastName (optionally idNumber) or clientId.";

    private const string MissingFromDateMessage =
        "Say from which day to delete: fromDate is required (untilDate is optional and defaults to fromDate).";

    private const string InvalidRangeMessage = "fromDate {0} is after untilDate {1}; nothing was changed.";

    private const string InvalidShiftIdMessage = "Invalid shiftId '{0}'. Expected a UUID, or omit it to take every shift.";

    private const string NothingMatchesMessage =
        "{0} has no work between {1} and {2}{3}; nothing was changed.";

    private const string TooManyMessage =
        "{0} matches {1} works between {2} and {3}, more than the limit of {4} per call; nothing was changed. " +
        "Ask the user to narrow the date range or the shift.";

    private const string LockedMessage =
        "{0} of the works between {1} and {2} are locked, so nothing was deleted. First locked: {3}";

    private const string SealedMessage =
        "These days are sealed for {0}, so nothing was deleted: {1}.";

    private readonly IWorkRepository _workRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ICompanyClock _companyClock;
    private readonly IContainerWorkCascadeService _cascadeService;

    public BulkWorkDeletionPlanner(
        IWorkRepository workRepository,
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISealedDayRepository sealedDayRepository,
        IShiftRepository shiftRepository,
        ICompanyClock companyClock,
        IContainerWorkCascadeService cascadeService)
    {
        _workRepository = workRepository;
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _sealedDayRepository = sealedDayRepository;
        _shiftRepository = shiftRepository;
        _companyClock = companyClock;
        _cascadeService = cascadeService;
    }

    public async Task<BulkWorkDeletionPlan> PlanAsync(
        Dictionary<string, object> parameters, string? userLanguage, CancellationToken cancellationToken = default)
    {
        var clientIdRaw = SkillParameterReader.Read<string>(parameters, "clientId");
        var firstName = SkillParameterReader.Read<string>(parameters, "firstName");
        var lastName = SkillParameterReader.Read<string>(parameters, "lastName");
        if (string.IsNullOrWhiteSpace(clientIdRaw)
            && string.IsNullOrWhiteSpace(firstName)
            && string.IsNullOrWhiteSpace(lastName))
        {
            return BulkWorkDeletionPlan.Refuse(MissingEmployeeMessage);
        }

        var (range, rangeError) = await ReadRangeAsync(parameters, userLanguage, cancellationToken);
        if (rangeError != null)
        {
            return BulkWorkDeletionPlan.Refuse(rangeError);
        }

        Guid? shiftId = null;
        var shiftIdRaw = SkillParameterReader.Read<string>(parameters, "shiftId");
        if (!string.IsNullOrWhiteSpace(shiftIdRaw))
        {
            if (!Guid.TryParse(shiftIdRaw, out var parsedShiftId))
            {
                return BulkWorkDeletionPlan.Refuse(string.Format(InvalidShiftIdMessage, shiftIdRaw));
            }

            shiftId = parsedShiftId;
        }

        var (client, clientError) = await ClientResolver.ResolveByIdOrNameAsync(
            _searchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw, firstName, lastName,
            SkillParameterReader.Read<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        if (client == null)
        {
            return BulkWorkDeletionPlan.Refuse(clientError!);
        }

        var employeeName = $"{client.FirstName} {client.Name}".Trim();
        var (from, until) = range;

        var works = MainPlanWorks.ParentsOnly(await _workRepository.GetByClientAndDateRangeAsync(
            client.Id, from.ToDateTime(TimeOnly.MinValue), until.ToDateTime(TimeOnly.MinValue), cancellationToken));
        if (shiftId.HasValue)
        {
            works = works.Where(work => work.ShiftId == shiftId.Value).ToList();
        }

        if (works.Count == 0)
        {
            return BulkWorkDeletionPlan.Refuse(string.Format(
                NothingMatchesMessage, employeeName, from.ToString(DateFormat), until.ToString(DateFormat),
                shiftId.HasValue ? " for that shift" : string.Empty));
        }

        if (works.Count > BulkWorkDeletionDefaults.MaxWorksPerCall)
        {
            return BulkWorkDeletionPlan.Refuse(string.Format(
                TooManyMessage, employeeName, works.Count, from.ToString(DateFormat), until.ToString(DateFormat),
                BulkWorkDeletionDefaults.MaxWorksPerCall));
        }

        var lockedRefusal = DescribeLockedWorks(works, from, until);
        if (lockedRefusal != null)
        {
            return BulkWorkDeletionPlan.Refuse(lockedRefusal);
        }

        var sealedRefusal = await DescribeSealedDaysAsync(client.Id, employeeName, works, cancellationToken);
        if (sealedRefusal != null)
        {
            return BulkWorkDeletionPlan.Refuse(sealedRefusal);
        }

        var children = await _cascadeService.GetChildIdsAsync(works.Select(work => work.Id).ToList(), cancellationToken);
        return new BulkWorkDeletionPlan(
            null, employeeName, from, until, await ToPlannedDeletionsAsync(works))
        {
            ChildWorkIds = children.WorkIds,
            ChildBreakIds = children.BreakIds
        };
    }

    private async Task<((DateOnly From, DateOnly Until) Range, string? Error)> ReadRangeAsync(
        Dictionary<string, object> parameters, string? userLanguage, CancellationToken cancellationToken)
    {
        var fromRaw = SkillParameterReader.Read<string>(parameters, "fromDate");
        if (string.IsNullOrWhiteSpace(fromRaw))
        {
            return (default, MissingFromDateMessage);
        }

        var untilRaw = SkillParameterReader.Read<string>(parameters, "untilDate");
        var today = await _companyClock.GetTodayAsync(cancellationToken);

        var (fromValue, invalidFrom) = SkillDateParser.ParseOptionalUtcDate(fromRaw, today, userLanguage);
        if (invalidFrom || !fromValue.HasValue)
        {
            return (default, SkillDateParser.InvalidDateMessageFor("fromDate", fromRaw));
        }

        var (untilValue, invalidUntil) = SkillDateParser.ParseOptionalUtcDate(untilRaw, today, userLanguage);
        if (invalidUntil)
        {
            return (default, SkillDateParser.InvalidDateMessageFor("untilDate", untilRaw!));
        }

        var from = DateOnly.FromDateTime(fromValue.Value);
        var until = untilValue.HasValue ? DateOnly.FromDateTime(untilValue.Value) : from;
        if (from > until)
        {
            return (default, string.Format(InvalidRangeMessage, from.ToString(DateFormat), until.ToString(DateFormat)));
        }

        return ((from, until), null);
    }

    private static string? DescribeLockedWorks(List<Work> works, DateOnly from, DateOnly until)
    {
        var locked = works.Where(work => work.LockLevel != WorkLockLevel.None).ToList();
        if (locked.Count == 0)
        {
            return null;
        }

        return string.Format(
            LockedMessage, locked.Count, from.ToString(DateFormat), until.ToString(DateFormat),
            WorkLockRefusal.Message(locked[0].Id, locked[0].LockLevel, WorkAction));
    }

    private async Task<string?> DescribeSealedDaysAsync(
        Guid clientId, string employeeName, List<Work> works, CancellationToken cancellationToken)
    {
        var pairs = works.Select(work => (work.CurrentDate, clientId)).Distinct().ToList();
        var sealedPairs = await _sealedDayRepository.GetLockedPairsAsync(pairs, cancellationToken);
        if (sealedPairs.Count == 0)
        {
            return null;
        }

        var days = sealedPairs
            .Select(pair => pair.Date)
            .Distinct()
            .OrderBy(date => date)
            .Take(MaxListedProblems)
            .Select(date => date.ToString(DateFormat));
        return string.Format(SealedMessage, employeeName, string.Join(", ", days));
    }

    private async Task<IReadOnlyList<PlannedWorkDeletion>> ToPlannedDeletionsAsync(List<Work> works)
    {
        var shiftNames = new Dictionary<Guid, string>();
        foreach (var shiftId in works.Select(work => work.ShiftId).Distinct())
        {
            var shift = await _shiftRepository.Get(shiftId);
            shiftNames[shiftId] = string.IsNullOrWhiteSpace(shift?.Name) ? shiftId.ToString() : shift.Name;
        }

        return works
            .Select(work => new PlannedWorkDeletion(
                work.Id, work.CurrentDate, work.ShiftId, shiftNames[work.ShiftId], work.StartTime, work.EndTime))
            .ToList();
    }
}
