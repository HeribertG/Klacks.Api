// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the emergent memory-relationship graph (P5). NeighbourMinConfidence is the floor used
/// by retrieval expansion (NeighboursOfAsync) — both creation confidences below MUST stay at or above
/// it, otherwise an edge is built by the population job but silently never surfaces in expansion.
/// SharedTagsBaseConfidence and VectorSimilarityThreshold are chosen with headroom above
/// NeighbourMinConfidence for exactly that reason.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.MemoryGraph;

public static class MemoryGraphConstants
{
    public const string SharedTagsProvenance = "shared-tags";
    public const string VectorSimilarityProvenance = "vector-similarity";

    public const int MinSharedTags = 1;
    public const double SharedTagsBaseConfidence = 0.65;
    public const double SharedTagsConfidencePerExtraTag = 0.05;
    public const double MaxSharedTagsConfidence = 0.9;

    public const double VectorSimilarityThreshold = 0.85;

    // Retrieval-side floor: both creation confidences above are >= this by construction.
    public const double NeighbourMinConfidence = 0.6;

    public const int MaxEdgesPerMemory = 5;
    public const int MaxExpansionSlots = 5;
    public const int MaxMemoriesPerBuildRun = 50;
}
