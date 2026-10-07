// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes several breaks (absence entries) in one call and recalculates the affected period hours. Breaks
/// owned by clients outside the caller's group visibility are treated exactly like ids that do not exist:
/// they are not deleted and count as failed. Like the single delete, the whole request is refused when any
/// visible break lies on a sealed day (IDayLockService), whatever the caller's role.
/// </summary>
/// <param name="clientVisibilityGuard">Filters the breaks down to clients the calling user may write for</param>
/// <param name="dayLockService">Refuses deletions on days sealed globally or for a group of the client</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class BulkDeleteBreaksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteBreaksCommand, BulkBreaksResponse>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IDayLockService _dayLockService;

    public BulkDeleteBreaksCommandHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IDayLockService dayLockService,
        ILogger<BulkDeleteBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _dayLockService = dayLockService;
    }

    public async Task<BulkBreaksResponse> Handle(BulkDeleteBreaksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (command.Request.BreakIds.Count > RateLimitingPolicies.MaxBulkOperationItems)
                throw new ArgumentException($"Maximum {RateLimitingPolicies.MaxBulkOperationItems} items allowed per bulk operation.");

            var response = new BulkBreaksResponse();
            var affectedClients = new HashSet<Guid>();

            var foundBreaks = await _breakRepository.GetByIdsAsync(command.Request.BreakIds);
            var deletedBreaks = await _clientVisibilityGuard.FilterVisibleAsync(
                foundBreaks, b => b.ClientId, cancellationToken);
            await _dayLockService.EnsureNoneLockedAsync(
                deletedBreaks.Select(b => (b.CurrentDate, b.ClientId, b.AnalyseToken)).ToList(),
                cancellationToken);

            foreach (var breakEntry in deletedBreaks)
            {
                _breakRepository.Remove(breakEntry);
            }

            var affected = deletedBreaks.Select(b => (b.ClientId, b.CurrentDate, b.AnalyseToken)).ToList();
            await _completionService.SaveBulkAndTrackAsync(affected);

            foreach (var breakEntry in deletedBreaks)
            {
                affectedClients.Add(breakEntry.ClientId);
                response.DeletedIds.Add(breakEntry.Id);
                response.SuccessCount++;
            }

            response.FailedCount = command.Request.BreakIds.Count - deletedBreaks.Count;

            if (deletedBreaks.Count > 0)
            {
                var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();
                foreach (var b in deletedBreaks)
                {
                    var (start, end) = await _periodHoursService.GetPeriodBoundariesAsync(b.CurrentDate);
                    if (!clientPeriods.TryGetValue(b.ClientId, out var existing))
                    {
                        clientPeriods[b.ClientId] = (start, end);
                    }
                    else
                    {
                        clientPeriods[b.ClientId] = (
                            start < existing.Start ? start : existing.Start,
                            end > existing.End ? end : existing.End);
                    }
                }

                response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                var tokenByClient = deletedBreaks
                    .GroupBy(b => b.ClientId)
                    .ToDictionary(g => g.Key, g => g.First().AnalyseToken);

                foreach (var clientId in affectedClients)
                {
                    var analyseToken = tokenByClient.TryGetValue(clientId, out var t) ? t : null;
                    var (clientStart, clientEnd) = clientPeriods[clientId];
                    var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                        clientId,
                        clientStart,
                        clientEnd,
                        analyseToken);
                    response.PeriodHours[clientId] = periodHours;
                }
            }

            return response;
        }, "BulkDeleteBreaks", new { Count = command.Request.BreakIds.Count });
    }
}
