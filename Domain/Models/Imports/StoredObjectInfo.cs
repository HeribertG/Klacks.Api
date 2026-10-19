// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

/// <summary>
/// Size, SHA-256 and (when the storage reports it) last write time of a stored object's raw bytes.
/// </summary>
/// <param name="SizeBytes">Number of raw bytes</param>
/// <param name="Sha256">Lower-case hex SHA-256 of the raw bytes</param>
/// <param name="LastModifiedUtc">Last write time of the object (UTC), or null when the producer does not report it</param>
public sealed record StoredObjectInfo(long SizeBytes, string Sha256, DateTime? LastModifiedUtc = null);
