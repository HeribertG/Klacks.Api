// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Whose rights one remediation run borrows, and where that permission came from. The action dispatcher
/// has two sources for it and they are not interchangeable: an approval stamped on the condition row (an
/// approval chain acknowledgement or a planner's delegation) and an administrator's standing approval for
/// the whole kind and scope. Everything downstream of the decision - the claim's audit detail, the ledger
/// event and the wording of the mandatory report - has to be able to tell them apart, and passing one
/// value instead of three loose parameters is what keeps that from being forgotten in one of the places.
/// </summary>
/// <param name="UserId">The human whose rights the run borrows; the acting name stays Klacksy's.</param>
/// <param name="Identity">Already-resolved identity, when the caller had to resolve it before deciding; null lets the executing path resolve it itself.</param>
/// <param name="Grant">The standing approval this run happens under, or null when a human approved this very finding.</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed record ConditionExecutionAuthority(
    Guid UserId,
    ProactiveActionIdentity? Identity = null,
    StandingApproval? Grant = null);
