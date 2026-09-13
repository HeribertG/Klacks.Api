// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deserialization shape of one entry of skill_selection_trajectories.knowledge_index_candidates_json,
/// written by TrajectoryCaptureService.SerializeCandidates. Score is not read back here; only the name,
/// the provenance and the offered rank decide what the correction menu shows.
/// </summary>

namespace Klacks.Api.Application.DTOs.Assistant;

public class TurnToolsetCandidate
{
    public string? Name { get; set; }

    public string? Source { get; set; }

    public int Rank { get; set; }
}
