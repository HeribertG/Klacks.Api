// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits of a stored confirmation binding. MaxStoredItems equals the per-call cap of the bulk Work deletion
/// (BulkWorkDeletionDefaults.MaxWorksPerCall): up to that many items are stored next to the fingerprint so a refusal can
/// say how the list changed; a larger set keeps only the fingerprint.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ConfirmationBindingDefaults
{
    public const int MaxStoredItems = 50;
}
