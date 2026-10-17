// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Shifts;

/// <summary>
/// Not-found texts the shift write paths share, so a hidden shift or group is answered with exactly the text a
/// missing one gets.
/// </summary>
public static class ShiftVisibilityMessages
{
    public const string ShiftNotFound = "Shift with ID {0} not found";

    public const string ParentShiftNotFound = "Parent shift with ID {0} not found";

    public const string SealedOrderNotFound = "SealedOrder with OriginalId {0} not found";

    public const string NoSealedOrderFound = "No SealedOrder found for OriginalId {0}";

    public const string GroupNotFound = "Group with ID {0} not found";
}
