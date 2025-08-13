using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : IRequestHandler<PutCommand, Klacks.Api.Domain.Models.Settings.CalendarRule?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly SettingsApplicationService _settingsApplicationService;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              SettingsApplicationService settingsApplicationService,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        _settingsApplicationService = settingsApplicationService;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _settingsApplicationService.UpdateCalendarRuleAsync(request.model, cancellationToken);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("CalendarRule with ID {CalendarRuleId} updated successfully.", request.model.Id);

            return request.model;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating CalendarRule with ID {CalendarRuleId}.", request.model.Id);
            throw;
        }
    }
}
