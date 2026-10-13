// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// English detail texts of a rejected account registration. The UI shows its own translation of the reason
/// codes (AccountRegistrationErrorCodes); these texts are the readable summary for logs and API clients.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AccountRegistrationMessages
{
    public const string Rejected = "The account registration was rejected.";

    public const string RegistrationRequired = "The registration data is required.";

    public const string InvalidEmail = "The e-mail address is not valid.";

    public const string FirstNameRequired = "The first name is required.";

    public const string LastNameRequired = "The last name is required.";

    public const string ReasonSeparator = " ";
}
