// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IExplainOffersSwitch"/>: explain offers are on unless the setting KLACKSY_EXPLAIN_OFFERS_ENABLED
/// parses as "false" (absent, empty or unparseable means on) or the proactive kill switch is active. Read on every call,
/// no caching, so flipping the setting takes effect with the next offer.
/// </summary>
/// <param name="settingsReader">Reads the explain-offers setting</param>
/// <param name="governance">Reads the proactive kill switch</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Assistant.ExplainOffers;

public sealed class ExplainOffersSwitch : IExplainOffersSwitch
{
    private readonly ISettingsReader _settingsReader;
    private readonly IProactiveGovernanceResolver _governance;

    public ExplainOffersSwitch(ISettingsReader settingsReader, IProactiveGovernanceResolver governance)
    {
        _settingsReader = settingsReader;
        _governance = governance;
    }

    public async Task<bool> IsOnAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.KlacksyExplainOffersEnabled);
        var settingOn = string.IsNullOrWhiteSpace(setting?.Value) || !bool.TryParse(setting.Value, out var enabled) || enabled;
        return settingOn && !await _governance.IsKillSwitchActiveAsync(cancellationToken);
    }
}
