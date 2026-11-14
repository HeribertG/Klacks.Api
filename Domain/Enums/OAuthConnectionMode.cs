// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Whether external tools may connect through the built-in OAuth server. Trusted means only targets of the
/// trusted list; Off blocks the whole flow.
/// </summary>
public enum OAuthConnectionMode
{
    Trusted = 0,
    Off = 1
}
