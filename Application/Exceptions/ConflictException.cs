// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System;

namespace Klacks.Api.Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    protected ConflictException(string message, string conflictCode) : base(message)
    {
        ConflictCode = conflictCode;
    }

    /// <summary>
    /// Machine-readable discriminator the middleware puts on the 409 body as "errorCode", so a client can
    /// tell this conflict apart from every other one. Null for an uncoded conflict.
    /// </summary>
    public string? ConflictCode { get; }
}
