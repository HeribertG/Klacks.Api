// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// One way a person's name can appear in text, collected while the vocabulary is built.
/// </summary>
/// <param name="Person">The person the variant belongs to</param>
/// <param name="Variant">The variant suffix, null for the full name</param>
/// <param name="Surface">The exact database string the variant restores to</param>
/// <param name="PrimaryKey">The surface folded with umlauts as ue, used to pick the representative of an ambiguous phrase</param>
public sealed record PseudonymNameCandidate(PseudonymPerson Person, string? Variant, string Surface, string PrimaryKey);
