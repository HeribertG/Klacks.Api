// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the persisted phonetic token string for a client's name fields: every word of
/// Name/FirstName/SecondName/MaidenName is encoded with the Kölner Phonetik and the distinct
/// codes are space-joined. The same builder encodes search queries, so a misheard spoken name
/// ("Meier" vs "Mayer") matches the stored client via phonetic code equality.
/// </summary>
/// <param name="nameParts">Raw name field values; null/blank parts are skipped</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Phonetics;

namespace Klacks.Api.Domain.Services.Clients;

public static class ClientPhoneticTokenBuilder
{
    private static readonly KoelnerPhoneticEncoder Encoder = new();
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private const int MinTokenLength = 2;
    private const char TokenSeparator = ' ';

    public static string? Build(params string?[] nameParts)
    {
        var codes = nameParts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .SelectMany(part => WordPattern.Matches(part!).Select(m => m.Value))
            .Where(token => token.Length >= MinTokenLength)
            .Select(Encoder.Encode)
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return codes.Count == 0 ? null : string.Join(TokenSeparator, codes);
    }

    public static string? BuildFor(Client client)
    {
        return Build(client.Name, client.FirstName, client.SecondName, client.MaidenName);
    }
}
