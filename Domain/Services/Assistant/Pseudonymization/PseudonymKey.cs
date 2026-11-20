// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// The stable identity a pseudonym number is allocated for: an entity id or the id-based key of an ambiguous name
/// (stored), or a detected phone or e-mail value (only within one request, never stored).
/// </summary>
public readonly record struct PseudonymKey(PseudonymKind Kind, string Key);
