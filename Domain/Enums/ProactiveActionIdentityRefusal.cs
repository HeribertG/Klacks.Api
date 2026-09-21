// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Why a proactive action could not be given an identity to act under. Kept apart from a plain reason
/// string because the cases call for different responses: a missing approver means the row was handed
/// to execution without anybody having released it, a refused token is usually temporary (a locked or
/// deactivated account) and the run should be retried, a policy refusal is permanent for that skill
/// until it is reclassified, and missing permissions mean the approver lost a role since they
/// acknowledged - the approval has to be asked again from somebody who holds it.
/// </summary>
public enum ProactiveActionIdentityRefusal
{
    None = 0,
    NoApprover = 1,
    TokenRefused = 2,
    PolicyRefused = 3,
    PermissionsMissing = 4
}
