// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Scripting;

public class ScriptTooComplexException : Exception
{
    public ScriptTooComplexException(string message) : base(message)
    {
    }

    public ScriptTooComplexException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
