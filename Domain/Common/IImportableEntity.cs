// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks an entity row that can be created and re-applied by the region-setup entity-import mechanism
/// (K20). ImportSourceKey is the deterministic natural key identifying which import produced the row
/// (e.g. "region-setup:compliance.periodCaps:month:totalHours"); ImportContentHash is the hash of the
/// row's editable value fields as of the last successful import. A re-import compares the row's CURRENT
/// content hash against the hash recomputed from today's file: unchanged means the row is still exactly
/// what the last import wrote and may be updated again; a mismatch means the customer edited the row
/// since, so the re-import must skip it instead of overwriting the edit.
/// </summary>

namespace Klacks.Api.Domain.Common;

public interface IImportableEntity
{
    string ImportSourceKey { get; set; }

    string ImportContentHash { get; set; }
}
