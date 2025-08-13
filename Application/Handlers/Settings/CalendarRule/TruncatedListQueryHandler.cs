using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class TruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedCalendarRule>
{
    private readonly SettingsApplicationService _settingsApplicationService;

    public TruncatedListQueryHandler(SettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService;
    }

    public async Task<TruncatedCalendarRule> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
    {
        return await _settingsApplicationService.GetTruncatedCalendarRulesAsync(request.Filter, cancellationToken);
    }
}
