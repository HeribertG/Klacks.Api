// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the deterministic skill guarantees for one turn - the skills that must be in the toolset
/// independent of retrieval quality (page/concept explain, workflow pairs, recipe steps, grouping intent,
/// proposal confirmation, keyword and learned-phrase matches, plan candidacy, planning-profile draft,
/// pending notes, and clarification pins).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface ISkillToolsetGuaranteeResolver
{
    Task<SkillToolsetGuaranteeResult> ResolveAsync(
        SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken);
}
