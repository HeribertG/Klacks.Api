// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System;

namespace Klacks.Api.Application.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
