// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// How deep Klacksy explains for the current user: the experience level and whether it stems from the user's own
/// persisted preference (explicit override) rather than the usage heuristic or the beginner default.
/// </summary>
/// <param name="Level">Experience level the explanation depth is chosen for</param>
/// <param name="IsExplicitOverride">True when the user set the level themselves; it then wins over the heuristic</param>
public sealed record ExplainDepthProfile(ExplainExperienceLevel Level, bool IsExplicitOverride);
