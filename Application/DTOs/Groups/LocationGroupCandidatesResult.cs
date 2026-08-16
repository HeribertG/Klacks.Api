// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates which cities have enough clients (of a given entity type) to justify creating a new
/// location group, so Klacksy can recommend a concrete create_group call before the user asks for one
/// blindly. Counts follow the exact same "preferred address" and "city name" rules as
/// CustomerGroupingPlanner, so a recommended city is guaranteed to be picked up by the next
/// propose_grouping run once the group exists. Cities that already match an existing group name are
/// excluded from the candidates entirely — proposing a duplicate name would break the uniqueness
/// precondition CustomerGroupingPlanner relies on for its name match. Creating a group and assigning
/// clients to it both stay manual, separate steps (create_group, then propose_grouping/apply_grouping).
/// </summary>
/// <param name="EntityType">Client population this evaluation covers</param>
/// <param name="Candidates">Cities without an existing matching group, at or above the minimum viable size</param>
/// <param name="NearThresholdCandidates">Cities without an existing matching group, below the minimum viable size — shown, not hidden</param>
/// <param name="ClientsWithoutUsableAddress">Clients with no address carrying a city value</param>
/// <param name="ClientsInExistingLocationGroup">Clients whose city already matches an existing group name — informational, these are not part of Candidates</param>
/// <param name="Recommendation">Conservative, factual summary — never creates a group itself</param>
public sealed record LocationGroupCandidatesResult(
    string EntityType,
    IReadOnlyList<LocationGroupCandidate> Candidates,
    IReadOnlyList<LocationGroupCandidate> NearThresholdCandidates,
    int ClientsWithoutUsableAddress,
    int ClientsInExistingLocationGroup,
    string Recommendation);
