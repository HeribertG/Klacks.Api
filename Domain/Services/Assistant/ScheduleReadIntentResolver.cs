// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a turn must carry read_schedule_state, the only skill that answers "who has duty on Monday in
/// Winterthur". Vector retrieval does not surface it for such questions: the word "Dienst" pulls the shift-structure
/// skills (create_shift, add_shift_to_group, ...) into the 25-candidate pool, read_schedule_state never enters the pool,
/// and the route boost (applied after the pool is built) cannot lift a skill that is not in it. Two signals decide:
/// the user is on the schedule page (any UI language), or the message is a question that combines a date signal
/// (weekday, month, relative day, numeric date) with a duty or plan word in one of the core languages de/en/fr/it.
/// A recognised question guarantees read_schedule_state together with list_groups, because the skill takes a group
/// UUID and the user names the group; the schedule page alone guarantees read_schedule_state only. The skills only
/// read, so the rule is liberal; a false positive costs one tool slot.
/// Matching: a duty stem matches anywhere in a word (Nachtdienst, Bereitschaftsdienst); a weekday prefix is cut off
/// before the duty check, so "Dienstagabend" holds no duty word while "Montagsdienst" does; a word that starts with a
/// weekday counts as a date (montags, Freitagabend); "hier" is not a date.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>
/// <param name="currentRoute">Frontend route from the page context; may carry entity ids, query strings or fragments.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ScheduleReadIntentResolver
{
    public const string ReadScheduleStateSkillName = "read_schedule_state";
    public const string ListGroupsSkillName = "list_groups";

    private static readonly string[] GuaranteedSkillsForQuestion = [ReadScheduleStateSkillName, ListGroupsSkillName];

    private static readonly string[] GuaranteedSkillsForPage = [ReadScheduleStateSkillName];

    internal static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly Regex NumericDatePattern = new(
        @"\b\d{1,2}\s*[./-]\s*\d{1,2}(?:\s*[./-]\s*\d{2,4})?\b", RegexOptions.Compiled);

    private static readonly HashSet<string> WeekdayWords = new(StringComparer.Ordinal)
    {
        "montag", "dienstag", "mittwoch", "donnerstag", "freitag", "samstag", "sonntag",
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
        "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi", "dimanche",
        "lunedì", "lunedi", "martedì", "martedi", "mercoledì", "mercoledi", "giovedì", "giovedi",
        "venerdì", "venerdi", "sabato", "domenica",
    };

    private static readonly HashSet<string> MonthWords = new(StringComparer.Ordinal)
    {
        "januar", "jänner", "februar", "märz", "maerz", "april", "mai", "juni", "juli", "august", "september",
        "oktober", "november", "dezember",
        "january", "february", "march", "may", "june", "july", "october", "december",
        "janvier", "février", "fevrier", "mars", "avril", "juin", "juillet", "août", "aout", "septembre",
        "octobre", "novembre", "décembre", "decembre",
        "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno", "luglio", "agosto", "settembre",
        "ottobre", "dicembre",
    };

    private static readonly HashSet<string> RelativeDayWords = new(StringComparer.Ordinal)
    {
        "heute", "morgen", "übermorgen", "uebermorgen", "gestern", "woche", "wochenende",
        "today", "tomorrow", "yesterday", "tonight", "week", "weekend",
        "demain", "semaine",
        "oggi", "domani", "ieri", "stasera", "settimana",
    };

    private static readonly string[] RelativeDayStems = ["aujourd"];

    private static readonly string[] DutyStems =
    [
        "dienst", "schicht", "eingeteilt", "einsatz", "arbeit", "steht", "plan", "geplant", "pikett", "besetz", "belegung",
        "shift", "duty", "working", "works", "scheduled", "assigned", "rostered", "roster", "planned", "schedule",
        "service", "garde", "travaill", "planifi", "affect", "planning", "horaire", "poste",
        "turn", "servizi", "lavor", "pianific", "assegnat", "programmat", "orario",
    ];

    private static readonly HashSet<string> InterrogativeWords = new(StringComparer.Ordinal)
    {
        "wer", "wen", "wem", "welche", "welcher", "welchen", "wieviele", "zeig", "zeige",
        "who", "whom", "which", "what", "show", "list",
        "qui", "quel", "quels", "quelle", "quelles", "combien", "montre", "affiche",
        "chi", "quale", "quali", "quanti", "mostra",
    };

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message, string? currentRoute)
    {
        if (IsScheduleQuestion(message))
        {
            return GuaranteedSkillsForQuestion;
        }

        return IsSchedulePage(currentRoute) ? GuaranteedSkillsForPage : [];
    }

    /// <summary>True on the schedule page, also with entity ids, query strings or fragments in the route.</summary>
    /// <param name="currentRoute">Frontend route from the page context</param>
    public static bool IsSchedulePage(string? currentRoute)
    {
        if (string.IsNullOrWhiteSpace(currentRoute))
        {
            return false;
        }

        var path = currentRoute.Split('?', '#')[0].TrimEnd('/');
        return string.Equals(path, ProactiveActionRoutes.Schedule, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(ProactiveActionRoutes.Schedule + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsScheduleQuestion(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        var words = WordPattern.Matches(lower).Select(match => match.Value).ToList();

        if (!HasDateSignal(lower, words))
        {
            return false;
        }

        var isQuestion = lower.IndexOfAny(RecipeReplyGuard.QuestionMarks) >= 0
            || words.Any(InterrogativeWords.Contains);
        if (!isQuestion)
        {
            return false;
        }

        return words
            .Select(WithoutWeekdayPrefix)
            .Any(word => DutyStems.Any(stem => word.Contains(stem, StringComparison.Ordinal)));
    }

    /// <summary>
    /// True when the message names a day in one of the core languages: weekday, month, relative day or numeric date.
    /// </summary>
    /// <param name="message">The current user chat message, matched case-insensitively.</param>
    public static bool HasDateSignal(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var lower = message.ToLowerInvariant();
        return HasDateSignal(lower, WordPattern.Matches(lower).Select(match => match.Value).ToList());
    }

    private static bool HasDateSignal(string lower, IReadOnlyList<string> words) =>
        NumericDatePattern.IsMatch(lower)
        || words.Any(word => StartsWithWeekday(word) || MonthWords.Contains(word) || RelativeDayWords.Contains(word)
            || RelativeDayStems.Any(stem => word.StartsWith(stem, StringComparison.Ordinal)));

    private static bool StartsWithWeekday(string word) =>
        WeekdayWords.Any(weekday => word.StartsWith(weekday, StringComparison.Ordinal));

    private static string WithoutWeekdayPrefix(string word)
    {
        var weekday = WeekdayWords.FirstOrDefault(candidate => word.StartsWith(candidate, StringComparison.Ordinal));
        return weekday == null ? word : word[weekday.Length..];
    }
}
