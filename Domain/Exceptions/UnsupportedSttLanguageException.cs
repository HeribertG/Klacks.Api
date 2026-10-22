// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Exceptions;

/// <summary>
/// The self-hosted speech recognition engine refused a recording because it does not support the requested language
/// (the local Parakeet engine covers 25 European languages only). A coded 400 rejection, so any caller that does not
/// catch it specifically still answers with the code and the language in the problem body.
/// </summary>
/// <param name="language">The language code that was sent to the engine</param>
public sealed class UnsupportedSttLanguageException : InvalidRequestException, ICodedRejection
{
    private const string LanguageDetail = "language";

    public UnsupportedSttLanguageException(string language)
        : base($"The local speech recognition does not support the language '{language}'.")
    {
        Language = language;
        Details = new Dictionary<string, object?> { [LanguageDetail] = language };
    }

    public string Language { get; }

    public string ErrorCode => SttErrorCodes.UnsupportedLanguage;

    public IReadOnlyDictionary<string, object?> Details { get; }
}
