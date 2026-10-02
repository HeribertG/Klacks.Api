// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// An accept or reject was requested for a scenario that is no longer Active (already accepted,
/// rejected or superseded). Running the decision again would soft-delete real schedule data without
/// anything left to promote, so it is refused as a 409 the client can tell apart from a compliance block.
/// </summary>
/// <param name="message">English detail for logs, the 409 body and skill results</param>
public sealed class ScenarioNotActiveException : ConflictException
{
    public const string ErrorCode = "SCENARIO_NOT_ACTIVE";

    public ScenarioNotActiveException(string message)
        : base(message, ErrorCode)
    {
    }
}
