using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using MediatR;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.CalendarRule?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly SettingsApplicationService _settingsApplicationService;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              SettingsApplicationService settingsApplicationService,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        _settingsApplicationService = settingsApplicationService;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = await _settingsApplicationService.CreateCalendarRuleAsync(request.model, cancellationToken);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New CalendarRule added successfully. ID: {CalendarRuleId}", calendarRule.Id);

            return calendarRule;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new CalendarRule. ID: {CalendarRuleId}", request.model.Id);
            throw;
        }
    }
}
