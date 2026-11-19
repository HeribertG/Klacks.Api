// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Machine codes of directory login and approval refusals; the Ui maps each to a translated text.
/// </summary>
public static class DirectoryAccountErrorCodes
{
    public const string AccessNotGranted = "directoryAccessNotGranted";

    public const string AccountNotFound = "directoryAccountNotFound";

    public const string AccountWithoutEmail = "directoryAccountWithoutEmail";

    public const string AccountEmailTaken = "directoryAccountEmailTaken";

    public const string ProviderNotUsableForLogin = "directoryProviderNotUsableForLogin";

    public const string AccountHasRoles = "directoryAccountHasRoles";

    public const string AccountNotLinked = "directoryAccountNotLinked";
}
