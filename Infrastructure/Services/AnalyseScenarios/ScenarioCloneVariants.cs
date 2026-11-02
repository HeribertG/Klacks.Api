// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The variants (keys) of a scenario clone, sorted once by ScenarioCloneSourceResolver so links, shifts and works agree.
/// </summary>
/// <param name="CarriedTokens">Tokens the clone works with: the ones its caller names plus the inherited keys</param>
/// <param name="InheritedVariantIds">Row ids of the source keys inside the group hierarchy of the new scenario</param>
/// <param name="DroppedTokens">Tokens of the source keys outside it, whose rows stay behind</param>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record ScenarioCloneVariants(
    IReadOnlySet<Guid> CarriedTokens,
    IReadOnlyList<Guid> InheritedVariantIds,
    IReadOnlySet<Guid> DroppedTokens);
