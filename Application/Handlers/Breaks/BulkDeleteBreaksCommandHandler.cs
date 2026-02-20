using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class BulkDeleteBreaksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteBreaksCommand, BulkBreaksResponse>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IPeriodHoursService _periodHoursService;

    public BulkDeleteBreaksCommandHandler(
        IBreakRepository breakRepository,
        IPeriodHoursService periodHoursService,
        ILogger<BulkDeleteBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _periodHoursService = periodHoursService;
    }

    public async Task<BulkBreaksResponse> Handle(BulkDeleteBreaksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkBreaksResponse();
        var affectedClients = new HashSet<Guid>();

        var deletedBreaks = await _breakRepository.BulkDeleteWithTracking(command.Request.BreakIds);

        foreach (var breakEntry in deletedBreaks)
        {
            affectedClients.Add(breakEntry.ClientId);
            response.DeletedIds.Add(breakEntry.Id);
            response.SuccessCount++;
        }

        response.FailedCount = command.Request.BreakIds.Count - deletedBreaks.Count;

        if (deletedBreaks.Count > 0)
        {
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
    }
}
