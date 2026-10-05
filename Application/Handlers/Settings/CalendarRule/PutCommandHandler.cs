// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a calendar rule (date rule, official flag, names) and drops every cached holiday calculator, so
/// holiday-work warnings and holiday surcharges see the change without a restart.
/// </summary>
/// <param name="request">Carries the calendar rule resource; returns null when the rule id is missing or unknown</param>

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
    private readonly IHolidayCalculatorCache _holidayCalculatorCache;

    public PutCommandHandler(
        ISettingsRepository settingsRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IMultiLanguageTranslationService translationService,
        IHolidayCalculatorCache holidayCalculatorCache,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _translationService = translationService;
        _holidayCalculatorCache = holidayCalculatorCache;
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
            _holidayCalculatorCache.InvalidateAll();

            return _scheduleMapper.ToCalendarRuleResource(existingRule);
        },
        "updating calendar rule",
        new { CalendarRuleId = request.Resource.Id });
    }

    private async Task TranslateMultiLanguageFieldsAsync(CalendarRuleResource resource)
    {
        if (!await _translationService.IsConfiguredAsync())
        {
            return;
        }

        if (resource.Name != null)
        {
            resource.Name = await _translationService.TranslateEmptyFieldsAsync(resource.Name);
        }
        if (resource.Description != null)
        {
            resource.Description = await _translationService.TranslateEmptyFieldsAsync(resource.Description);
        }
    }
}
