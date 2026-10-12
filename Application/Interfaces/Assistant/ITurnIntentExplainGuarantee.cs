// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Turn-intent guarantee of the toolset: on a confident Ask in an enabled locale, the best-ranked retrieved
/// Explain skills. Returns no names unless the classifier runs in Active mode; Shadow only logs.
/// </summary>
public interface ITurnIntentExplainGuarantee
{
    Task<IReadOnlyList<string>> ResolveAsync(SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken);
}
