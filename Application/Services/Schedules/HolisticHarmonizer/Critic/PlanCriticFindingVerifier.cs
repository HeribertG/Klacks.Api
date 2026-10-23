// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Deterministic check of every critic finding against the real plan. A vision model reads grids imprecisely and
/// can invent cells, so a finding is kept only when it cites at least one cell, has a pattern label, and every cited
/// cell lies inside the plan and shows exactly the symbol the critic claims. Anything else is dropped as a likely
/// hallucination. Duplicate cells inside one finding are collapsed, and at most <c>maxFindings</c> findings are kept.
/// </summary>
public static class PlanCriticFindingVerifier
{
    public static (IReadOnlyList<PlanCriticFinding> Verified, int Discarded) Verify(
        HarmonyBitmap plan,
        IReadOnlyList<PlanCriticFinding> findings,
        int maxFindings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(findings);

        var verified = new List<PlanCriticFinding>();
        var discarded = 0;
        foreach (var finding in findings)
        {
            if (verified.Count >= maxFindings || !TryVerify(plan, finding, out var clean))
            {
                discarded++;
                continue;
            }

            verified.Add(clean);
        }

        return (verified, discarded);
    }

    private static bool TryVerify(HarmonyBitmap plan, PlanCriticFinding finding, out PlanCriticFinding clean)
    {
        clean = finding;
        if (string.IsNullOrWhiteSpace(finding.Pattern) || finding.Cells.Count == 0)
        {
            return false;
        }

        var cells = new List<PlanCriticCell>(finding.Cells.Count);
        var seen = new HashSet<(int Row, int Day)>();
        foreach (var cell in finding.Cells)
        {
            if (!Matches(plan, cell))
            {
                return false;
            }

            if (seen.Add((cell.Row, cell.Day)))
            {
                cells.Add(cell with { Symbol = PlanCriticSymbols.Normalize(cell.Symbol) });
            }
        }

        clean = finding with { Pattern = finding.Pattern.Trim(), Cells = cells };
        return true;
    }

    private static bool Matches(HarmonyBitmap plan, PlanCriticCell cell)
        => cell.Row >= 0
           && cell.Row < plan.RowCount
           && cell.Day >= 0
           && cell.Day < plan.DayCount
           && PlanCriticSymbols.Normalize(cell.Symbol) == PlanCriticSymbols.For(plan.GetCell(cell.Row, cell.Day));
}
