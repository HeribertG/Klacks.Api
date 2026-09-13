// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Boundary applied by AgentMemoryRepository.FindDuplicateAsync before it loads a memory's key and
/// content into memory for normalization. A single extracted fact is normally far shorter than this
/// (AutoMemoryExtractionService caps the whole extraction call at 512 output tokens for up to three
/// facts), so excluding longer stored rows in SQL keeps the scan cheap. It is a cost boundary, not a
/// correctness one: a stored row above the limit is never offered as a duplicate, so at worst the same
/// fact is written a second time and deduplicated later by the embedding-similarity check.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentMemoryDedupeLimits
{
    public const int MaxCandidateContentLength = 1000;
}
