// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A move reversal refused for a business reason, answered with 400. The middleware writes the code into the body.
/// </summary>
/// <param name="message">English diagnostic message</param>
/// <param name="errorCode">One of the MoveReversalRefusalCodes</param>
public sealed class MoveReversalRefusedException : InvalidRequestException, ICodedRejection
{
    public MoveReversalRefusedException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
