// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

/// <summary>
/// What a matched name phrase becomes on the line: the token text, the base token for the legend and the surface
/// it restores to.
/// </summary>
public sealed record PseudonymPhrase(string Token, string BaseToken, string Surface);
