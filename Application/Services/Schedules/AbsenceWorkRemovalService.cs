// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule which works an entered absence takes away (the service does not happen, it is not billed, the open place
/// shows the gap). On-call absences take none. A work stays when it is sealed, carries a replacement or a sealed child, or
/// lies on a shift outside the caller's groups; without a selected group nothing tells a foreign shift apart, so nothing
/// is removed then. Deleting runs through the Work delete, so its guards, cascades and period hours apply.
/// </summary>
/// <param name="scheduleEntries">Reads the works of the absent people with their group restriction</param>
/// <param name="factsReader">Tells which works are untouched</param>
/// <param name="absenceRepository">Knows the on-call absence types</param>
/// <param name="groupContextResolver">Resolves the caller's selected groups</param>
/// <param name="periodHoursService">Resolves the period the Work delete recalculates</param>
/// <param name="unitOfWork">Holds the absence and the delete in one transaction</param>
/// <param name="mediator">Runs the Work delete</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class AbsenceWorkRemovalService : IAbsenceWorkRemovalService
{
    private readonly IScheduleEntriesService _scheduleEntries;
    private readonly ICoverMoveFactsReader _factsReader;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public AbsenceWorkRemovalService(
        IScheduleEntriesService scheduleEntries,
        ICoverMoveFactsReader factsReader,
        IAbsenceRepository absenceRepository,
        ISelectedGroupContextResolver groupContextResolver,
        IPeriodHoursService periodHoursService,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _scheduleEntries = scheduleEntries;
        _factsReader = factsReader;
        _absenceRepository = absenceRepository;
        _groupContextResolver = groupContextResolver;
        _periodHoursService = periodHoursService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<RemovableWork>> FindAsync(
        IReadOnlyList<AbsenceSpan> absences, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var onCall = await _absenceRepository.GetOnCallAbsenceIdsAsync(cancellationToken);
        var taking = absences.Where(absence => !onCall.Contains(absence.AbsenceId)).ToList();
        if (taking.Count == 0)
        {
            return [];
        }

        var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
        if (visibleGroupIds is null || visibleGroupIds.Count == 0)
        {
            return [];
        }

        var cells = await _scheduleEntries.GetScheduleEntriesAsync(
            new ScheduleEntriesCriteria(taking.Min(a => a.Date), taking.Max(a => a.Date), visibleGroupIds, analyseToken)
            {
                ClientIds = taking.Select(a => a.ClientId).Distinct().ToList(),
                EntryType = ScheduleEntryType.Work,
            },
            cancellationToken);

        var candidates = cells
            .Where(cell => !cell.IsGroupRestricted && taking.Any(absence => Takes(absence, cell)))
            .Select(ToRemovable)
            .ToList();
        if (candidates.Count == 0)
        {
            return [];
        }

        var facts = await _factsReader.LoadAsync(candidates.Select(work => work.WorkId).ToList(), analyseToken, cancellationToken);
        return candidates
            .Where(work => facts.TryGetValue(work.WorkId, out var fact) && CoverMoveDecision.IsUntouched(fact))
            .ToList();
    }

    public async Task WriteAndRemoveAsync(
        IReadOnlyList<RemovableWork> taken, Func<Task> writeAbsences, CancellationToken cancellationToken)
    {
        if (taken.Count == 0)
        {
            await writeAbsences();
            return;
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await writeAbsences();
                await DeleteAsync(taken, cancellationToken);
                return taken.Count;
            });
        }
        catch
        {
            _unitOfWork.DiscardTrackedChanges();
            throw;
        }
    }

    private async Task DeleteAsync(IReadOnlyList<RemovableWork> taken, CancellationToken cancellationToken)
    {
        foreach (var work in taken)
        {
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.Date);
            await _mediator.Send(new DeleteWorkCommand(work.WorkId, periodStart, periodEnd), cancellationToken);
        }
    }

    private static bool Takes(AbsenceSpan absence, ScheduleCell cell)
        => absence.ClientId == cell.ClientId
            && absence.Date == DateOnly.FromDateTime(cell.EntryDate)
            && AbsenceWorkCoverage.CoversWork(
                absence.StartTime, absence.EndTime, TimeOnly.FromTimeSpan(cell.StartTime), TimeOnly.FromTimeSpan(cell.EndTime));

    private static RemovableWork ToRemovable(ScheduleCell cell)
        => new(
            cell.SourceId, cell.ClientId, cell.EntryId, DateOnly.FromDateTime(cell.EntryDate),
            TimeOnly.FromTimeSpan(cell.StartTime), TimeOnly.FromTimeSpan(cell.EndTime));
}
