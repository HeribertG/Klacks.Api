// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Another request changed the planning constraint (approve, update, expiry sweep) between this request's read
/// and its save; detected through the xmin row version. Answered as 409 with errorCode
/// <see cref="ErrorCode"/> instead of the generic 400 BaseHandler would produce for a ConcurrencyException.
/// </summary>
/// <param name="innerException">The ConcurrencyException raised by the unit of work</param>

namespace Klacks.Api.Application.Exceptions;

public class PlanningConstraintConcurrencyException : ConflictException
{
    public const string ErrorCode = "planningConstraintConcurrencyConflict";

    public PlanningConstraintConcurrencyException(Exception innerException)
        : base("The planning constraint was changed by another request. Reload it and try again.", ErrorCode)
    {
        InnerCause = innerException;
    }

    public Exception InnerCause { get; }
}
