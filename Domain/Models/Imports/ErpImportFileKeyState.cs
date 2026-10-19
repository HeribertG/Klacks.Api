// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

/// <summary>
/// Storage key of an erp_import_file row and whether the row is soft-deleted; the projection the inbox
/// registration needs to tell new files, registered files and leftovers of deleted files apart.
/// </summary>
/// <param name="StorageKey">Storage key of the row</param>
/// <param name="IsDeleted">True when the row was soft-deleted</param>
public sealed record ErpImportFileKeyState(string StorageKey, bool IsDeleted);
