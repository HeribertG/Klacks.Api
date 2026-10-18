// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A booking was refused for a reason answered with 400 (sealed day, planning period, work lock level). Still an
/// <see cref="InvalidRequestException"/>, so status, message and every existing catch stay as they are; the middleware
/// additionally writes the code and the details into the 400 body.
/// </summary>
/// <param name="message">English diagnostic message, unchanged from the uncoded refusal</param>
/// <param name="errorCode">Machine-readable discriminator, one of the WorkBookingRefusalCodes</param>
/// <param name="details">Fields written next to the error code in the 400 body (date, client, period bounds, lock level)</param>
public sealed class WorkBookingRefusedException : InvalidRequestException, ICodedRejection
{
    public WorkBookingRefusedException(string message, string errorCode, IReadOnlyDictionary<string, object?> details)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Details { get; }
}
