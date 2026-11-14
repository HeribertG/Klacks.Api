// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Kind of trusted OAuth redirect target: a vendor of an AI tool from the shipped list, a program on the
/// same computer (loopback), or a target an administrator added.
/// </summary>
public enum OAuthRedirectClass
{
    Vendor = 0,
    Loopback = 1,
    Custom = 2
}
