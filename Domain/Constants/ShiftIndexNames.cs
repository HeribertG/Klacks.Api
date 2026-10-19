// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of database indexes on the shift table that application code recognises when a write violates them.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ShiftIndexNames
{
    public const string ExternalOrderReferenceLookup = "ix_shift_source_system_id_external_order_reference";

    public const string OpenDraftReferenceUnique = "ux_shift_open_draft_source_system_id_external_order_reference";
}
