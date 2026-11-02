// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hands the variant (key) links of a source scenario on to the scenario built on it (scenario from scenario). It stages one
/// link per key ScenarioCloneSourceResolver.ResolveCloneVariantsAsync lets through: a key inside the group hierarchy of the
/// new scenario, the rule a direct selection has to meet (ScenarioVariantSelection), so no chain carries a key past it. The
/// shift and work cloners leave the rows of the other keys behind by the same answer, and the accept
/// (ScenarioAcceptConflictValidator) stays the defence against a clone of a key the scenario does not carry. Without a
/// source scenario, and for a new scenario without a group, there is nothing to stage. Everything is staged on the shared
/// context; the caller commits.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioVariantLinkCloner
{
    private readonly DataBaseContext _context;

    public ScenarioVariantLinkCloner(DataBaseContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Stages the links of the new scenario to the variants its source scenario carries.
    /// </summary>
    /// <param name="token">Token of the new scenario that receives the links</param>
    /// <param name="variants">The keys the new scenario inherits from its source</param>
    internal async Task CloneLinksAsync(Guid token, ScenarioCloneVariants variants, CancellationToken ct)
    {
        foreach (var variantId in variants.InheritedVariantIds)
        {
            await _context.Set<AnalyseScenarioVariant>().AddAsync(
                new AnalyseScenarioVariant { Id = Guid.NewGuid(), ScenarioToken = token, VariantId = variantId }, ct);
        }
    }
}
