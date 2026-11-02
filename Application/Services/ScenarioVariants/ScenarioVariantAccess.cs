// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Access rules for a variant (key). A variant of a hidden group or a missing id is answered with the not-found exception
/// of a missing scenario; a variant that is no longer Active (consumed by an accepted scenario) cannot be changed or deleted
/// and is answered with a conflict. A variant an Active scenario carries, through an inherited link too, is frozen: what the
/// scenario planned must still fit when it is accepted, so every write is a conflict until those scenarios are discarded.
/// EnsureWritableAsync is the one place of both conflicts, for the row id handlers and for IVariantShiftWriteGuard. The
/// child-count check at the accept stays behind it as the defence against a change made past this rule.
/// </summary>
/// <param name="repository">Loads the tracked variant by row id</param>
/// <param name="guard">Decides whether the caller may see the variant's group</param>
/// <param name="id">Row id of the variant</param>
/// <param name="variant">The loaded variant</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public static class ScenarioVariantAccess
{
    public const string ConsumedMessage = "The variant has already been consumed by an accepted scenario and can no longer be changed or deleted";

    public const string InUseMessage = "The variant is carried by an active scenario and cannot be changed or deleted; discard those scenarios first";

    public static async Task<AnalyseScenario?> FindVisibleAsync(
        IScenarioVariantRepository repository, IGroupVisibilityGuard guard, Guid id, CancellationToken cancellationToken)
    {
        var variant = await repository.GetVariantAsync(id, cancellationToken);

        return await guard.IsScenarioVisibleAsync(variant, cancellationToken) ? variant : null;
    }

    public static async Task<AnalyseScenario> LoadEditableAsync(
        IScenarioVariantRepository repository, IGroupVisibilityGuard guard, Guid id, CancellationToken cancellationToken)
    {
        var variant = await FindVisibleAsync(repository, guard, id, cancellationToken)
            ?? throw new KeyNotFoundException(string.Format(ScenarioGroupVisibility.ScenarioNotFoundFormat, id));

        await EnsureWritableAsync(repository, variant, cancellationToken);

        return variant;
    }

    public static void EnsureNotConsumed(AnalyseScenario variant)
    {
        if (variant.Status != AnalyseScenarioStatus.Active)
        {
            throw new ScenarioVariantConflictException(ConsumedMessage, ScenarioVariantErrorCodes.Consumed);
        }
    }

    public static async Task EnsureWritableAsync(
        IScenarioVariantRepository repository, AnalyseScenario variant, CancellationToken cancellationToken)
    {
        EnsureNotConsumed(variant);

        if (await repository.HasActiveScenarioAsync(variant.Id, cancellationToken))
        {
            throw new ScenarioVariantConflictException(InUseMessage, ScenarioVariantErrorCodes.InUse);
        }
    }
}
