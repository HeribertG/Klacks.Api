// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One skill the keyword guarantee selected, as SkillMatchingEngine.TopKeywordMatches returns it.
/// </summary>
/// <param name="Name">The skill name</param>
/// <param name="IsBroadOnly">True when every term the skill matched also matched more skills than the guarantee
/// cap holds, so the match cannot tell this skill apart from its neighbours</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record KeywordSkillMatch(string Name, bool IsBroadOnly);
