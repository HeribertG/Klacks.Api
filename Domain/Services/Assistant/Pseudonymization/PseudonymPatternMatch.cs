// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// A value found by pattern (e-mail, phone) and its position in the text.
/// The value is the exact text, so the pseudonym restores to the same spelling.
/// </summary>
public readonly record struct PseudonymPatternMatch(PseudonymKind Kind, int Index, int Length, string Value);
