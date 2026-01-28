using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class BulkAddBreaksCommandHandler : BaseHandler, IRequestHandler<BulkAddBreaksCommand, BulkBreaksResponse>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IBreakMacroService _breakMacroService;

    public BulkAddBreaksCommandHandler(
        IBreakRepository breakRepository,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IBreakMacroService breakMacroService,
        ILogger<BulkAddBreaksCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _breakMacroService = breakMacroService;
    }

    public async Task<BulkBreaksResponse> Handle(BulkAddBreaksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkBreaksResponse();
        var createdBreaks = new List<Break>();
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
                    IsSealed = false,
                    IsDeleted = false
                };

                await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
                await _breakRepository.Add(breakEntry);
                createdBreaks.Add(breakEntry);
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

        if (createdBreaks.Count > 0)
        {
            await _unitOfWork.CompleteAsync();

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
