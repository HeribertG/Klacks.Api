// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Authentication;

/// <summary>
/// Outcome kinds of resolving one system user account from an id or a name.
/// </summary>
public enum SystemUserResolutionStatus
{
    Resolved = 0,
    MissingInput = 1,
    NotFoundById = 2,
    NotFoundByName = 3,
    Ambiguous = 4,
    LoadFailed = 5
}
