// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The street lines of one client address as the pseudonym dictionary reads them from the database. Postcode, town,
/// state and country are not part of it: they stay readable for the model.
/// </summary>
/// <param name="EntityKey">The address id, the stable key of the pseudonym</param>

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed record PseudonymAddress(
    string EntityKey,
    string? Street,
    string? Street2,
    string? Street3,
    string? AddressLine1,
    string? AddressLine2);
