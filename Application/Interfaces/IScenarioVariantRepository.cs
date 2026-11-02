// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository interface for variants (keys): analyse_scenarios rows of kind Variant and their links to the scenarios
/// that carry them. Every variant read returns kind Variant only and leaves soft-deleted rows out. Stage-only: the
/// methods that write only stage changes, the caller commits them through IUnitOfWork.
/// </summary>
/// <param name="GetVariantAsync">Tracked variant by row id, or null when the id is no live variant</param>
/// <param name="ListByGroupsAsync">Variants of the given groups, consumed ones included, ordered by name then id</param>
/// <param name="AddLinksAsync">Stages the missing scenario/variant links; nothing is saved</param>
/// <param name="GetVariantIdsAsync">Ids of the live variants a scenario carries through live links</param>
/// <param name="GetVariantIdsByScenarioTokensAsync">Ids of the live variants per scenario token, in one query; a token without live variants has no entry</param>
/// <param name="GetVariantsOfScenarioAsync">The live variants a scenario carries through live links</param>
/// <param name="GetVariantsByIdsAsync">The live variants among the given ids</param>
/// <param name="HasActiveScenarioAsync">Whether a live, Active scenario carries the variant through a live link</param>
/// <param name="CountShiftsByVariantTokenAsync">Live services (SealedOrder shifts, not their OriginalShift or SplitShift copies) per variant token, zero for tokens without services</param>
/// <param name="GetShiftIdsByVariantTokensAsync">Ids of every live shift row carrying one of the variant tokens: sealed orders, plannable copies and cut pieces</param>
/// <param name="GetSealedShiftsAsync">Live services (SealedOrder shifts) of one variant token with client, groups, expenses and qualifications loaded, ordered by name then id; untracked</param>
/// <param name="GetShiftFamilyIdsAsync">Ids of the live rows of one order inside a variant: the sealed order and every row whose OriginalId points at it</param>
/// <param name="HasCutsAsync">Whether the order of the sealed order id has live SplitShift rows inside the variant token</param>
/// <param name="HasShiftsOutsidePeriodAsync">Whether a live row of the variant, of any status, starts before or ends after the period or has no end</param>
/// <param name="HasScenarioClonesAsync">Whether a live scenario clone points at one of the shifts through ScenarioSourceShiftId</param>
/// <param name="SoftDeleteFinishedScenarioLinksAsync">Stages the soft-delete of the live links between a variant and every scenario that is not an Active live scenario; nothing is saved</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScenarioVariantRepository
{
    Task<AnalyseScenario?> GetVariantAsync(Guid id, CancellationToken ct = default);

    Task<List<AnalyseScenario>> ListByGroupsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default);

    Task AddLinksAsync(Guid scenarioToken, IReadOnlyCollection<Guid> variantIds, CancellationToken ct = default);

    Task<List<Guid>> GetVariantIdsAsync(Guid scenarioToken, CancellationToken ct = default);

    Task<Dictionary<Guid, List<Guid>>> GetVariantIdsByScenarioTokensAsync(IReadOnlyCollection<Guid> scenarioTokens, CancellationToken ct = default);

    Task<List<AnalyseScenario>> GetVariantsOfScenarioAsync(Guid scenarioToken, CancellationToken ct = default);

    Task<List<AnalyseScenario>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<bool> HasActiveScenarioAsync(Guid variantId, CancellationToken ct = default);

    Task<Dictionary<Guid, int>> CountShiftsByVariantTokenAsync(IReadOnlyCollection<Guid> tokens, CancellationToken ct = default);

    Task<List<Guid>> GetShiftIdsByVariantTokensAsync(IReadOnlyCollection<Guid> variantTokens, CancellationToken ct = default);

    Task<List<Shift>> GetSealedShiftsAsync(Guid variantToken, CancellationToken ct = default);

    Task<List<Guid>> GetShiftFamilyIdsAsync(Guid variantToken, Guid familyRootId, CancellationToken ct = default);

    Task<bool> HasCutsAsync(Guid variantToken, Guid sealedOrderId, CancellationToken ct = default);

    Task<bool> HasShiftsOutsidePeriodAsync(Guid variantToken, DateOnly periodFrom, DateOnly periodUntil, CancellationToken ct = default);

    Task<bool> HasScenarioClonesAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default);

    Task SoftDeleteFinishedScenarioLinksAsync(Guid variantId, CancellationToken ct = default);
}
