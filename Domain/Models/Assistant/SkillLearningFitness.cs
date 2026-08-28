// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Weekly usefulness snapshot of one activated learning candidate, the input of the pruning decision.
/// Nothing writes rows of this type before stage G3; the table exists in G1 so the fitness service can
/// be added without a second schema change.
/// </summary>
/// <param name="WindowStartUtc">Monday of the calendar week the counters belong to</param>
/// <param name="Recurrences">New cases in the originating cluster after the artefact went live</param>
/// <param name="Quote">(Successes + Helpful) / Uses, zero while Uses is zero</param>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillLearningFitness : BaseEntity
{
    public Guid CandidateId { get; set; }

    public DateTime WindowStartUtc { get; set; }

    public int Uses { get; set; }

    public int Successes { get; set; }

    public int Failures { get; set; }

    public int Helpful { get; set; }

    public int Corrections { get; set; }

    public int Recurrences { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public decimal Quote { get; set; }
}
