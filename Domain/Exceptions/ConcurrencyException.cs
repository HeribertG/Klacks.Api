// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
