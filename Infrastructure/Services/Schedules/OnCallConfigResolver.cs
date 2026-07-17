// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IOnCallConfigResolver"/>. Loads the four WORKTIME_ONCALL_* global settings and
/// delegates parsing to <see cref="OnCallConfig.FromSettings"/> so the resolver and the WorkRepository
/// cache-miss fallback build the config from one shared parser. Defaults mirror the region-setup
/// importer: feature disabled when the enabled flag is absent, presence 100 %, standby 0 %, and the
/// weighted hours excluded from the K5 period caps unless the include flag is explicitly true.
/// </summary>
/// <param name="context">Database access for the WORKTIME_ONCALL_* settings rows.</param>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class OnCallConfigResolver : IOnCallConfigResolver
{
    private readonly DataBaseContext _context;

    public OnCallConfigResolver(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<OnCallConfig> ResolveAsync()
    {
        var keys = OnCallConfig.RelevantSettingKeys;
        var settings = await _context.Settings
            .Where(s => keys.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        return OnCallConfig.FromSettings(settings);
    }
}
