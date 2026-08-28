// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answer to "start a learning run now": whether this caller started one, and if not, why. Reported
/// instead of the run's counters because a run takes minutes - the manual trigger returns as soon as the
/// run is under way, not when it is finished.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningRunTicket(bool Started, string? Reason)
{
    public static SkillLearningRunTicket Accepted() => new(true, null);

    public static SkillLearningRunTicket Refused(string reason) => new(false, reason);
}
