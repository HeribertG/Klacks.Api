// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects an account registration from a UI form (own-admin setup, user administration) with machine-readable
/// reasons the UI translates. ErrorHandlingMiddleware answers it with 400, errorCode
/// AccountRegistrationErrorCodes.ErrorCode and the reasons array. Derives from InvalidRequestException so
/// BaseHandler rethrows it unchanged, like ClientImportRejectedException.
/// </summary>
/// <param name="reasons">ASP.NET Identity error codes (AccountRegistrationErrorCodes); unknown Identity codes pass through unchanged</param>
/// <param name="message">English summary of the reasons; it reaches the client</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Application.Exceptions;

public class AccountRegistrationRejectedException : InvalidRequestException
{
    public AccountRegistrationRejectedException(IReadOnlyList<string> reasons, string message)
        : base(message)
    {
        Reasons = reasons;
    }

    public IReadOnlyList<string> Reasons { get; }

    public static AccountRegistrationRejectedException InvalidEmail() =>
        new([AccountRegistrationErrorCodes.InvalidEmail], AccountRegistrationMessages.InvalidEmail);

    public static AccountRegistrationRejectedException FromIdentityErrors(IEnumerable<IdentityError>? errors)
    {
        var list = errors?.ToList() ?? [];
        return FromReasons(list.Select(error => error.Code), list.Select(error => error.Description));
    }

    public static AccountRegistrationRejectedException FromReasons(IEnumerable<string> codes, IEnumerable<string> descriptions)
    {
        var reasons = codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var texts = descriptions
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToList();
        var message = texts.Count > 0
            ? string.Join(AccountRegistrationMessages.ReasonSeparator, texts)
            : AccountRegistrationMessages.Rejected;

        return new AccountRegistrationRejectedException(reasons, message);
    }
}
