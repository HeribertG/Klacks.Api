// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The latest payroll export of one person for a period and format: the hash of what was exported and its revision.
/// @param ClientId - The exported person
/// @param ContentHash - SHA-256 (lowercase hex) over the exported day entries
/// @param Revision - 1-based revision counter of this export; the next export uses Revision + 1
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public record LatestExportedItem(Guid ClientId, string ContentHash, int Revision);
