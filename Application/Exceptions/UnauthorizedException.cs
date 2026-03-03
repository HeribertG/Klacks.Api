// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System;

namespace Klacks.Api.Application.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
