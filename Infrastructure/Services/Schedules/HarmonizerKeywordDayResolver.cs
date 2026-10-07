// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Turns the recognized schedule commands of a Wizard 2 run into its day availability inputs. Several commands of
/// one (agent, day) restrict cumulatively through <see cref="ScheduleCommandKeywordCombiner"/>, exactly as Wizard 1
/// reads them; a combination no shift kind satisfies closes the day like FREE. A NotFree-only day restricts nothing.
/// </summary>
public static class HarmonizerKeywordDayResolver
{
    /// <summary>
    /// Reduces the commands to free days and per-day shift-kind restrictions.
    /// </summary>
    /// <param name="commands">Recognized commands of the run (agent, day, keyword), in any order.</param>
    public static HarmonizerKeywordDays Resolve(IEnumerable<(Guid AgentId, DateOnly Date, ScheduleCommandKeyword Keyword)> commands)
    {
        var freeDates = new HashSet<(Guid AgentId, DateOnly Date)>();
        var restrictions = new Dictionary<(Guid AgentId, DateOnly Date), (CellSymbol? Required, CellSymbol? Forbidden)>();

        foreach (var day in commands.GroupBy(c => (c.AgentId, c.Date)))
        {
            var combined = ScheduleCommandKeywordCombiner.Combine(day.Select(c => c.Keyword));
            switch (combined)
            {
                case ScheduleCommandKeyword.Free:
                    freeDates.Add(day.Key);
                    break;
                case ScheduleCommandKeyword.OnlyEarly:
                    restrictions[day.Key] = (CellSymbol.Early, null);
                    break;
                case ScheduleCommandKeyword.OnlyLate:
                    restrictions[day.Key] = (CellSymbol.Late, null);
                    break;
                case ScheduleCommandKeyword.OnlyNight:
                    restrictions[day.Key] = (CellSymbol.Night, null);
                    break;
                case ScheduleCommandKeyword.NoEarly:
                    restrictions[day.Key] = (null, CellSymbol.Early);
                    break;
                case ScheduleCommandKeyword.NoLate:
                    restrictions[day.Key] = (null, CellSymbol.Late);
                    break;
                case ScheduleCommandKeyword.NoNight:
                    restrictions[day.Key] = (null, CellSymbol.Night);
                    break;
            }
        }

        return new HarmonizerKeywordDays(freeDates, restrictions);
    }
}
