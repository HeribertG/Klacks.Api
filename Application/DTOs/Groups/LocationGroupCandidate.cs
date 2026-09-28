// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One city bucket in a location-group-candidate evaluation: how many clients share this city as their
/// preferred address, and whether that count meets the minimum viable group size.
/// </summary>
/// <param name="City">City name as it appears on the clients' preferred address (trimmed)</param>
/// <param name="ClientCount">Number of clients whose preferred address city is this value</param>
/// <param name="IsViable">True when ClientCount meets GroupingAdvisoryDefaults.MinViableGroupSize</param>
public sealed record LocationGroupCandidate(
    string City,
    int ClientCount,
    bool IsViable);
