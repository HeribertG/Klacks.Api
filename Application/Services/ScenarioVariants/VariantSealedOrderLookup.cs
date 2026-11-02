// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds the world of a stored shift for the cut paths, which carry no token: only a shift that carries the token of an
/// Active, visible variant (key) yields it, in use or not. A shift without a token, with the token of a scenario clone or of a
/// hidden variant, or one that does not exist yields null; a consumed variant is refused by the guard (409). The caller answers
/// null like a missing order, and never queries the world-filtered rows without a token. This is a read: a caller that writes
/// (ResetCutsAsync) applies the write rule of the guard itself.
/// </summary>
/// <param name="shiftRepository">Reads the stored shift and the sealed order inside the variant</param>
/// <param name="variantShiftWriteGuard">Decides whether the token is a readable variant</param>
/// <param name="shiftId">Id of the shift the caller asked for: the sealed order, its plannable copy or a cut piece</param>
/// <param name="originalId">Id of the sealed order the caller asked for</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public static class VariantSealedOrderLookup
{
    public static async Task<Guid?> FindReadableVariantTokenAsync(
        IShiftRepository shiftRepository,
        IVariantShiftWriteGuard variantShiftWriteGuard,
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var stored = await shiftRepository.Get(shiftId);
        if (stored?.AnalyseToken is not { } token
            || !await variantShiftWriteGuard.IsReadableVariantAsync(token, cancellationToken))
        {
            return null;
        }

        return token;
    }

    public static async Task<Shift?> FindAsync(
        IShiftRepository shiftRepository,
        IVariantShiftWriteGuard variantShiftWriteGuard,
        Guid originalId,
        CancellationToken cancellationToken = default)
    {
        if (await FindReadableVariantTokenAsync(shiftRepository, variantShiftWriteGuard, originalId, cancellationToken) is not { } token)
        {
            return null;
        }

        return await shiftRepository.GetSealedOrder(originalId, token);
    }
}
