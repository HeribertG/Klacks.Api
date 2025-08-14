using AutoMapper;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using MediatR;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PostCommandHandler : IRequestHandler<PostCommand, Klacks.Api.Domain.Models.Settings.CalendarRule?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              ISettingsRepository settingsRepository,
                              IMapper mapper,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _mapper = mapper;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarRule = _mapper.Map<Klacks.Api.Domain.Models.Settings.CalendarRule>(request.model);
            var result = _settingsRepository.AddCalendarRule(calendarRule);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New CalendarRule added successfully. ID: {CalendarRuleId}", result.Id);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new CalendarRule. ID: {CalendarRuleId}", request.model.Id);
            throw;
        }
    }
}
