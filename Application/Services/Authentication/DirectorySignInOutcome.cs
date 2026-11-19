// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Authentication;

/// <summary>
/// Result of one directory provider in the sign-in loop: no matching entry or wrong password (try the next provider),
/// refused (locked out, stop), correct password without granted access, or granted.
/// </summary>
public enum DirectorySignInOutcome
{
    NoMatch = 0,
    Refused = 1,
    NotGranted = 2,
    Granted = 3
}
