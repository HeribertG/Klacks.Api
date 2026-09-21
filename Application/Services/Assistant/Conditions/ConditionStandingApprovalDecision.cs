// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the standing-approval gate decided about one candidate row. Three outcomes, and the third is the
/// reason this is not a nullable authority: "execute under this grant", "no usable grant applies, ask a
/// human as before", and "leave this row alone this tick" - the last one happens when somebody approved
/// the finding in the very moment the gate was applying the grant, so the row already carries an approval
/// and asking for a second one would wake a roster for nothing.
/// </summary>
/// <param name="AskForApproval">True only for the fall-back-to-the-chain outcome.</param>
/// <param name="Authority">The authority to execute under, or null when there is nothing to execute now.</param>

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed record ConditionStandingApprovalDecision(
    bool AskForApproval,
    ConditionExecutionAuthority? Authority)
{
    public static readonly ConditionStandingApprovalDecision NotCovered = new(true, null);

    public static readonly ConditionStandingApprovalDecision LeaveAlone = new(false, null);

    public static ConditionStandingApprovalDecision Execute(ConditionExecutionAuthority authority) =>
        new(false, authority);
}
