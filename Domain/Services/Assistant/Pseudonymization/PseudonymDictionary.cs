// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Immutable name side of the pseudonymisation, built by <see cref="PseudonymVocabulary"/>: folded name phrases to
/// tokens, canonical tokens back to the exact database strings, the legend per person, the personnel-number tokens
/// the tails of the phone numbers stored in the communication table and the street keys of the stored addresses.
/// </summary>
/// <param name="phrases">Folded phrase key to the token it becomes</param>
/// <param name="reverse">Canonical token to the surface it restores to</param>
/// <param name="legend">Base token to its legend text (gender or organisation)</param>
/// <param name="idNumberTokens">Unique personnel number to its P token</param>
/// <param name="knownPhoneTails">Last digits of the stored phone numbers</param>
/// <param name="streets">Folded street key to the ADR token it becomes</param>
/// <param name="streetHouseNumbers">House numbers that occur in a street key, a cheap filter for the text scan</param>

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed class PseudonymDictionary
{
    private readonly IReadOnlyDictionary<string, PseudonymPhrase> _phrases;
    private readonly IReadOnlyDictionary<string, string> _reverse;
    private readonly IReadOnlyDictionary<string, string> _legend;
    private readonly IReadOnlyDictionary<int, string> _idNumberTokens;
    private readonly IReadOnlySet<string> _knownPhoneTails;
    private readonly IReadOnlyDictionary<string, PseudonymPhrase> _streets;
    private readonly IReadOnlySet<string> _streetHouseNumbers;

    public PseudonymDictionary(
        IReadOnlyDictionary<string, PseudonymPhrase> phrases,
        IReadOnlyDictionary<string, string> reverse,
        IReadOnlyDictionary<string, string> legend,
        IReadOnlyDictionary<int, string> idNumberTokens,
        IReadOnlySet<string> knownPhoneTails,
        IReadOnlyDictionary<string, PseudonymPhrase> streets,
        IReadOnlySet<string> streetHouseNumbers)
    {
        _phrases = phrases;
        _reverse = reverse;
        _legend = legend;
        _idNumberTokens = idNumberTokens;
        _knownPhoneTails = knownPhoneTails;
        _streets = streets;
        _streetHouseNumbers = streetHouseNumbers;
    }

    public int PhraseCount => _phrases.Count;

    public int StreetCount => _streets.Count;

    public bool TryGetPhrase(string foldedKey, out PseudonymPhrase phrase) =>
        _phrases.TryGetValue(foldedKey, out phrase!);

    public bool TryGetSurface(string canonicalToken, out string surface) =>
        _reverse.TryGetValue(canonicalToken, out surface!);

    public bool TryGetLegend(string baseToken, out string legend) =>
        _legend.TryGetValue(baseToken, out legend!);

    public bool TryGetIdNumberToken(int idNumber, out string token) =>
        _idNumberTokens.TryGetValue(idNumber, out token!);

    public bool IsKnownPhone(string comparisonTail) => _knownPhoneTails.Contains(comparisonTail);

    public bool TryGetStreet(string foldedKey, out PseudonymPhrase phrase) =>
        _streets.TryGetValue(foldedKey, out phrase!);

    public bool IsStreetHouseNumber(string houseNumber) => _streetHouseNumbers.Contains(houseNumber);
}
