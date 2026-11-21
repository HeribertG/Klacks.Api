// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// The street-and-number part of a stored address line: its folded keys (one per fold mode) and the exact stored text
/// of that part, which a pseudonym restores to.
/// </summary>
public sealed record PseudonymStreetLine(IReadOnlyList<string> Keys, string Surface, string HouseNumber);
