// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Timing and listing constants of the ERP import file ledger. The runner writes a heartbeat every
/// HeartbeatInterval or every HeartbeatEveryOrders processed orders, whichever comes first; an Importing
/// row whose last heartbeat is older than StaleAfter counts as interrupted. ProcessedListingCap is the
/// number of newest Completed/CompletedWithWarnings rows the drop-point listing returns. Completed and
/// CompletedWithWarnings files (stored object and row) and soft-deleted rows are removed RetentionPeriod after
/// they finished (or were deleted); one run removes at most RetentionBatchSize of them per drop point.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportFileTiming
{
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    public const int HeartbeatEveryOrders = 25;

    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

    public const int ProcessedListingCap = 200;

    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    public const int RetentionBatchSize = 200;
}
