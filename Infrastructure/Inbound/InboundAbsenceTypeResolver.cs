// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the absence type an inbound sickness, vacation or training message refers to by matching keywords against
/// the absence names, and tells a training request apart from a vacation request. Training is not its own EmailIntent:
/// the LLM classifies a course request as VacationRequest too, so the type is picked from the wording instead. The
/// keyword sets stay separate because ResolveByKeywords demands exactly one match - a merged list would match both the
/// vacation and the training type in any installation that has both, and record nothing at all.
/// </summary>
/// <param name="absences">Absence types of the installation.</param>
/// <param name="keywords">Keyword set the absence name must contain (case-insensitive).</param>

using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Inbound;

public static class InboundAbsenceTypeResolver
{
    public static readonly string[] SicknessKeywords = ["krank", "sick", "malad", "malatt"];

    public static readonly string[] VacationKeywords = ["ferien", "urlaub", "vacation", "holiday", "vacanc", "vacanz", "congé"];

    public static readonly string[] TrainingKeywords =
        ["schulung", "weiterbildung", "fortbildung", "kurs", "training", "course", "formation", "corso"];

    /// <summary>
    /// The single non-deleted absence type whose name contains one of the keywords, or null when none or several match.
    /// </summary>
    public static Absence? ResolveByKeywords(IEnumerable<Absence> absences, string[] keywords)
    {
        var matches = absences
            .Where(a => !a.IsDeleted && MatchesAnyKeyword(a, keywords))
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// True when the subject or the analysis summary mentions a training keyword.
    /// </summary>
    public static bool MentionsTraining(InboundSource source, InboundAnalysis analysis)
    {
        var haystack = $"{source.Subject} {analysis.Summary}".ToLowerInvariant();
        return TrainingKeywords.Any(k => haystack.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesAnyKeyword(Absence absence, string[] keywords)
    {
        var names = new[] { absence.Name?.De, absence.Name?.En, absence.Name?.Fr, absence.Name?.It };
        return names.Any(name =>
            !string.IsNullOrWhiteSpace(name) &&
            keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)));
    }
}
