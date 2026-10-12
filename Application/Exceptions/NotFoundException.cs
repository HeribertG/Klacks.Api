// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Exceptions;

public class NotFoundException : Exception, IUserFacingRejection
{
    public NotFoundException(string message) : base(message)
    {
    }
}
