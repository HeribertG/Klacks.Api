// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A write on a variant (key) that its state refuses, answered as a 409 with a code of ScenarioVariantErrorCodes.
/// </summary>
/// <param name="message">English detail for logs and developers</param>
/// <param name="conflictCode">One of ScenarioVariantErrorCodes</param>

namespace Klacks.Api.Application.Exceptions;

public sealed class ScenarioVariantConflictException : ConflictException
{
    public ScenarioVariantConflictException(string message, string conflictCode)
        : base(message, conflictCode)
    {
    }
}
