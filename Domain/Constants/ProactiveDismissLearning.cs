// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Deterministic thresholds for learning from dismissed proactive messages: after
/// DismissStreakThreshold consecutive dismissals of the same trigger kind, Klacksy asks the user
/// once whether that kind should be muted. RecentReactionsTake bounds how many recent reactions
/// are loaded to count the streak.
/// </summary>
public static class ProactiveDismissLearning
{
    public const int DismissStreakThreshold = 3;

    public const int RecentReactionsTake = DismissStreakThreshold;
}
