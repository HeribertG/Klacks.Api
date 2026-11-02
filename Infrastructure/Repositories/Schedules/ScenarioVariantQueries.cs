// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one definition of a live variant (key) row and of the live variants a scenario carries through live links, shared
/// by ScenarioVariantRepository, ShiftScenarioWriteGate, VariantShiftMirrorWriter, the clone source resolver, the accept
/// conflict validator, the variant promoter and the coverage calculator.
/// </summary>
/// <param name="context">Database context of the caller</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ScenarioVariantQueries
{
    public static IQueryable<AnalyseScenario> LiveVariants(DataBaseContext context)
    {
        return context.Set<AnalyseScenario>()
            .Where(v => !v.IsDeleted && v.Kind == AnalyseScenarioKind.Variant);
    }

    public static IQueryable<AnalyseScenario> LiveVariantsOfScenario(DataBaseContext context, Guid scenarioToken)
    {
        return
            from link in context.Set<AnalyseScenarioVariant>()
            join variant in LiveVariants(context) on link.VariantId equals variant.Id
            where !link.IsDeleted && link.ScenarioToken == scenarioToken
            select variant;
    }
}
