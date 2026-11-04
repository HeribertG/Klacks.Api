// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// A WorkChange refused because its work is a container, answered with 400. The middleware writes the code into the body.
/// </summary>
/// <param name="message">English diagnostic message</param>
/// <param name="errorCode">One of the ContainerWorkChangeRefusalCodes</param>
public sealed class ContainerWorkChangeRefusedException : InvalidRequestException, ICodedRejection
{
    public ContainerWorkChangeRefusedException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, object?> Details { get; } = new Dictionary<string, object?>();
}
