using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class BulkAddBreaksCommandHandler : BaseHandler, IRequestHandler<BulkAddBreaksCommand, BulkBreaksResponse>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IPeriodHoursService _periodHoursService;

    public BulkAddBreaksCommandHandler(
        IBreakRepository breakRepository,
        IPeriodHoursService periodHoursService,
        ILogger<BulkAddBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _periodHoursService = periodHoursService;
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
                        Description = item.Description
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
                await _breakRepository.BulkAddWithTracking(breaks);

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
