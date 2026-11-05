// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a chat message asks to enter what e-mails contain into the planning data ("Trag das bitte alles ein:
/// Verfügbarkeiten zu den Verfügbarkeiten, Wünsche als Keywords", "Übernimm alles aus diesen Mails"). Three signals must
/// hold together: the message carries an enter/apply verb, it names a thing to enter (a referent such as "alles" / "all" /
/// "tout" / "tutto", or availabilities, wishes and keywords), and the e-mails are the source, either because the message
/// names mails or because the previous tool-calling turn read them (PreviousTurnSkillNames of the guarantee request, the
/// same names TurnPreparationService reads off the last-action record, so the toolset and the recipe decision cannot
/// disagree). A message that opens with an information question never counts.
/// Why it exists: the data is already in the conversation, so the guided recipe offer ("Möchtest du diese geführte Aktion
/// starten: ... verfügbar ...") would ask the user for what they just had read out, and the retrieval for such a wording
/// ranks mail-management skills, not the write skills. Core languages de/en/fr/it only, matched case-insensitively on word
/// starts; a plugin-language wording does not count. The skills it names only matter for visibility: the autonomy gate and
/// the confirmations are untouched.
/// </summary>
/// <param name="message">The current user chat message.</param>
/// <param name="previousTurnSkillNames">Skills the previous tool-calling turn ran successfully; null or empty when none.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class MailApplyIntentResolver
{
    public const string SearchEmployeesSkillName = "search_employees";
    public const string SetClientAvailabilitySkillName = "set_client_availability";
    public const string AddScheduleCommandSkillName = "add_schedule_command";
    public const string AddScheduleCommandsRangeSkillName = "add_schedule_commands_range";
    public const string OpenScheduleSkillName = "open_schedule";

    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly string[] GuaranteedSkills =
    [
        SearchEmployeesSkillName,
        SetClientAvailabilitySkillName,
        AddScheduleCommandSkillName,
        AddScheduleCommandsRangeSkillName,
        OpenScheduleSkillName,
    ];

    private static readonly string[] MailReadGuaranteedSkills = ["list_emails", "read_email"];

    private static readonly HashSet<string> MailReadSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "list_emails", "read_email", "get_email_analysis", "list_emails_by_client", "list_emails_by_group", "translate_email",
    };

    private static readonly string[] MailWordStems = ["mail", "email", "courriel", "posta"];

    private static readonly string[] EnterVerbStems =
    [
        "trag", "eintrag", "übernimm", "übernehm", "uebernimm", "uebernehm", "erfass", "hinterleg", "verbuch", "setz",
        "enter", "apply", "applied", "record", "input", "register", "save", "put", "transfer", "import",
        "saisi", "enregistr", "inscri", "appliqu", "report", "mets", "mettez",
        "inserisc", "registr", "applic", "riport", "metti", "salva", "imposta",
    ];

    private static readonly HashSet<string> ReferentWords = new(StringComparer.Ordinal)
    {
        "alles", "alle", "allem", "das", "dies", "diese", "diesen", "dieses", "davon", "daraus",
        "all", "everything", "this", "that", "these", "those", "it", "them",
        "tout", "tous", "toutes", "ça", "ca", "cela", "ceci", "ces",
        "tutto", "tutti", "tutte", "questo", "questa", "questi", "queste", "quello",
    };

    private static readonly string[] ContentNounStems =
    [
        "verfügbar", "verfuegbar", "wunsch", "wünsch", "wuensch", "keyword", "stichwort",
        "availab", "wish", "disponib", "souhait", "desider",
    ];

    /// <summary>The skills a mail-driven entry turn needs: the person lookup, the two writes and the schedule view.</summary>
    public static IReadOnlyList<string> GuaranteedSkillNames => GuaranteedSkills;

    /// <summary>
    /// The skills that read the mails themselves, needed when the message names the mails as its source and no earlier turn
    /// has read them ("Lies die Mails und trag ... ein"): the imperative "Lies" carries no keyword of read_email, and at the
    /// provider cap the model listed the mails but could not open one (live 2026-11-05).
    /// </summary>
    public static IReadOnlyList<string> MailReadSkillNames => MailReadGuaranteedSkills;

    /// <summary>True when the message itself names e-mails (a mail word in de/en/fr/it).</summary>
    /// <param name="message">The current user chat message</param>
    public static bool NamesMails(string? message) =>
        !string.IsNullOrWhiteSpace(message)
        && WordPattern.Matches(message.ToLowerInvariant()).Any(match => StartsWithAny(match.Value, MailWordStems));

    /// <summary>True when the message asks to enter the contents of e-mails into the planning data.</summary>
    /// <param name="message">The current user chat message</param>
    /// <param name="previousTurnSkillNames">Skills the previous tool-calling turn ran successfully</param>
    public static bool IsMailApply(string? message, IReadOnlyCollection<string>? previousTurnSkillNames)
    {
        if (string.IsNullOrWhiteSpace(message) || MutationIntentDetector.IsInformationQuestion(message))
        {
            return false;
        }

        var words = WordPattern.Matches(message.ToLowerInvariant()).Select(match => match.Value).ToList();
        if (!words.Any(word => StartsWithAny(word, EnterVerbStems)))
        {
            return false;
        }

        if (!words.Any(word => ReferentWords.Contains(word) || StartsWithAny(word, ContentNounStems)))
        {
            return false;
        }

        return NamesMails(message) || PreviousTurnReadMails(previousTurnSkillNames);
    }

    private static bool PreviousTurnReadMails(IReadOnlyCollection<string>? previousTurnSkillNames) =>
        previousTurnSkillNames != null && previousTurnSkillNames.Any(MailReadSkills.Contains);

    private static bool StartsWithAny(string word, string[] stems) =>
        stems.Any(stem => word.StartsWith(stem, StringComparison.Ordinal));
}
