// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The directory accepted the password, but no administrator granted that directory account access to Klacks (no
/// linked account, or the linked account is deactivated). ErrorHandlingMiddleware answers 401 with errorCode
/// directoryAccessNotGranted, so the login form can say "no access, contact your administrator".
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public sealed class DirectoryAccessNotGrantedException : UnauthorizedException, ICodedRejection
{
    public const string DefaultMessage =
        "The directory accepted the sign-in, but this account has not been granted access. Contact your administrator.";

    public DirectoryAccessNotGrantedException()
        : base(DefaultMessage)
    {
    }

    public string ErrorCode => DirectoryAccountErrorCodes.AccessNotGranted;

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
