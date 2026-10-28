// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Action filter behind BlockedSettingKeysOnPlayground: on the Playground instance a visitor is the shared demo admin
/// and must not set the STT, TTS or DeepL keys and engines or the update channel and pinned version (the operator pays
/// for them or carries the risk). The filter checks the key in the body and, because a write addresses the stored row by
/// its id, also the key of the row stored under that id, so an update cannot hide a blocked key behind another Type. It
/// answers HTTP 403 with errorCode PLAYGROUND_FEATURE_BLOCKED for such a key; all other settings (language, layout, voice,
/// output mode ...) pass. With the Playground mode off it does nothing at all, not even a lookup.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>
/// <param name="settingsRepository">Reads the stored row of a setting by id</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Playground;
using Microsoft.AspNetCore.Mvc.Filters;
using SettingEntity = Klacks.Api.Domain.Models.Settings.Settings;

namespace Klacks.Api.Presentation.Filters;

public class BlockedSettingKeysOnPlaygroundFilter : IAsyncActionFilter
{
    private readonly IPlaygroundMode _mode;
    private readonly ISettingsRepository _settingsRepository;

    public BlockedSettingKeysOnPlaygroundFilter(IPlaygroundMode mode, ISettingsRepository settingsRepository)
    {
        _mode = mode;
        _settingsRepository = settingsRepository;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_mode.IsActive)
        {
            foreach (var setting in context.ActionArguments.Values.OfType<SettingEntity>())
            {
                if (await IsBlockedAsync(setting))
                {
                    context.Result = PlaygroundProblemResultFactory.FeatureBlocked();
                    return;
                }
            }
        }

        await next();
    }

    private async Task<bool> IsBlockedAsync(SettingEntity setting)
    {
        if (PlaygroundBlockedSettingKeys.IsBlocked(setting.Type))
        {
            return true;
        }

        if (setting.Id == Guid.Empty)
        {
            return false;
        }

        var stored = await _settingsRepository.GetSettingByIdNoTracking(setting.Id);

        return stored is not null && PlaygroundBlockedSettingKeys.IsBlocked(stored.Type);
    }
}
