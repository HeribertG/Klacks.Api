// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// A known street address found in a text: position and length of the matched span and what it becomes.
/// </summary>
public readonly record struct PseudonymStreetMatch(int Index, int Length, PseudonymPhrase Phrase);
