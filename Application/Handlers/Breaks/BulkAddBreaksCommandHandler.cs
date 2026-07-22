// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class BulkAddBreaksCommandHandler : BaseHandler, IRequestHandler<BulkAddBreaksCommand, BulkBreaksResponse>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IBreakMacroService _breakMacroService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IDayLockService _dayLockService;

    public BulkAddBreaksCommandHandler(
        IBreakRepository breakRepository,
        IBreakMacroService breakMacroService,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IDayLockService dayLockService,
        ILogger<BulkAddBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _breakMacroService = breakMacroService;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _dayLockService = dayLockService;
    }

    public async Task<BulkBreaksResponse> Handle(BulkAddBreaksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureGuardsPassAsync(command, cancellationToken);

            var response = new BulkBreaksResponse();
            var breaks = new List<Break>();
            var affectedClients = new HashSet<Guid>();

            foreach (var item in command.Request.Breaks)
            {
                try
                {
                    var breakEntry = new Break
                    {
                        Id = Guid.NewGuid(),
                        ClientId = item.ClientId,
                        AbsenceId = item.AbsenceId,
                        CurrentDate = item.CurrentDate,
                        WorkTime = item.WorkTime,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime,
                        Information = item.Information,
                        Description = item.Description,
                        AnalyseToken = item.AnalyseToken
                    };

                    breaks.Add(breakEntry);
                    response.CreatedIds.Add(breakEntry.Id);
                    response.SuccessCount++;
                    affectedClients.Add(item.ClientId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create break for client {ClientId} on {Date}",
                        item.ClientId, item.CurrentDate);
                    response.FailedCount++;
                }
            }

            if (breaks.Count > 0)
            {
                var paymentInterval = command.Request.PaymentInterval;

                foreach (var b in breaks)
                {
                    await _breakMacroService.ProcessBreakMacroAsync(b, paymentInterval);
                    await _breakRepository.Add(b);
                }

                var affected = breaks.Select(b => (b.ClientId, b.CurrentDate, b.AnalyseToken)).ToList();
                await _completionService.SaveBulkAndTrackAsync(affected);

                var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();
                foreach (var b in breaks)
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

                var tokenByClient = breaks
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
        }, "BulkAddBreaks", new { Count = command.Request.Breaks.Count });
    }

    private async Task EnsureGuardsPassAsync(BulkAddBreaksCommand command, CancellationToken cancellationToken)
    {
        foreach (var (clientId, date, analyseToken) in command.Request.Breaks
            .Select(b => (b.ClientId, b.CurrentDate, b.AnalyseToken))
            .Distinct())
        {
            await _dayLockService.EnsureNotLockedAsync(date, clientId, analyseToken, cancellationToken);
        }

        // Structural guard for breaks is duplicate-absence, not work-collision: a break may legitimately
        // sit over a work (BreakDominatesCollidingWork) or over an absence of another type (sick during
        // vacation). It is refused only when an active break of the SAME absence type already overlaps the
        // same client and day within the SAME analyse regime (IS NOT DISTINCT FROM: real vs real, scenario
        // vs the same scenario) — so, unlike the work checker, this guard also applies to scenario rows.
        foreach (var item in command.Request.Breaks)
        {
            var alreadyExists = await _breakRepository.HasOverlappingAbsenceOfSameTypeAsync(
                item.ClientId,
                item.CurrentDate,
                item.AbsenceId,
                item.StartTime,
                item.EndTime,
                item.AnalyseToken,
                cancellationToken);

            if (alreadyExists)
            {
                throw new ConflictException(
                    $"Duplicate absence blocked: client {item.ClientId} already has an overlapping absence of " +
                    $"type {item.AbsenceId} on {item.CurrentDate:yyyy-MM-dd} in the same analyse regime. Not committed.");
            }
        }
    }
}
