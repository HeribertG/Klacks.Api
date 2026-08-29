// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One skill the capability generator is allowed to build with, reduced to what the model needs to chain
/// it: what it does and what it takes. The caller decides membership - only skills that are relevant to
/// the wish and that the risk classifier places in ReadOnly or Reversible ever appear here, so the model
/// cannot propose a composition the execution oracle would have to reject on principle.
/// </summary>
/// <param name="Parameters">Rendered parameter list, required ones marked</param>
/// <param name="ReadOnly">True when the skill only reads, which is what makes a step provable without a rollback</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record CapabilityBuildingBlock(
    string Name,
    string Description,
    IReadOnlyList<string> Parameters,
    bool ReadOnly);
