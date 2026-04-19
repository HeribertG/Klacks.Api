// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
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

    public BulkAddBreaksCommandHandler(
        IBreakRepository breakRepository,
        IBreakMacroService breakMacroService,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        ILogger<BulkAddBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _breakMacroService = breakMacroService;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
    }

    public async Task<BulkBreaksResponse> Handle(BulkAddBreaksCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
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

                var periodStart = command.Request.PeriodStart;
                var periodEnd = command.Request.PeriodEnd;

                response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                foreach (var clientId in affectedClients)
                {
                    var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                        clientId,
                        periodStart,
                        periodEnd);
                    response.PeriodHours[clientId] = periodHours;
                }
            }

            return response;
        }, "BulkAddBreaks", new { Count = command.Request.Breaks.Count });
    }
}
