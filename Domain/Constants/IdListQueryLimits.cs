// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Limits of the reads that filter by a list of ids: a longer list is split into chunks of ChunkSize ids, one query
/// per chunk, so a single query never carries an unbounded id parameter.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class IdListQueryLimits
{
    public const int ChunkSize = 2000;
}
