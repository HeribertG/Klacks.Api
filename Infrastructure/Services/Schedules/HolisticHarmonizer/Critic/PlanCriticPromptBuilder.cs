// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using System.Text;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Prompt of the Wizard 3 vision critic. The plan the critic sees has already passed every hard rule and the numeric
/// harmony optimizer, so the prompt asks only for what a numeric score tends to miss and demands exact cell
/// references that the backend verifies. The symbol letters come from <see cref="PlanCriticSymbols"/>, the same
/// alphabet the image, the text grid and the verifier use.
/// </summary>
public static class PlanCriticPromptBuilder
{
    public static string BuildSystemPrompt(PlanCriticRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();
        sb.AppendLine("You are a senior shift planner reviewing a finished staff schedule. You do NOT change the schedule;");
        sb.AppendLine("you only point out patterns a human planner would consider poor.");
        sb.AppendLine();
        sb.AppendLine("CONTEXT:");
        sb.AppendLine("- The schedule already satisfies every hard rule (availability, qualifications, maximum hours, minimum rest)");
        sb.AppendLine("  and was optimised by a numeric harmony score. Do not report hard-rule violations; report what such a");
        sb.AppendLine("  score is likely to miss: fairness between employees (nights, weekends, late shifts), awkward sequences");
        sb.AppendLine("  (e.g. night directly followed by early, isolated single working days, single free days between long blocks),");
        sb.AppendLine("  broken rotation (early -> late -> night), unbalanced weekend distribution, fragmented blocks.");
        sb.AppendLine("- Employees are anonymous. Rows are labelled r00, r01, ...; refer to them only by that label.");
        sb.AppendLine();
        sb.AppendLine("INPUT:");
        sb.AppendLine("- An image of the schedule: rows are employees (label left), columns are days (day of month + weekday");
        sb.AppendLine("  letter on top), Saturday and Sunday columns are tinted. Each working cell shows one letter:");
        sb.AppendLine($"    {PlanCriticSymbols.Early} = early (yellow), {PlanCriticSymbols.Late} = late (orange), {PlanCriticSymbols.Night} = night (dark blue),");
        sb.AppendLine($"    {PlanCriticSymbols.Other} = other (grey), {PlanCriticSymbols.Break} = absence such as vacation (red stripes). Blank cells are free.");
        sb.AppendLine("  A thick black border marks a locked cell.");
        sb.AppendLine($"- The same schedule as a text grid. Use it for exact coordinates: free cells are written as {PlanCriticSymbols.Free},");
        sb.AppendLine("  a trailing * marks a locked cell, the first header line gives the zero-based day index (d00, d01, ...).");
        sb.AppendLine();
        sb.AppendLine("OUTPUT: reply with ONE JSON object and nothing else:");
        sb.AppendLine("{\"findings\":[{\"pattern\":\"night_then_early\",\"severity\":\"high\",\"description\":\"...\",");
        sb.AppendLine("  \"cells\":[{\"row\":3,\"day\":5,\"symbol\":\"N\"},{\"row\":3,\"day\":6,\"symbol\":\"E\"}]}]}");
        sb.AppendLine("- pattern: short snake_case label. severity: low, medium or high.");
        sb.AppendLine($"- description: one or two sentences in the language \"{request.Language}\", naming the row label(s).");
        sb.AppendLine("- cells: every cell that shows the pattern. row = number of the row label (r03 -> 3), day = zero-based day index.");
        sb.AppendLine($"  symbol = the letter in that cell, {PlanCriticSymbols.Free} for a free cell, without the *.");
        sb.AppendLine("  Every cell is checked against the real schedule; a finding with a single wrong cell is discarded.");
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"- At most {request.MaxFindings} findings, most important first. If nothing is worth reporting, reply {{\"findings\":[]}}."));
        return sb.ToString();
    }

    public static string BuildUserMessage(PlanCriticRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sb = new StringBuilder();
        sb.AppendLine("Review this schedule. The image is attached; here is the text grid:");
        sb.AppendLine();
        sb.AppendLine(request.PlanText);
        return sb.ToString();
    }
}
