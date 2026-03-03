// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿namespace Klacks.Api.Domain.Exceptions;

public class InvalidRequestException : Exception
{
    public InvalidRequestException(string message)
        : base(message)
    {
    }
}
