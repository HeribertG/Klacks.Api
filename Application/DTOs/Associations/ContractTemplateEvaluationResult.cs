// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of comparing the active contract templates with the administrator's wishes. Facts and the
/// recommendation are computed deterministically; nothing here is created or changed.
/// </summary>
/// <param name="ActiveTemplateCount">Number of active contract templates that were compared</param>
/// <param name="WishCount">Number of wished fields the administrator gave</param>
/// <param name="Candidates">The closest templates, fewest differences first</param>
/// <param name="Warnings">Facts the caller must not overlook, such as an unknown region</param>
/// <param name="Recommendation">Conservative, factual summary of the best match</param>
namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateEvaluationResult(
    int ActiveTemplateCount,
    int WishCount,
    IReadOnlyList<ContractTemplateCandidate> Candidates,
    IReadOnlyList<string> Warnings,
    string Recommendation);
