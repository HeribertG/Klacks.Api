// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects an employee import request with a stable code the UI translates (file errors of Parse,
/// invalid grids or policies, remaining row errors on Commit). Derives from InvalidRequestException so
/// BaseHandler rethrows it unchanged; ErrorHandlingMiddleware answers it with 400 and the code.
/// </summary>
/// <param name="code">One of ClientImportErrorCodes</param>
/// <param name="message">English detail for logs and developers; it reaches the client, so it never carries a library's raw message</param>
/// <param name="innerException">Library failure behind the rejection; only logged, never sent to the client</param>

using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public class ClientImportRejectedException : InvalidRequestException
{
    public ClientImportRejectedException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public ClientImportRejectedException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
