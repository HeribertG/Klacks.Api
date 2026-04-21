// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Maps the string command keywords stored in <c>ScheduleCommand.CommandKeyword</c>
/// to the strongly-typed <see cref="ScheduleCommandKeyword"/> enum used by the optimizer.
/// Uses the default token set (FREE/-FREE/EARLY/-EARLY/LATE/-LATE/NIGHT/-NIGHT).
/// Unknown or empty values yield false — unknown commands are silently skipped upstream.
/// </summary>
public static class ScheduleCommandKeywordMapper
{
    private static readonly Dictionary<string, ScheduleCommandKeyword> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FREE"] = ScheduleCommandKeyword.Free,
        ["-FREE"] = ScheduleCommandKeyword.NotFree,
        ["EARLY"] = ScheduleCommandKeyword.OnlyEarly,
        ["-EARLY"] = ScheduleCommandKeyword.NoEarly,
        ["LATE"] = ScheduleCommandKeyword.OnlyLate,
        ["-LATE"] = ScheduleCommandKeyword.NoLate,
        ["NIGHT"] = ScheduleCommandKeyword.OnlyNight,
        ["-NIGHT"] = ScheduleCommandKeyword.NoNight,
    };

    public static bool TryMap(string? rawKeyword, out ScheduleCommandKeyword keyword)
    {
        keyword = default;
        if (string.IsNullOrWhiteSpace(rawKeyword))
        {
            return false;
        }

        return Map.TryGetValue(rawKeyword.Trim(), out keyword);
    }
}
