// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

/// <summary>
/// Single candidate in a retrieval result with its rerank score.
/// </summary>
/// <param name="Entry">The matched knowledge entry.</param>
/// <param name="Score">Normalized rerank score between 0 and 1.</param>
public sealed record RetrievalCandidate(KnowledgeEntry Entry, double Score);
