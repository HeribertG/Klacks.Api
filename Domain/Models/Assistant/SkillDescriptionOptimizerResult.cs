// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What one call to the optimizer did. Generated counts the proposals actually written; Attempts counts every
/// wrongly-chosen skill for which the optimizer asked the model for a suggestion (skipped skills - not found,
/// or already carrying an open proposal - are never attempts); Failures counts the attempts whose answer the
/// optimizer could not use (provider error, empty content, or unparseable/rejected JSON). A model that
/// correctly kept the current description is an attempt but never a failure.
/// </summary>
/// <param name="Generated">Proposals written to the repository</param>
/// <param name="Attempts">Suggestion requests actually sent to the model</param>
/// <param name="Failures">Attempts whose answer could not be used</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillDescriptionOptimizerResult(int Generated, int Attempts, int Failures)
{
    public static SkillDescriptionOptimizerResult Empty { get; } = new(0, 0, 0);
}
