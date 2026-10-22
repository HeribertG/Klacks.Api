// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Common;

/// <summary>
/// Whether group visibility limits the calling user at all, by the same rule the client list applies (the
/// Infrastructure-only IClientGroupFilterService).
/// </summary>
public interface ICallerGroupRestriction
{
    /// <summary>
    /// True when group visibility does not limit the caller at all: an admin, an installation without groups,
    /// or work without a calling user (neither an HTTP user nor an execution principal).
    /// </summary>
    Task<bool> IsCallerUnrestrictedAsync();
}
