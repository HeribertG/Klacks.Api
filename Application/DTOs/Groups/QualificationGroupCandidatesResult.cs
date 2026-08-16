// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates which qualifications have enough clients (of a given entity type) to justify creating a
/// new qualification group, so Klacksy can recommend a concrete create_group call before the user asks
/// for one blindly. Only qualifications valid today (company clock) count, matching
/// fill_group_by_criteria's own qualificationValidityDate semantics. Unlike location grouping,
/// qualifications OVERLAP — a client can hold several — so bucket counts can sum to more than
/// TotalClientsEvaluated; this is not a partition. A qualification whose display name already matches
/// an existing group name is listed under QualificationsAlreadyCovered instead of the candidates, since
/// a group for it plausibly already exists. Creating a group (create_group) and filling it
/// (fill_group_by_criteria) both stay manual, separate steps.
/// </summary>
/// <param name="EntityType">Client population this evaluation covers</param>
/// <param name="TotalClientsEvaluated">Total clients of this entity type considered</param>
/// <param name="Candidates">Qualifications without an existing matching group, at or above the minimum viable size</param>
/// <param name="NearThresholdCandidates">Qualifications without an existing matching group, below the minimum viable size — shown, not hidden</param>
/// <param name="ClientsWithoutValidQualification">Clients with no qualification valid today</param>
/// <param name="QualificationsAlreadyCovered">Display names of qualifications whose name already matches an existing group — informational</param>
/// <param name="Recommendation">Conservative, factual summary — never creates a group itself</param>
public sealed record QualificationGroupCandidatesResult(
    string EntityType,
    int TotalClientsEvaluated,
    IReadOnlyList<QualificationGroupCandidate> Candidates,
    IReadOnlyList<QualificationGroupCandidate> NearThresholdCandidates,
    int ClientsWithoutValidQualification,
    IReadOnlyList<string> QualificationsAlreadyCovered,
    string Recommendation);
