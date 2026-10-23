// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using System.Text;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Text grid of the plan for the vision critic. Unlike the Wizard 3 text renderer it carries no agent names: rows are
/// labelled r00, r01, ... only. The header gives the zero-based day index the critic must cite, plus day of month and
/// weekday letter so weekends stay recognisable.
/// </summary>
public static class AnonymizedPlanTextRenderer
{
    private const int RowLabelWidth = 6;
    private const int CellWidth = 5;
    private const string DayIndexPrefix = "d";
    private const string LockedSuffix = "*";

    public static string Render(HarmonyBitmap plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sb = new StringBuilder();
        AppendHeader(sb, plan);
        for (var row = 0; row < plan.RowCount; row++)
        {
            sb.Append(PlanCriticSymbols.RowLabel(row).PadRight(RowLabelWidth));
            for (var day = 0; day < plan.DayCount; day++)
            {
                var cell = plan.GetCell(row, day);
                var text = PlanCriticSymbols.For(cell) + (cell.IsLocked ? LockedSuffix : string.Empty);
                sb.Append(text.PadRight(CellWidth));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, HarmonyBitmap plan)
    {
        sb.Append(string.Empty.PadRight(RowLabelWidth));
        for (var day = 0; day < plan.DayCount; day++)
        {
            sb.Append((DayIndexPrefix + day.ToString("D2", CultureInfo.InvariantCulture)).PadRight(CellWidth));
        }

        sb.AppendLine();
        sb.Append(string.Empty.PadRight(RowLabelWidth));
        for (var day = 0; day < plan.DayCount; day++)
        {
            var date = plan.Days[day];
            var label = date.Day.ToString("D2", CultureInfo.InvariantCulture) + WeekdayLetter(date.DayOfWeek);
            sb.Append(label.PadRight(CellWidth));
        }

        sb.AppendLine();
    }

    private static string WeekdayLetter(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "Mo",
        DayOfWeek.Tuesday => "Tu",
        DayOfWeek.Wednesday => "We",
        DayOfWeek.Thursday => "Th",
        DayOfWeek.Friday => "Fr",
        DayOfWeek.Saturday => "Sa",
        _ => "Su",
    };
}
