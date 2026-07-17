// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IOnCallConfigResolver
{
    /// <summary>
    /// Resolves the system's single on-call configuration from the WORKTIME_ONCALL_* global
    /// settings. On-call is a global (not client-scoped) worktime policy, so the resolution takes no
    /// client or date. When the settings are absent the feature is disabled and the weighting factors
    /// fall back to their documented defaults (presence 100 %, standby 0 %).
    /// </summary>
    Task<OnCallConfig> ResolveAsync();
}
