// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One PAXml (Sweden) entry of a group's AbsenceMappingJson. Accepts the common payroll mapping shape
/// {"WageType":"…","Ausfallschluessel":"…"} as well as the older PAXml-specific shape {"LonArt":"…"}: the
/// format-specific LonArt wins when it is set, otherwise the common WageType is used.
/// </summary>

namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PaxmlAbsenceMapping
{
    public string LonArt { get; set; } = string.Empty;

    public string WageType { get; set; } = string.Empty;

    /// <returns>The trimmed LonArt if set, otherwise the trimmed WageType; empty when neither is set</returns>
    public string ResolveCode()
    {
        return !string.IsNullOrWhiteSpace(LonArt)
            ? LonArt.Trim()
            : WageType?.Trim() ?? string.Empty;
    }
}
