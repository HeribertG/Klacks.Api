using AutoMapper;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using MediatR;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.CalendarRule?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              ISettingsRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = mapper.Map<CalendarRuleResource, Klacks.Api.Domain.Models.Settings.CalendarRule>(request.model);
            repository.AddCalendarRule(calendarRule);

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
