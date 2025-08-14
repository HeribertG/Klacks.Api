using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class TruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedCalendarRule>
{
    private readonly ISettingsRepository _settingsRepository;

    public TruncatedListQueryHandler(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<TruncatedCalendarRule> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
    {
        return await _settingsRepository.GetTruncatedCalendarRuleList(request.Filter);
    }
}
