// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One Stormware PAMICA/POHODA (Czech) entry of a group's AbsenceMappingJson. Accepts the common payroll mapping
/// shape {"WageType":"…","Ausfallschluessel":"…"} as well as the older POHODA-specific shape {"Kod":"…"}: the
/// format-specific Kod wins when it is set, otherwise the common WageType is used.
/// </summary>

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PohodaAbsenceMapping
{
    public string Kod { get; set; } = string.Empty;

    public string WageType { get; set; } = string.Empty;

    /// <returns>The trimmed Kod if set, otherwise the trimmed WageType; empty when neither is set</returns>
    public string ResolveCode()
    {
        return !string.IsNullOrWhiteSpace(Kod)
            ? Kod.Trim()
            : WageType?.Trim() ?? string.Empty;
    }
}
