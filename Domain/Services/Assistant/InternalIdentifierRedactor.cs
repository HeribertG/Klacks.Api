// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replaces internal snake_case identifiers (skill, tool and function names) with neutral wording in
/// text that reaches the end user WITHOUT passing through the model. Prompt rules cannot cover those
/// fallback notices, because they are assembled in code after the model loop has already ended.
/// Model-facing text (tool results fed back into the loop) must never be redacted - the model needs
/// the real names to keep working.
/// </summary>
/// <param name="text">Raw text that may contain internal identifiers</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static partial class InternalIdentifierRedactor
{
    public static string Redact(string? text)
    {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : SnakeCaseIdentifierRegex().Replace(text, MutationGuardConstants.RedactedInternalIdentifier);
    }

    [GeneratedRegex(@"\b[a-z][a-z0-9]*(?:_[a-z0-9]+)+\b")]
    private static partial Regex SnakeCaseIdentifierRegex();
}
