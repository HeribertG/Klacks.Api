// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Signals that an import with the same token was already committed. Derives from ConflictException
/// so BaseHandler rethrows it and ErrorHandlingMiddleware answers 409 with the code.
/// </summary>
/// <param name="message">English detail for logs and developers</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Exceptions;

public class ClientImportConflictException : ConflictException
{
    public ClientImportConflictException(string message)
        : base(message)
    {
    }

    public string Code => ClientImportErrorCodes.AlreadyCommitted;
}
