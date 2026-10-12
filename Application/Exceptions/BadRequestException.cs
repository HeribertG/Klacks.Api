// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public class BadRequestException : Exception, IUserFacingRejection
{
    public BadRequestException(string message) : base(message)
    {
    }
}
