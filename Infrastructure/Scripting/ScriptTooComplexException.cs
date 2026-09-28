// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
