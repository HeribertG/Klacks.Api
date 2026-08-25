// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Why a proactive action could not be given an identity to act under. Kept apart from a plain reason
/// string because the three cases call for different responses: a missing owner is a governance gap an
/// administrator must close, a refused token is usually temporary (a locked or deactivated account) and
/// the run should be retried, and a policy refusal is permanent for that skill until it is reclassified.
/// </summary>
public enum ProactiveActionIdentityRefusal
{
    None = 0,
    NoResponsibleOwner = 1,
    TokenRefused = 2,
    PolicyRefused = 3
}
