// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One learned capability: a recipe the loop composed from existing skills. Empty before stage G3, which
/// is what generates them - the section exists in G1 so the card has a stable contract.
/// </summary>
/// <param name="Id">Id of the recipe</param>
/// <param name="Steps">The composed skills in execution order</param>
/// <param name="Quote">Usefulness quote, null until stage G3 measures it</param>
/// <param name="Uses">Observed uses, null until stage G3 measures it</param>
/// <param name="NeedsFirstUse">True while the execution oracle still owes a first confirmed real run</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record LearnedCapabilityDto(
    Guid Id,
    string Name,
    string Goal,
    IReadOnlyList<LearnedCapabilityStepDto> Steps,
    DateTime? LearnedAt,
    decimal? Quote,
    int? Uses,
    bool NeedsFirstUse);
