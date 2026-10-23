// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Rendering.Grid;
using SkiaSharp;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Plan image for the vision critic. Same cell colours and letters as the Wizard 3 LLM image
/// (HarmonyBitmapPngRenderer), but the row header shows r00, r01, ... instead of agent initials, so no name,
/// initial or other personal label leaves the backend; the frontend maps the row index back to the agent through
/// AgentDisplayNames. The colours are repeated here on purpose: the optimizer renderer always prints initials and
/// changing it would need a coordinated release of Klacks.ScheduleOptimizer.
/// </summary>
public sealed class AnonymizedPlanImageRenderer
{
    private const int CellSize = 24;
    private const int HeaderLeft = 40;
    private const int HeaderTop = 32;
    private const int ThickBorderWidth = 2;

    private static readonly SKColor EarlyFillColor = new(0xFF, 0xD7, 0x00);
    private static readonly SKColor LateFillColor = new(0xFF, 0x8C, 0x00);
    private static readonly SKColor NightFillColor = new(0x1E, 0x3A, 0x8A);
    private static readonly SKColor OtherFillColor = new(0x4B, 0x55, 0x63);
    private static readonly SKColor BreakStripeColor = new(0xDC, 0x26, 0x26);

    private readonly GridImageRenderer _gridRenderer =
        new(new GridImageRenderOptions(CellSize, HeaderLeft, HeaderTop, ThickBorderWidth));

    public byte[] Render(HarmonyBitmap plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return _gridRenderer.Render(ToGridImage(plan));
    }

    /// <summary>Row labels exactly as painted into the image; exposed so tests can pin that no name is used.</summary>
    public static IReadOnlyList<string> RowLabels(HarmonyBitmap plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return Enumerable.Range(0, plan.RowCount).Select(PlanCriticSymbols.RowLabel).ToList();
    }

    private static GridImage ToGridImage(HarmonyBitmap plan)
    {
        var columns = plan.Days
            .Select(d => new GridImageColumn(
                d.Day.ToString(CultureInfo.InvariantCulture),
                WeekdayLetter(d.DayOfWeek),
                d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
            .ToList();

        var cells = new GridImageCell[plan.RowCount, plan.DayCount];
        for (var row = 0; row < plan.RowCount; row++)
        {
            for (var day = 0; day < plan.DayCount; day++)
            {
                cells[row, day] = ToGridCell(plan.GetCell(row, day));
            }
        }

        return new GridImage(RowLabels(plan), columns, cells, []);
    }

    private static GridImageCell ToGridCell(Cell cell) => cell.Symbol switch
    {
        CellSymbol.Early => new GridImageCell(EarlyFillColor, null, PlanCriticSymbols.Early, false, cell.IsLocked),
        CellSymbol.Late => new GridImageCell(LateFillColor, null, PlanCriticSymbols.Late, false, cell.IsLocked),
        CellSymbol.Night => new GridImageCell(NightFillColor, null, PlanCriticSymbols.Night, true, cell.IsLocked),
        CellSymbol.Other => new GridImageCell(OtherFillColor, null, PlanCriticSymbols.Other, true, cell.IsLocked),
        CellSymbol.Break => new GridImageCell(null, BreakStripeColor, PlanCriticSymbols.Break, false, cell.IsLocked),
        _ => new GridImageCell(null, null, null, false, cell.IsLocked),
    };

    private static string WeekdayLetter(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => "M",
        DayOfWeek.Tuesday => "T",
        DayOfWeek.Wednesday => "W",
        DayOfWeek.Thursday => "T",
        DayOfWeek.Friday => "F",
        _ => "S",
    };
}
