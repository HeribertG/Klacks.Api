using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class RuleTokenListHandler : IRequestHandler<RuleTokenList, IEnumerable<StateCountryToken>>
{
    private readonly SettingsApplicationService _settingsApplicationService;

    public RuleTokenListHandler(SettingsApplicationService settingsApplicationService)
    {
        _settingsApplicationService = settingsApplicationService;
    }

    public async Task<IEnumerable<StateCountryToken>> Handle(RuleTokenList request, CancellationToken cancellationToken)
    {
        return await _settingsApplicationService.GetRuleTokenListAsync(request.IsSelected, cancellationToken);
    }
}
