// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The caller is authenticated but lacks the right for this operation. Mapped to 403 by
/// ErrorHandlingMiddleware. Deliberately not UnauthorizedException, which maps to 401 and makes the SPA
/// log the user out — a missing right is not a missing or expired session. Deliberately not
/// InvalidRequestException either, which maps to 400 and would describe a rights problem as a malformed
/// request the caller could fix by sending different data.
/// </summary>

namespace Klacks.Api.Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
