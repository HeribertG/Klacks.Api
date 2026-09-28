// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks a controller or action as reachable by the seeded admin account even while the own-admin
/// setup gate is active - session self-service only (login/logout/status), never business data.
/// </summary>

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ExemptFromAdminSetupGateAttribute : Attribute
{
}
