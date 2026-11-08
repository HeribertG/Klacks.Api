// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires <see cref="Permissions.CanUseAssistant"/>. Every role holds it today, so the policy
    /// changes nothing for existing callers — it exists so that taking the assistant away from a role
    /// is a one-line change in the permission matrix instead of a new gate on every entry point.
    /// </summary>
    public const string RequireAssistant = "RequireAssistant";

    /// <summary>
    /// Requires <see cref="Permissions.CanManageAutomation"/>: the autonomy level and starting or approving
    /// multi-step agent plans. Mirrors the gate of the Klacksy skills so the REST endpoints cannot bypass it.
    /// </summary>
    public const string RequireAutomationManagement = "RequireAutomationManagement";

    /// <summary>
    /// Requires <see cref="Permissions.CanManageAccessTokens"/>: minting a personal access token.
    /// </summary>
    public const string RequireAccessTokenManagement = "RequireAccessTokenManagement";

    /// <summary>
    /// Requires <see cref="Permissions.CanEditSchedule"/>, the planning right: the normal autofill (AutoWizard start, status,
    /// cancel and its job hub). Mirrors the gate of the skills start_autowizard, list_open_wizard_jobs and cancel_wizard_job.
    /// </summary>
    public const string RequireScheduleEditing = "RequireScheduleEditing";
}
