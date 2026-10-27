// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Diagnostic record of where a skill can be lost on its way into a turn's toolset; read by the turn eval only.
/// </summary>
/// <param name="RetrievedNames">Skills the knowledge index surfaced, best score first, after the correction exclusion and before the cap</param>
/// <param name="PreCapNames">The whole selection (always-on, retrieved, guaranteed, expanded) before the provider cap was applied</param>
/// <param name="TruncatedNames">Skills of the selection that the provider cap removed</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillToolsetTrace(
    IReadOnlyList<string> RetrievedNames,
    IReadOnlyList<string> PreCapNames,
    IReadOnlyList<string> TruncatedNames);
