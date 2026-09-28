// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Domain.Exceptions;

public class InvalidRequestException : Exception
{
    public InvalidRequestException(string message)
        : base(message)
    {
    }
}
