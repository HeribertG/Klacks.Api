// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// One address line that produces a street key: the address it belongs to, the line variant (null for Street) and the
/// stored street-and-number text it restores to.
/// </summary>
public sealed record PseudonymStreetCandidate(string EntityKey, string? Variant, string Surface);
