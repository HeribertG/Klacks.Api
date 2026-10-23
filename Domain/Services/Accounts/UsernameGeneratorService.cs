// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives a unique, technical user name from first and last name (e.g. "Max Mustermann" -> "mmu"). The result is
/// plain ASCII [a-z0-9], because Identity's default AllowedUserNameCharacters rejects everything else: a name in
/// Cyrillic, Greek, Arabic, Hebrew or CJK script falls back to "usr" plus a counter instead of producing a user name
/// Identity refuses. People sign in with their e-mail address, so the user name never has to be readable.
/// </summary>
/// <param name="userManager">Looks up whether a candidate user name is already taken</param>

using System.Globalization;
using System.Text;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Domain.Services.Accounts;

public class UsernameGeneratorService : IUsernameGeneratorService
{
    private const string FallbackUsername = "usr";
    private const int BaseUsernameLength = 3;
    private const int LastNamePrefixLength = 2;
    private const int MaxNumericSuffix = 999;
    private const int RandomSuffixLength = 4;

    private static readonly IReadOnlyDictionary<string, string> Transliterations = new Dictionary<string, string>
    {
        ["ä"] = "ae", ["ö"] = "oe", ["ü"] = "ue", ["ß"] = "ss",
        ["Ä"] = "Ae", ["Ö"] = "Oe", ["Ü"] = "Ue",
        ["æ"] = "ae", ["Æ"] = "Ae", ["œ"] = "oe", ["Œ"] = "Oe",
        ["ø"] = "o", ["Ø"] = "O", ["ł"] = "l", ["Ł"] = "L",
        ["đ"] = "d", ["Đ"] = "D", ["ð"] = "d", ["Ð"] = "D",
        ["þ"] = "th", ["Þ"] = "Th", ["ı"] = "i",
    };

    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UsernameGeneratorService> _logger;

    public UsernameGeneratorService(
        UserManager<AppUser> userManager,
        ILogger<UsernameGeneratorService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName)
    {
        var baseUsername = CreateBaseUsername(firstName, lastName);
        var username = baseUsername;
        var counter = 1;

        while (await UsernameExistsAsync(username))
        {
            username = $"{baseUsername}{counter}";
            counter++;

            if (counter > MaxNumericSuffix)
            {
                username = $"{baseUsername}{Guid.NewGuid().ToString()[..RandomSuffixLength]}";
                break;
            }
        }

        _logger.LogInformation("Generated a unique username ({Length} characters)", username.Length);

        return username;
    }

    private static string CreateBaseUsername(string firstName, string lastName)
    {
        var first = NormalizeString(firstName);
        var last = NormalizeString(lastName);

        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
        {
            return FallbackUsername;
        }

        if (string.IsNullOrEmpty(last))
        {
            return first.Length >= BaseUsernameLength ? first[..BaseUsernameLength].ToLowerInvariant() : PadToThree(first);
        }

        if (string.IsNullOrEmpty(first))
        {
            return last.Length >= BaseUsernameLength ? last[..BaseUsernameLength].ToLowerInvariant() : PadToThree(last);
        }

        // First letter of firstName + first 2 letters of lastName (e.g., "Max Mustermann" -> "mmu")
        var result = first[0].ToString();
        result += last.Length >= LastNamePrefixLength ? last[..LastNamePrefixLength] : last + "x";

        return result.ToLowerInvariant();
    }

    private static string PadToThree(string input)
    {
        var result = input.ToLowerInvariant();
        while (result.Length < BaseUsernameLength)
        {
            result += "x";
        }
        return result;
    }

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var transliterated = Transliterations.Aggregate(
            input.Trim(),
            (current, pair) => current.Replace(pair.Key, pair.Value, StringComparison.Ordinal));

        var decomposed = transliterated.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
                && char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private async Task<bool> UsernameExistsAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        return user != null;
    }
}
