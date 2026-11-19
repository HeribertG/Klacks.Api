// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A refused change of the directory approval list (or a provider that cannot serve it), answered with 400 and one of
/// the DirectoryAccountErrorCodes. Derives from InvalidRequestException, so BaseHandler rethrows it unchanged.
/// </summary>
/// <param name="errorCode">One of the DirectoryAccountErrorCodes</param>
/// <param name="message">English diagnostic message</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public sealed class DirectoryAccountAccessRejectedException : InvalidRequestException, ICodedRejection
{
    private const string AccountNotFoundMessage = "The directory account was not found or is disabled.";
    private const string AccountWithoutEmailMessage = "The directory account has no valid e-mail address.";
    private const string AccountEmailTakenMessage =
        "The e-mail address of the directory account is already used by a Klacks account. Delete that account or change its e-mail first.";
    private const string AccountHasRolesMessage =
        "The account linked to this directory entry holds roles and cannot be reactivated or relinked from the directory list.";
    private const string AccountNotLinkedMessage = "No Klacks account is linked to this directory entry.";
    private const string ProviderNotUsableMessage =
        "The identity provider is not an LDAP or Active Directory provider used for authentication.";

    public DirectoryAccountAccessRejectedException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();

    public static DirectoryAccountAccessRejectedException AccountNotFound() =>
        new(DirectoryAccountErrorCodes.AccountNotFound, AccountNotFoundMessage);

    public static DirectoryAccountAccessRejectedException AccountWithoutEmail() =>
        new(DirectoryAccountErrorCodes.AccountWithoutEmail, AccountWithoutEmailMessage);

    public static DirectoryAccountAccessRejectedException AccountEmailTaken() =>
        new(DirectoryAccountErrorCodes.AccountEmailTaken, AccountEmailTakenMessage);

    public static DirectoryAccountAccessRejectedException AccountHasRoles() =>
        new(DirectoryAccountErrorCodes.AccountHasRoles, AccountHasRolesMessage);

    public static DirectoryAccountAccessRejectedException AccountNotLinked() =>
        new(DirectoryAccountErrorCodes.AccountNotLinked, AccountNotLinkedMessage);

    public static DirectoryAccountAccessRejectedException ProviderNotUsableForLogin() =>
        new(DirectoryAccountErrorCodes.ProviderNotUsableForLogin, ProviderNotUsableMessage);
}
