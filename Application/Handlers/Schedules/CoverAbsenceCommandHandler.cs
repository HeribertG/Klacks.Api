// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="CoverAbsenceCommand"/>. Orchestrates the disruption flow in one call: create
/// a scenario and clone the real schedule under its token, flush, record the absence (Break) for the
/// employee in the scenario, then for each of the employee's (cloned) work slots that day propose a
/// rule-compliant replacement (via <see cref="FindReplacementQuery"/>) as a Replacement WorkChange on
/// that cloned work (which inherits the scenario token through the WorkChange handler). Locked slots are
/// reported for manual review; slots without an eligible candidate as under-coverage.
/// </summary>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones the real schedule under the scenario token</param>
/// <param name="scheduleEntriesService">Reads the absent employee's slots in the scenario</param>
/// <param name="mediator">Dispatches FindReplacementQuery + the Break and Replacement-WorkChange commands</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the slots are read</param>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class CoverAbsenceCommandHandler : IRequestHandler<CoverAbsenceCommand, CoverAbsenceOutcome>
{
    private const string ScenarioNamePrefix = "Absence cover";
    private const string LockedReason = "locked";
    private const string NoCandidateReason = "no eligible candidate";
    private const int HoursPerDay = 24;
    private const decimal DefaultAbsenceHours = 8m;
    private static readonly TimeOnly DayStart = new(0, 0);
    private static readonly TimeOnly DayEnd = new(23, 59);

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CoverAbsenceCommandHandler(
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IScheduleEntriesService scheduleEntriesService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _scheduleEntriesService = scheduleEntriesService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CoverAbsenceOutcome> Handle(CoverAbsenceCommand request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        var date = request.Date;
        var groupId = request.GroupId;
        var absenceId = request.AbsenceId;

        var token = Guid.NewGuid();
        var name = await GenerateUniqueNameAsync(date, groupId, cancellationToken);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = date,
            UntilDate = date,
            Token = token,
            RunGroupId = Guid.NewGuid()
        };
        await _scenarioRepository.Add(scenario);

        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(
            groupId, date, date, token, additionalShiftIds: null, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var cloneToOriginalShift = shiftIdMap.ToDictionary(kv => kv.Value, kv => kv.Key);

        var slots = await _scheduleEntriesService
            .GetScheduleEntriesQuery(date, date, new List<Guid> { groupId }, token)
            .Where(c => c.EntryType == (int)ScheduleEntryType.Work && c.ClientId == clientId)
            .ToListAsync(cancellationToken);

        var absenceHours = slots.Count > 0
            ? slots.Sum(s => WorkHours(TimeOnly.FromTimeSpan(s.StartTime), TimeOnly.FromTimeSpan(s.EndTime)))
            : DefaultAbsenceHours;
        await RecordAbsenceAsync(clientId, date, absenceId, absenceHours, token, cancellationToken);

        var covered = new List<CoveredSlot>();
        var uncovered = new List<UncoveredSlot>();

        foreach (var slot in slots)
        {
            var originalShiftId = cloneToOriginalShift.TryGetValue(slot.EntryId, out var mapped)
                ? mapped
                : slot.EntryId;

            if (slot.LockLevel != (int)WorkLockLevel.None)
            {
                uncovered.Add(new UncoveredSlot(originalShiftId, date, LockedReason));
                continue;
            }

            var start = TimeOnly.FromTimeSpan(slot.StartTime);
            var end = TimeOnly.FromTimeSpan(slot.EndTime);

            var search = await _mediator.Send(
                new FindReplacementQuery(originalShiftId, date, start, end, groupId, token), cancellationToken);
            var best = search.Eligible.FirstOrDefault();
            if (best == null)
            {
                uncovered.Add(new UncoveredSlot(originalShiftId, date, NoCandidateReason));
                continue;
            }

            await _mediator.Send(new PostCommand<WorkChangeResource>(new WorkChangeResource
            {
                WorkId = slot.SourceId,
                Type = WorkChangeType.ReplacementWithin,
                StartTime = start,
                EndTime = end,
                ChangeTime = WorkHours(start, end),
                ReplaceClientId = best.ClientId
            }), cancellationToken);

            covered.Add(new CoveredSlot(originalShiftId, date, best.ClientId, best.Name));
        }

        return new CoverAbsenceOutcome(scenario.Id, token, name, covered, uncovered);
    }

    private async Task RecordAbsenceAsync(
        Guid clientId,
        DateOnly date,
        Guid absenceId,
        decimal hours,
        Guid token,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new BulkAddBreaksCommand(new BulkAddBreaksRequest
        {
            PeriodStart = date,
            PeriodEnd = date,
            Breaks =
            [
                new BulkBreakItem
                {
                    ClientId = clientId,
                    AbsenceId = absenceId,
                    CurrentDate = date,
                    StartTime = DayStart,
                    EndTime = DayEnd,
                    WorkTime = hours,
                    AnalyseToken = token
                }
            ]
        }), cancellationToken);
    }

    private async Task<string> GenerateUniqueNameAsync(DateOnly date, Guid? groupId, CancellationToken cancellationToken)
    {
        var baseName = $"{ScenarioNamePrefix} {date:dd.MM.yy}";
        var existing = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        var existingNames = existing.Select(s => s.Name).ToHashSet();

        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var counter = 2;
        while (existingNames.Contains($"{baseName} ({counter})"))
        {
            counter++;
        }
        return $"{baseName} ({counter})";
    }

    private static decimal WorkHours(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(HoursPerDay));
        }
        return (decimal)duration.TotalHours;
    }
}
