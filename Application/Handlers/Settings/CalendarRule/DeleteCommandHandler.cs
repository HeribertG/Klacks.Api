// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes a calendar rule and drops every cached holiday calculator that may still contain it.
/// </summary>
/// <param name="request">Id of the calendar rule to delete</param>

using Klacks.Api.Application.Commands.Settings.CalendarRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand, Domain.Models.Settings.CalendarRule>
{    
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHolidayCalculatorCache _holidayCalculatorCache;

    public DeleteCommandHandler(
                                ISettingsRepository settingsRepository,
                                IUnitOfWork unitOfWork,
                                IHolidayCalculatorCache holidayCalculatorCache,
                                ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _holidayCalculatorCache = holidayCalculatorCache;
    }

    public async Task<Domain.Models.Settings.CalendarRule> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarRule = await _settingsRepository.DeleteCalendarRule(request.Id);
            if (calendarRule == null)
            {
                return null!;
            }

            await _unitOfWork.CompleteAsync();
            _holidayCalculatorCache.InvalidateAll();

            return calendarRule;
        }, 
        "operation", 
        new { });
    }
}
