// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Settings;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand, Domain.Models.Settings.CalendarRule?>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultiLanguageTranslationService _translationService;

    public PostCommandHandler(
                              ISettingsRepository settingsRepository,
                              ScheduleMapper scheduleMapper,
                              IUnitOfWork unitOfWork,
                              IMultiLanguageTranslationService translationService,
                              ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _translationService = translationService;
    }

    public async Task<Domain.Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await TranslateMultiLanguageFieldsAsync(request.model);

            var calendarRule = _scheduleMapper.ToCalendarRuleEntity(request.model);
            var result = _settingsRepository.AddCalendarRule(calendarRule);

            await _unitOfWork.CompleteAsync();

            return result;
        },
        "operation",
        new { });
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
