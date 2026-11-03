// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Keys of translation parameters whose value is a name the reader must see in their own language. A finding
/// that is broadcast to readers of different languages (the schedule error list over SignalR, the period
/// closing issues, the harmonizer compliance report) carries such a name twice: under the plain key as
/// readable text for consumers that do not localize, and under the plain key plus MultiLanguageSuffix as the
/// serialized MultiLanguage the UI resolves to the reader's language at display time.
/// </summary>
public static class LocalizedCommentParamKeys
{
    public const string MultiLanguageSuffix = "I18n";

    public const string Holiday = "holiday";

    public const string HolidayMultiLanguage = Holiday + MultiLanguageSuffix;

    public const string Absence = "absence";

    public const string AbsenceMultiLanguage = Absence + MultiLanguageSuffix;

    /// <summary>
    /// Parameters the schedule error list shows to planners and that ForLanguage leaves out of skill results: the
    /// absence type can reveal a health reason (illness, accident), so the uncovered-absence notice reaches the
    /// language model with person, day and shift only. Skills that read absences directly are not covered.
    /// </summary>
    public static readonly IReadOnlySet<string> WithheldFromAssistant = new HashSet<string>(StringComparer.Ordinal)
    {
        Absence,
        AbsenceMultiLanguage,
    };
}
