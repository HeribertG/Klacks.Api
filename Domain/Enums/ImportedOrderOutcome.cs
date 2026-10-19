// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// What importing one ERP order did: Created a new draft shift, Updated an existing draft whose data
/// differed, left a draft or sealed order Unchanged (nothing written), Superseded a sealed order (closed
/// it and opened a replacement draft), or Rejected it (recorded as an erp_import_exception). Each outcome
/// feeds one counter of the erp_import_file ledger row.
/// </summary>
public enum ImportedOrderOutcome
{
    Created = 0,
    Updated = 1,
    Unchanged = 2,
    Superseded = 3,
    Rejected = 4
}
