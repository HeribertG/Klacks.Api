// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule set for writing shifts in a variant (key). The token filter only sees a token the request carries, while
/// DELETE, a PUT without a token in its body and the skills carry none, so edit and delete decide from the stored token.
/// A stored token that is no live variant is not this guard's business: those calls return and ShiftRepository refuses them.
/// </summary>
/// <param name="EnsureCreatableAsync">Refuses a POST unless the token is an Active, visible variant no Active scenario carries and the shift has a customer, only groups inside the variant group and start and end inside the variant's period</param>
/// <param name="EnsureEditableAsync">Refuses a PUT of a variant shift under the same rules, and any status change, because LockedShiftEditPolicy skips token shifts; a change of a cut-relevant field (VariantShiftMirror) of the sealed order or its plannable copy is a ConflictException while the order has cuts, of a cut piece always</param>
/// <param name="EnsureOpenIfVariantAsync">False when the token belongs to no variant (a scenario clone); for a variant it refuses a hidden one (KeyNotFoundException with the text of a missing shift, naming shiftId), a consumed one and one an Active scenario carries (ConflictException) and returns true. Used by the writing cut paths, which carry no token in their request and decide from the stored one; it reads no content of a cut, ShiftCutFacade takes customer and groups from the hierarchy</param>
/// <param name="EnsureInsideVariantPeriodAsync">For a variant it refuses a range that starts before or ends after the period or has no end (BadRequestException); for every other token it does nothing. Used for the dates of a cut and of a reset, which do not pass EnsureEditableAsync</param>
/// <param name="IsReadableVariantAsync">The read door of the cut paths: true for a visible variant in or out of use; false for every other token and for a hidden variant, so the caller answers it exactly like a missing id; a consumed variant stays a ConflictException</param>
/// <param name="ResolveDeletableFamilyAsync">Null for a shift outside a variant; for a variant shift the ids of its whole family, after refusing a consumed or hidden variant, one an Active scenario carries and a family a scenario cloned</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IVariantShiftWriteGuard
{
    Task EnsureCreatableAsync(Guid variantToken, Shift shift, CancellationToken cancellationToken = default);

    Task EnsureEditableAsync(Shift stored, Shift incoming, CancellationToken cancellationToken = default);

    Task<bool> EnsureOpenIfVariantAsync(Guid token, Guid shiftId, CancellationToken cancellationToken = default);

    Task EnsureInsideVariantPeriodAsync(Guid token, IReadOnlyCollection<(DateOnly From, DateOnly? Until)> ranges, CancellationToken cancellationToken = default);

    Task<bool> IsReadableVariantAsync(Guid token, CancellationToken cancellationToken = default);

    Task<List<Guid>?> ResolveDeletableFamilyAsync(Shift stored, CancellationToken cancellationToken = default);
}
