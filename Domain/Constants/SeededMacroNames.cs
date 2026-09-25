// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Names of the template macros shipped with Klacks (the MacrosSeed rows and the AllShiftAdditive row of the
/// AddAllShiftAdditiveMacro migration). The assistant may never give one of these names to a macro of its own,
/// not even after a template was renamed or deleted, so a template name always means the template.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SeededMacroNames
{
    public const string Accident = "Accident";
    public const string Accident50Percent = "Accident50%";
    public const string AllShift = "AllShift";
    public const string AllShiftAdditive = "AllShiftAdditive";
    public const string MilitaryService = "Military Service";
    public const string NullHour = "Null Hour";
    public const string PaidAbsence = "Paid Absence";
    public const string Vacation = "Vacation";
    public const string Vacation50Percent = "Vacation50%";

    public static readonly IReadOnlyList<string> All =
    [
        Accident,
        Accident50Percent,
        AllShift,
        AllShiftAdditive,
        MilitaryService,
        NullHour,
        PaidAbsence,
        Vacation,
        Vacation50Percent
    ];
}
