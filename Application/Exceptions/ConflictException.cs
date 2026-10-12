// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public class ConflictException : Exception, IUserFacingRejection
{
    public ConflictException(string message) : base(message) { }

    protected ConflictException(string message, string conflictCode) : base(message)
    {
        ConflictCode = conflictCode;
    }

    protected ConflictException(string message, string conflictCode, IReadOnlyDictionary<string, object?> conflictDetails)
        : this(message, conflictCode)
    {
        ConflictDetails = conflictDetails;
    }

    /// <summary>
    /// Machine-readable discriminator the middleware puts on the 409 body as "errorCode", so a client can
    /// tell this conflict apart from every other one. Null for an uncoded conflict.
    /// </summary>
    public string? ConflictCode { get; }

    /// <summary>
    /// Fields the middleware writes next to <see cref="ConflictCode"/> in the 409 body (counts, instants, names),
    /// so a client can explain the conflict in the user's language. Null when the conflict carries none.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? ConflictDetails { get; }
}
