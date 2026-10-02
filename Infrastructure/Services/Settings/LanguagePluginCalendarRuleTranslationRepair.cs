// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Undoes the translation merge the pre-v1.0.36 installer did by id alone: it wrote every non-core name and
/// description value of a pack rule into whatever row held that id, including a rule of another country. Only a
/// value that is exactly the polluting pack text is touched, so a value the customer changed stays. The value is
/// replaced by the holder's own pack text for that language, or removed when there is none, so the normal language
/// fallback applies. Core languages were never merged and are never touched.
/// </summary>
/// <param name="holder">Rule of another country that holds the colliding id and received the foreign texts</param>
/// <param name="polluter">Pack entry whose texts were merged into the holder</param>
/// <param name="ownSource">Pack entry the holder itself comes from, or null when no pack ships it</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public static class LanguagePluginCalendarRuleTranslationRepair
{
    public static int Repair(CalendarRule holder, CalendarRule polluter, CalendarRule? ownSource) =>
        RepairValues(holder.Name, polluter.Name, ownSource?.Name)
        + RepairValues(holder.Description, polluter.Description, ownSource?.Description);

    private static int RepairValues(MultiLanguage target, MultiLanguage polluting, MultiLanguage? own)
    {
        var repaired = 0;

        foreach (var (language, pollutedValue) in polluting.GetAllValues().ToList())
        {
            if (MultiLanguage.CoreLanguages.Contains(language) || string.IsNullOrEmpty(pollutedValue))
            {
                continue;
            }

            if (!string.Equals(target.GetValue(language), pollutedValue, StringComparison.Ordinal))
            {
                continue;
            }

            var ownValue = own?.GetValue(language);
            if (string.Equals(ownValue, pollutedValue, StringComparison.Ordinal))
            {
                continue;
            }

            target.SetValue(language, ownValue);
            repaired++;
        }

        return repaired;
    }
}
