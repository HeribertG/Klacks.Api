// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Shifts;

/// <summary>
/// Rejection texts for writes that would set the server-owned shift hierarchy (status, order reference, cut tree) from
/// the request: a new shift must start as an order, a draft order may only stay a draft or be sealed, a cut update may only
/// turn a plannable shift or a piece into a piece.
/// </summary>
public static class ShiftHierarchyMessages
{
    public const string PostStatusNotAllowed =
        "A new shift must be created as an order (status OriginalOrder or SealedOrder); status {0} is not allowed";

    public const string DraftStatusChangeNotAllowed =
        "A draft order may only be saved as a draft (OriginalOrder) or sealed (SealedOrder); shift {0} was sent with status {1}";

    public const string CutUpdateStatusNotAllowed =
        "A cut update may only turn a plannable shift or a cut piece into a cut piece; shift {0} has status {1} and was sent with status {2}";
}
