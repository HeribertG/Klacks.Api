using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CalendarRuleResource>, CalendarRuleResource?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultiLanguageTranslationService _translationService;

    public PutCommandHandler(
        ISettingsRepository settingsRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IMultiLanguageTranslationService translationService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _translationService = translationService;
    }

    public async Task<CalendarRuleResource?> Handle(PutCommand<CalendarRuleResource> request, CancellationToken cancellationToken)
    {
        await TranslateMultiLanguageFieldsAsync(request.Resource);

        return await ExecuteAsync(async () =>
        {
            if (request.Resource.Id == null)
            {
                return null;
            }

            var existingRule = await _settingsRepository.GetCalendarRule(request.Resource.Id.Value);
            if (existingRule == null)
            {
                return null;
            }

            _scheduleMapper.UpdateCalendarRuleEntity(request.Resource, existingRule);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToCalendarRuleResource(existingRule);
        },
        "updating calendar rule",
        new { CalendarRuleId = request.Resource.Id });
    }

    private async Task TranslateMultiLanguageFieldsAsync(CalendarRuleResource resource)
    {
        if (!_translationService.IsConfigured)
        {
            return;
        }

        resource.Name = await _translationService.TranslateEmptyFieldsAsync(resource.Name);
        resource.Description = await _translationService.TranslateEmptyFieldsAsync(resource.Description);
    }
}
