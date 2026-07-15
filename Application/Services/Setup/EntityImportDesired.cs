// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One row a region-setup entity-import section wants to exist, as computed from the profile file
/// before any database access.
/// </summary>
/// <param name="SourceKey">Deterministic natural key, e.g. "region-setup:compliance.periodCaps:month:totalHours"</param>
/// <param name="ContentHash">Hash of the row's editable value fields, computed by the caller (e.g. via ImportContentHasher)</param>
/// <param name="Values">The value payload to write on Insert/Update</param>

namespace Klacks.Api.Application.Services.Setup;

public sealed record EntityImportDesired<TValues>(string SourceKey, string ContentHash, TValues Values);
