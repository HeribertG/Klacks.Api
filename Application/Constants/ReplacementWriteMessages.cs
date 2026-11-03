// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Refusal texts of the replacement write guard, as shown to the planner.
/// </summary>
public static class ReplacementWriteMessages
{
    public const string PersonRequired = "A replacement needs a person: ReplaceClientId is required.";

    public const string PersonNotFound = "The replacement person does not exist or is deleted.";

    public const string Blocked =
        "Replacement blocked: client {0} would introduce {1} blocking conflict(s) on {2}. Not committed.";

    public const string PersonNotAnEmployee = "The replacement person must be an employee or an external employee.";
}
