// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Exports.Payroll;

/// <summary>
/// Per-group (location/branch) configuration for a country-pack payroll export.
/// Holds the target-system format, delimiter, encoding and the mapping of Klacks time values
/// to the target payroll codes (wage type / absence key). The concrete wage-type and absence-key
/// values are tenant/tax-advisor specific and are intentionally stored here rather than hard-coded
/// in the formatter.
/// </summary>
/// <remarks>
/// AbsenceMappingJson is a serialized dictionary keyed by the stable Absence id (Guid string) mapping
/// to a target payload; it is parsed by the formatter, not by EF. The concrete DATEV wage-type and
/// Ausfallschluessel values cannot be validated without a DATEV account and are placeholders until a
/// customer's tax advisor provides them.
/// </remarks>
public class PayrollExportGroupConfig : BaseEntity
{
    public Guid GroupId { get; set; }

    [MaxLength(32)]
    public string TargetSystem { get; set; } = string.Empty;

    [MaxLength(4)]
    public string Delimiter { get; set; } = ";";

    [MaxLength(32)]
    public string Encoding { get; set; } = "windows-1252";

    [MaxLength(32)]
    public string BaseWageType { get; set; } = string.Empty;

    [MaxLength(32)]
    public string SurchargeWageType { get; set; } = string.Empty;

    public string AbsenceMappingJson { get; set; } = "{}";
}
