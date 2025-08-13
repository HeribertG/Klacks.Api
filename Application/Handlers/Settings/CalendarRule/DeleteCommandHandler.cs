using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.CalendarRule>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly SettingsApplicationService _settingsApplicationService;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                SettingsApplicationService settingsApplicationService,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        _settingsApplicationService = settingsApplicationService;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = await _settingsApplicationService.DeleteCalendarRuleAsync(request.Id, cancellationToken);
            if (calendarRule == null)
            {
                logger.LogWarning("CalendarRule with ID {CalendarRuleId} not found for deletion.", request.Id);
                return null!;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("CalendarRule with ID {CalendarRuleId} deleted successfully.", request.Id);

            return calendarRule;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting CalendarRule with ID {CalendarRuleId}.", request.Id);
            throw;
        }
    }
}
