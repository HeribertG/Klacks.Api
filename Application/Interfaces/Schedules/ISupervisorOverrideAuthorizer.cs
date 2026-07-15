// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// K1 supervisor override authorization: whether the current caller may override a Block-mode
/// compliance escalation right now. Never authorizes a structural error (collision, missing
/// mandatory qualification) — callers must gate those separately; this only answers "is an
/// override in principle allowed for this request", independent of any particular conflict.
/// </summary>
public interface ISupervisorOverrideAuthorizer
{
    /// <summary>
    /// True when <paramref name="overrideBlockRequested"/> is set, the caller holds the Admin or
    /// Authorised role, and the COMPLIANCE_ENFORCEMENT_ALLOW_SUPERVISOR_OVERRIDE setting allows it.
    /// </summary>
    Task<bool> IsAuthorizedAsync(bool overrideBlockRequested);
}
