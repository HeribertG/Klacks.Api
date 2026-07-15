// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fixed row ids of the seeded standard macros (MacrosSeed / AddAllShiftAdditiveMacro migration). The
/// region-setup macros import may demote THESE rows to Custom when a country profile ships its own
/// standard macro — a customer-created function holder is never demoted silently.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SeededMacroIds
{
    public static readonly Guid AllShift = Guid.Parse("a3edd3f5-c31c-4746-a9a0-c613d14ffd23");
    public static readonly Guid AllShiftAdditive = Guid.Parse("e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3");
}
