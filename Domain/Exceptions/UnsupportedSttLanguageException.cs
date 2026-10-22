// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// The self-hosted speech recognition engine refused a recording because it does not support the requested language
/// (the local Parakeet engine covers 25 European languages only).
/// </summary>
/// <param name="language">The language code that was sent to the engine</param>
public sealed class UnsupportedSttLanguageException : Exception
{
    public UnsupportedSttLanguageException(string language)
        : base($"The local speech recognition does not support the language '{language}'.")
    {
        Language = language;
    }

    public string Language { get; }
}
