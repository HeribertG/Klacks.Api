// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Header and gender vocabulary of the employee import in all supported languages, read from the
/// embedded resource client-import-header-synonyms.json. The first synonym of a target is the header the
/// template writes for that language; every synonym, normalized, maps to exactly one target across all
/// languages, so the column detector needs no language hint. FindNameParts locates the first-name,
/// last-name and generic name words inside a combined header such as "Nachname, Vorname". It reads the
/// header with the words of one language at a time and keeps the language whose words cover most of it,
/// so "Nom et prenom" is French (nom + prenom) and not Italian "nome" followed by a French "prenom";
/// within a language longer words claim their characters first, so "Cognome" is one last-name word and
/// not "Nome" inside it. A generic name word is a full-name synonym of the language that contains none of
/// its first- or last-name words ("Name", "Navn").
/// </summary>
/// <param name="headers">Language code -> target -> synonyms (first one is the template header)</param>
/// <param name="genderValues">Language code -> gender -> gender words and salutations</param>

using System.Reflection;
using System.Text.Json;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportSynonymCatalog
{
    public const string ResourceName = "Klacks.Api.ClientImport.HeaderSynonyms.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, ClientImportTarget> _headerIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GenderEnum> _genderIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<ClientImportTarget, List<string>>> _headers;
    private readonly List<List<(string Word, ClientImportNameWordKind Kind)>> _nameWordsPerLanguage;

    public ClientImportSynonymCatalog(
        Dictionary<string, Dictionary<ClientImportTarget, List<string>>> headers,
        Dictionary<string, Dictionary<GenderEnum, List<string>>> genderValues)
    {
        _headers = new Dictionary<string, Dictionary<ClientImportTarget, List<string>>>(headers, StringComparer.OrdinalIgnoreCase);

        foreach (var synonym in headers.Values.SelectMany(t => t).SelectMany(t => t.Value.Select(s => (Target: t.Key, Synonym: s))))
        {
            _headerIndex.TryAdd(ClientImportTextNormalizer.Normalize(synonym.Synonym), synonym.Target);
        }

        foreach (var value in genderValues.Values.SelectMany(g => g).SelectMany(g => g.Value.Select(v => (Gender: g.Key, Value: v))))
        {
            _genderIndex.TryAdd(ClientImportTextNormalizer.Normalize(value.Value), value.Gender);
        }

        HeaderSynonyms = _headers;
        GenderValues = genderValues;
        _nameWordsPerLanguage = headers.Values.Select(BuildNameWords).ToList();
    }

    public IReadOnlyDictionary<string, Dictionary<ClientImportTarget, List<string>>> HeaderSynonyms { get; }

    public IReadOnlyDictionary<string, Dictionary<GenderEnum, List<string>>> GenderValues { get; }

    public IEnumerable<string> Languages => _headers.Keys;

    public IReadOnlyDictionary<string, ClientImportTarget> NormalizedHeaderIndex => _headerIndex;

    public static ClientImportSynonymCatalog LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        var document = JsonSerializer.Deserialize<ClientImportSynonymDocument>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is empty.");

        return new ClientImportSynonymCatalog(document.Headers, document.GenderValues);
    }

    public bool IsSupportedLanguage(string? language) =>
        !string.IsNullOrWhiteSpace(language) && _headers.ContainsKey(language);

    public ClientImportTarget? MatchHeader(string? header)
    {
        var normalized = ClientImportTextNormalizer.Normalize(header);
        return normalized.Length > 0 && _headerIndex.TryGetValue(normalized, out var target) ? target : null;
    }

    public ClientImportTarget? MatchHeaderContaining(string? header, int minimumSynonymLength)
    {
        var normalized = ClientImportTextNormalizer.Normalize(header);
        if (normalized.Length == 0)
        {
            return null;
        }

        var best = _headerIndex
            .Where(entry => entry.Key.Length >= minimumSynonymLength && normalized.Contains(entry.Key, StringComparison.Ordinal))
            .OrderByDescending(entry => entry.Key.Length)
            .Select(entry => (ClientImportTarget?)entry.Value)
            .FirstOrDefault();

        return best;
    }

    /// <param name="header">Header text of one column</param>
    /// <param name="minimumWordLength">First- and last-name words shorter than this are ignored (generic words always count)</param>
    public ClientImportNameHeaderParts FindNameParts(string? header, int minimumWordLength)
    {
        var normalized = ClientImportTextNormalizer.Normalize(header);
        var best = new ClientImportNameHeaderParts(null, null, null);
        var bestCoverage = 0;

        foreach (var words in _nameWordsPerLanguage)
        {
            var (parts, coverage) = FindNameParts(normalized, words, minimumWordLength);
            if (coverage > bestCoverage)
            {
                best = parts;
                bestCoverage = coverage;
            }
        }

        return best;
    }

    private static (ClientImportNameHeaderParts Parts, int Coverage) FindNameParts(
        string normalized, List<(string Word, ClientImportNameWordKind Kind)> words, int minimumWordLength)
    {
        var claimed = new bool[normalized.Length];
        var positions = new Dictionary<ClientImportNameWordKind, int>();
        var coverage = 0;

        foreach (var (word, kind) in words)
        {
            if (string.Equals(word, normalized, StringComparison.Ordinal) || (kind != ClientImportNameWordKind.GenericName && word.Length < minimumWordLength))
            {
                continue;
            }

            for (var start = normalized.IndexOf(word, StringComparison.Ordinal); start >= 0; start = normalized.IndexOf(word, start + 1, StringComparison.Ordinal))
            {
                if (IsFree(claimed, start, word.Length))
                {
                    Array.Fill(claimed, true, start, word.Length);
                    coverage += word.Length;
                    positions[kind] = positions.TryGetValue(kind, out var existing) ? Math.Min(existing, start) : start;
                }
            }
        }

        var parts = new ClientImportNameHeaderParts(
            Position(positions, ClientImportNameWordKind.FirstName),
            Position(positions, ClientImportNameWordKind.LastName),
            Position(positions, ClientImportNameWordKind.GenericName));
        return (parts, coverage);
    }

    public GenderEnum? ResolveGender(string? value)
    {
        var normalized = ClientImportTextNormalizer.Normalize(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        if (_genderIndex.TryGetValue(normalized, out var gender))
        {
            return gender;
        }

        var firstWord = value!.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var normalizedFirstWord = ClientImportTextNormalizer.Normalize(firstWord);
        return normalizedFirstWord.Length > 0 && _genderIndex.TryGetValue(normalizedFirstWord, out var firstWordGender)
            ? firstWordGender
            : null;
    }

    private static List<(string Word, ClientImportNameWordKind Kind)> BuildNameWords(Dictionary<ClientImportTarget, List<string>> targets)
    {
        var partWords = NormalizedSynonyms(targets, ClientImportTarget.FirstName).Select(w => (Word: w, Kind: ClientImportNameWordKind.FirstName))
            .Concat(NormalizedSynonyms(targets, ClientImportTarget.LastName).Select(w => (Word: w, Kind: ClientImportNameWordKind.LastName)))
            .ToList();

        var genericWords = NormalizedSynonyms(targets, ClientImportTarget.FullName)
            .Where(word => !partWords.Any(part => word.Contains(part.Word, StringComparison.Ordinal)))
            .Select(w => (Word: w, Kind: ClientImportNameWordKind.GenericName));

        return partWords.Concat(genericWords)
            .DistinctBy(entry => entry.Word)
            .OrderByDescending(entry => entry.Word.Length)
            .ThenBy(entry => entry.Word, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<string> NormalizedSynonyms(Dictionary<ClientImportTarget, List<string>> targets, ClientImportTarget target) =>
        (targets.TryGetValue(target, out var synonyms) ? synonyms : [])
            .Select(ClientImportTextNormalizer.Normalize)
            .Where(word => word.Length > 0)
            .Distinct(StringComparer.Ordinal);

    private static bool IsFree(bool[] claimed, int start, int length) =>
        Array.IndexOf(claimed, true, start, length) < 0;

    private static int? Position(Dictionary<ClientImportNameWordKind, int> positions, ClientImportNameWordKind kind) =>
        positions.TryGetValue(kind, out var position) ? position : null;

    public string? TemplateHeader(string language, ClientImportTarget target) =>
        _headers.TryGetValue(language, out var targets) && targets.TryGetValue(target, out var synonyms) && synonyms.Count > 0
            ? synonyms[0]
            : null;
}
