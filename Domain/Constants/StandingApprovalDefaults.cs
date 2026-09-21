// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The limits of a standing approval (Owner decisions 2026-09-21). Code constants rather than settings,
/// for the same reason AgentConditionActionDefaults is: an administrator already chooses the duration and
/// the budget of each individual grant, and these are the backstops that stay in force no matter what is
/// asked for. There is deliberately no "unlimited" value for either.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class StandingApprovalDefaults
{
    /// <summary>
    /// Multiple of <see cref="DefaultDailyBudget"/> a single grant may be raised to. Kept as a factor so
    /// the two numbers cannot drift apart, and deliberately not derived from the governance budget: that
    /// one is the outer cap and is configured per kind, while this is the ceiling of what an administrator
    /// may hand to the automation in one grant.
    /// </summary>
    private const int MaximumDailyBudgetFactor = 5;

    /// <summary>Duration a grant gets when the request names none.</summary>
    public const int DefaultDurationDays = 30;

    /// <summary>Longest duration a grant may be asked for; a longer request is refused, never truncated.</summary>
    public const int MaximumDurationDays = 90;

    /// <summary>Shortest duration a grant may be asked for - a grant that expires the same instant is a no-op.</summary>
    public const int MinimumDurationDays = 1;

    /// <summary>Daily action budget a grant gets when the request names none.</summary>
    public const int DefaultDailyBudget = 20;

    /// <summary>
    /// Smallest budget a grant may carry. Zero is refused rather than stored: a grant that can never act
    /// would report as active autonomy while executing nothing, which is worse than no grant at all.
    /// </summary>
    public const int MinimumDailyBudget = 1;

    /// <summary>Largest budget a grant may carry, see <see cref="MaximumDailyBudgetFactor"/>.</summary>
    public const int MaximumDailyBudget = DefaultDailyBudget * MaximumDailyBudgetFactor;

    /// <summary>Column length of the trigger kind, matching AgentTriggerGovernanceConfiguration.</summary>
    public const int TriggerKindMaxLength = 64;
}
