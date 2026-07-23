// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The choices offered for the first planning-profile question ("what is your business most comparable
/// to?"): one of the five shipped industry slugs (see <see cref="IndustrySlugs"/>) whose template rules
/// are copied, or <see cref="Scratch"/> to begin with a single blank rule. Scratch is deliberately a
/// distinct token from <see cref="IndustrySlugs.Custom"/>: Scratch is the base-choice for the dialog,
/// while Custom is the ACTIVE_INDUSTRIES marker every applied profile ends on regardless of the choice.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class PlanningProfileBaseChoices
{
    public const string Scratch = "scratch";

    public static readonly IReadOnlyList<string> All = new[]
    {
        IndustrySlugs.Homecare,
        IndustrySlugs.Healthcare,
        IndustrySlugs.Security,
        IndustrySlugs.Facility,
        IndustrySlugs.Logistics,
        Scratch,
    };
}
