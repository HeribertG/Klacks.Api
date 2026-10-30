// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name fragments that mark a skill invocation parameter as secret-carrying, the names that only
/// look like one, and the placeholder written in place of a redacted value. The fragments are
/// derived from the skills that really accept secrets: the settings writers (apiKey, sttApiKey,
/// deepgramApiKey, groqApiKey, assemblyAiApiKey, openAiTtsApiKey, elevenLabsTtsApiKey,
/// googleTtsApiKey), the identity provider skills (bindPassword, clientSecret) and the mail
/// configuration skills (password, smtpPassword). Some parameters are no secrets by name but carry personal content in one
/// skill only, so they are listed per skill: "content" must stay readable elsewhere, and a name fragment would also hit
/// contentType.
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

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PersonalParametersBySkill =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["send_broadcast"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "content", "idNumbers" },
            ["send_message"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "content" }
        };

    public static bool IsPersonalInSkill(string? skillName, string parameterName) =>
        skillName != null
        && PersonalParametersBySkill.TryGetValue(skillName, out var names)
        && names.Contains(parameterName);
}
