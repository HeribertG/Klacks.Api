// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fixed row ids of the template macros shipped with Klacks: the eight MacrosSeed rows plus the
/// AllShiftAdditive row of the AddAllShiftAdditiveMacro migration. The region-setup macros import may
/// demote AllShift / AllShiftAdditive to Custom when a country profile ships its own standard macro — a
/// customer-created function holder is never demoted silently. All lists every template id; the
/// AddMacroOrigin migration marks exactly these rows with the Seed origin.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SeededMacroIds
{
    public static readonly Guid Accident = Guid.Parse("b1481e19-eaba-458a-a33b-666f2ecc28d2");
    public static readonly Guid Accident50Percent = Guid.Parse("ac8a7b05-2312-41aa-a21d-e3edba54aef5");
    public static readonly Guid AllShift = Guid.Parse("a3edd3f5-c31c-4746-a9a0-c613d14ffd23");
    public static readonly Guid AllShiftAdditive = Guid.Parse("e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3");
    public static readonly Guid MilitaryService = Guid.Parse("ad86380e-3e8e-4497-95c1-3555ee0803c4");
    public static readonly Guid NullHour = Guid.Parse("f7704df2-bb51-40c8-9ecd-ad57c1064490");
    public static readonly Guid PaidAbsence = Guid.Parse("9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31");
    public static readonly Guid Vacation = Guid.Parse("3bac9e54-4368-4174-8bc9-435ce08aecbd");
    public static readonly Guid Vacation50Percent = Guid.Parse("7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43");

    public static readonly IReadOnlyList<Guid> All =
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
