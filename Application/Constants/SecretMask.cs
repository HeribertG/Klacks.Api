// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Placeholder returned to clients in place of stored secret values (passwords, client secrets).
/// When a write request carries this placeholder, the existing stored value is preserved instead
/// of being overwritten.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class SecretMask
{
    public const string Placeholder = "***";
}
