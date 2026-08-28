// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A frozen "this utterance must route to that skill or recipe" expectation, kept so a later learning
/// round cannot silently break what an earlier one fixed. Deliberately holds no user id: the excerpt is
/// evidence about the product, not about a person. Survives its cluster, which is why the foreign key
/// sets null rather than cascading.
/// </summary>
/// <param name="Query">The utterance excerpt, at most 120 characters</param>
/// <param name="ExpectedSourceId">Name of the skill or recipe the query must retrieve</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillLearningGoldenCase : BaseEntity
{
    public string Query { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string ExpectedSourceId { get; set; } = string.Empty;

    public Guid? ClusterId { get; set; }
}
