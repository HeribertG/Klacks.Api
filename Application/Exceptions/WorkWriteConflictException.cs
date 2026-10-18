// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A work write was refused with a reason the client can explain in the user's language. Still a
/// <see cref="ConflictException"/>, so everything that handles a conflict keeps working; the middleware
/// additionally puts the code and the details into the 409 body.
/// </summary>
/// <param name="message">English diagnostic message, kept for logs and for clients that do not know the code</param>
/// <param name="errorCode">Machine-readable discriminator, one of the WorkWriteConflictCodes</param>
/// <param name="details">Fields written next to the error code in the 409 body (names, counts, dates, conflict items)</param>
public sealed class WorkWriteConflictException : ConflictException, ICodedRejection
{
    public WorkWriteConflictException(string message, string errorCode, IReadOnlyDictionary<string, object?> details)
        : base(message, errorCode, details)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Details { get; }

    /// <summary>
    /// Raises exactly the refusal a non-throwing booking check found, so the check and the write cannot diverge.
    /// </summary>
    /// <param name="refusal">The refusal returned by an IWorkWriteGuard Find method</param>
    public static WorkWriteConflictException From(WorkBookingRefusal refusal)
        => new(refusal.Message, refusal.Code, refusal.Details);
}
