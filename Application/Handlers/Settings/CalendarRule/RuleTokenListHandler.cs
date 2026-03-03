// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Settings.CalendarRules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRules;

public class RuleTokenListHandler : BaseHandler, IRequestHandler<RuleTokenList, IEnumerable<StateCountryToken>>
{
    private readonly ISettingsTokenService _settingsTokenService;

    public RuleTokenListHandler(
        ISettingsTokenService settingsTokenService,
        ILogger<RuleTokenListHandler> logger)
        : base(logger)
    {
        _settingsTokenService = settingsTokenService;
    }

    public async Task<IEnumerable<StateCountryToken>> Handle(RuleTokenList request, CancellationToken cancellationToken)
    {
        return await _settingsTokenService.GetRuleTokenListAsync(request.IsSelected);
    }
}
