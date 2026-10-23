// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// The one symbol alphabet the critic image, the critic text grid, the prompt and the verifier share.
/// Free cells are "_" so that every cell has a symbol the critic can cite and the verifier can compare.
/// </summary>
public static class PlanCriticSymbols
{
    public const string Free = "_";
    public const string Early = "E";
    public const string Late = "L";
    public const string Night = "N";
    public const string Other = "O";
    public const string Break = "B";

    public static string For(Cell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        return cell.Symbol switch
        {
            CellSymbol.Early => Early,
            CellSymbol.Late => Late,
            CellSymbol.Night => Night,
            CellSymbol.Other => Other,
            CellSymbol.Break => Break,
            _ => Free,
        };
    }

    /// <summary>Upper-cases and trims a symbol read from a model answer; null or blank stays empty.</summary>
    public static string Normalize(string? symbol)
        => string.IsNullOrWhiteSpace(symbol) ? string.Empty : symbol.Trim().ToUpperInvariant();

    /// <summary>Label of a row in every critic artefact (r00, r01, ...). Names never leave the backend.</summary>
    public static string RowLabel(int rowIndex)
        => "r" + rowIndex.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
}
