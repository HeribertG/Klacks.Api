// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Lifecycle state of a delivered ERP order file in the erp_import_file ledger. Legal transitions:
/// Received -> Importing (claim) -> Completed | CompletedWithWarnings | Failed (finish);
/// Importing -> Interrupted (stale heartbeat reaped, or finished by the importing process after an unexpected
/// error); Failed | Interrupted -> Received (retry reset).
/// Every transition is a conditional update (WHERE status = expected), so two API instances can never
/// both win the same transition.
/// </summary>
public enum ErpImportFileStatus
{
    Received = 0,
    Importing = 1,
    Completed = 2,
    CompletedWithWarnings = 3,
    Failed = 4,
    Interrupted = 5
}
