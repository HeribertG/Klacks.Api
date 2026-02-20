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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public BulkDeleteBreaksCommandHandler(
        IBreakRepository breakRepository,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<BulkDeleteBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleChangeTracker = scheduleChangeTracker;
    }

    public async Task<BulkBreaksResponse> Handle(BulkDeleteBreaksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkBreaksResponse();
        var deletedBreaks = new List<Break>();
        var affectedClients = new HashSet<Guid>();

        foreach (var breakId in command.Request.BreakIds)
        {
            try
            {
                var breakEntry = await _breakRepository.Get(breakId);
                if (breakEntry == null)
                {
                    _logger.LogWarning("Break with ID {BreakId} not found for deletion", breakId);
                    response.FailedCount++;
                    continue;
                }

                affectedClients.Add(breakEntry.ClientId);
                deletedBreaks.Add(breakEntry);

                await _breakRepository.Delete(breakId);
                response.DeletedIds.Add(breakId);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete break {BreakId}", breakId);
                response.FailedCount++;
            }
        }

        if (deletedBreaks.Count > 0)
        {
            await _unitOfWork.CompleteAsync();

            foreach (var breakEntry in deletedBreaks)
            {
                await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
            }

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
