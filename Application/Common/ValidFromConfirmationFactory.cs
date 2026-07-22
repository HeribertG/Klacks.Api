// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the confirmation SkillResult used when a membership ValidFrom date fails the plausibility
/// check. It stores the original invocation plus an override flag under a one-time token and instructs
/// the model to redeem the token via confirm_pending_action only after the user confirmed the date.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Common;

public static class ValidFromConfirmationFactory
{
    /// <summary>
    /// Creates a pending confirmation for the given skill call and returns the confirmation result.
    /// </summary>
    /// <param name="store">Store that mints the one-time confirmation token.</param>
    /// <param name="userId">User the token is bound to.</param>
    /// <param name="skillName">Name of the skill to replay once the user confirms.</param>
    /// <param name="originalParameters">The original invocation parameters to replay unchanged.</param>
    /// <param name="reason">English explanation of why the date is implausible.</param>
    public static SkillResult RequireConfirmation(
        IPendingConfirmationStore store,
        Guid userId,
        string skillName,
        IReadOnlyDictionary<string, object> originalParameters,
        string reason)
    {
        var pendingParameters = new Dictionary<string, object>(originalParameters)
        {
            [MembershipPlausibilityDefaults.ValidFromConfirmedParameter] =
                MembershipPlausibilityDefaults.ConfirmedValue
        };

        var token = store.Create(userId, skillName, pendingParameters);

        var message =
            $"Plausibility check: {reason} ValidFrom is the plannability boundary in the schedule — if it is a " +
            $"typo the employee becomes invisible for scheduling. The action is stored and NOT executed yet. Ask " +
            $"the user to explicitly confirm this start date; only after they confirmed in their own words, call " +
            $"'{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
            $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. Never confirm on your own.";

        return SkillResult.Confirmation(message, token, new { skillName, reason });
    }
}
