// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Name fragments that mark a skill invocation parameter as secret-carrying, the names that only
/// look like one, and the placeholder written in place of a redacted value. The fragments are
/// derived from the skills that really accept secrets: the settings writers (apiKey, sttApiKey,
/// deepgramApiKey, groqApiKey, assemblyAiApiKey, openAiTtsApiKey, elevenLabsTtsApiKey,
/// googleTtsApiKey), the identity provider skills (bindPassword, clientSecret) and the mail
/// configuration skills (password, smtpPassword).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SensitiveSkillParameters
{
    public const string RedactedValue = "[REDACTED]";

    public static readonly IReadOnlyList<string> NameFragments = new[]
    {
        "password",
        "passphrase",
        "apikey",
        "api_key",
        "secret",
        "credential",
        "privatekey",
        "private_key",
        "token"
    };

    public static readonly IReadOnlySet<string> NonSecretNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "tokenUrl",
        "tokenId",
        "analyseToken",
        "maxTokens",
        "costPerInputToken",
        "costPerOutputToken"
    };
}
