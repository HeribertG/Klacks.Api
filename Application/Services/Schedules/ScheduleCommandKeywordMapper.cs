// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Maps the string command keywords stored in <c>ScheduleCommand.CommandKeyword</c>
/// to the strongly-typed <see cref="ScheduleCommandKeyword"/> enum used by the optimizer.
/// The token set is admin-configurable (Settings), so callers resolve it once per run via
/// <see cref="Klacks.Api.Domain.Interfaces.Schedules.IScheduleCommandKeywordProvider"/>, build the
/// lookup map with <see cref="BuildMap"/>, and reuse it for every <see cref="TryMap"/> call.
/// Unknown or empty values yield false — unknown commands are silently skipped upstream.
/// </summary>
public static class ScheduleCommandKeywordMapper
{
    public static IReadOnlyDictionary<string, ScheduleCommandKeyword> BuildMap(ScheduleCommandKeywordSet keywords) =>
        new Dictionary<string, ScheduleCommandKeyword>(StringComparer.OrdinalIgnoreCase)
        {
            [keywords.FreeToken] = ScheduleCommandKeyword.Free,
            [keywords.NegFreeToken] = ScheduleCommandKeyword.NotFree,
            [keywords.EarlyToken] = ScheduleCommandKeyword.OnlyEarly,
            [keywords.NegEarlyToken] = ScheduleCommandKeyword.NoEarly,
            [keywords.LateToken] = ScheduleCommandKeyword.OnlyLate,
            [keywords.NegLateToken] = ScheduleCommandKeyword.NoLate,
            [keywords.NightToken] = ScheduleCommandKeyword.OnlyNight,
            [keywords.NegNightToken] = ScheduleCommandKeyword.NoNight,
        };

    public static bool TryMap(
        string? rawKeyword, IReadOnlyDictionary<string, ScheduleCommandKeyword> map, out ScheduleCommandKeyword keyword)
    {
        keyword = default;
        if (string.IsNullOrWhiteSpace(rawKeyword))
        {
            return false;
        }

        return map.TryGetValue(rawKeyword.Trim(), out keyword);
    }
}
