// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Everything one request needs to pseudonymise and restore: the name dictionary plus the numbers of the pattern
/// values (phone, e-mail) of this request. Pattern values are numbered per request in
/// order of first appearance and never stored, so no plain value is ever written to the database; the history comes
/// first in every request, which keeps its numbers the same from turn to turn. Restoring is limited to what the forward pass of this request emitted: a code the model never got
/// from Klacks (a guessed TEL-0009, a code the user typed) is treated as unknown, so the return path can never reveal
/// data the request did not carry. For a person whose name was emitted, the full name and the other name parts may also
/// be restored (the model may write MA-0017 after seeing MA-0017-V); the personnel number only when it was emitted.
/// </summary>
/// <param name="dictionary">The name side</param>
/// <param name="patternValues">The pattern values found in the request, in order of appearance</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed class PseudonymMap
{
    private readonly IReadOnlyDictionary<PseudonymKey, int> _patternOrdinals;
    private readonly Dictionary<string, string> _patternReverse = new(StringComparer.Ordinal);
    private readonly HashSet<string> _emitted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _emittedPersonBases = new(StringComparer.Ordinal);

    public PseudonymMap(PseudonymDictionary dictionary, IEnumerable<PseudonymPatternMatch> patternValues)
    {
        Dictionary = dictionary;
        var ordinals = new Dictionary<PseudonymKey, int>();
        var countPerKind = new Dictionary<PseudonymKind, int>();
        foreach (var value in patternValues)
        {
            var key = new PseudonymKey(value.Kind, value.Value);
            if (ordinals.ContainsKey(key))
            {
                continue;
            }

            var ordinal = countPerKind.GetValueOrDefault(value.Kind) + 1;
            countPerKind[value.Kind] = ordinal;
            ordinals[key] = ordinal;
            _patternReverse[new PseudonymToken(value.Kind, ordinal).ToString()] = value.Value;
        }

        _patternOrdinals = ordinals;
    }

    public PseudonymDictionary Dictionary { get; }

    public bool TryGetPatternToken(PseudonymKind kind, string value, out string token)
    {
        if (_patternOrdinals.TryGetValue(new PseudonymKey(kind, value), out var ordinal))
        {
            token = new PseudonymToken(kind, ordinal).ToString();
            return true;
        }

        token = string.Empty;
        return false;
    }

    /// <summary>
    /// Records a token the forward pass wrote into the request, which makes it restorable.
    /// </summary>
    public void MarkEmitted(string token)
    {
        _emitted.Add(token);
        if (TryParsePersonToken(token, out var baseToken, out _))
        {
            _emittedPersonBases.Add(baseToken);
        }
    }

    public bool TryRestore(string canonicalToken, out string surface)
    {
        surface = string.Empty;
        if (!IsRestorable(canonicalToken))
        {
            return false;
        }

        return Dictionary.TryGetSurface(canonicalToken, out surface) || _patternReverse.TryGetValue(canonicalToken, out surface!);
    }

    private bool IsRestorable(string canonicalToken)
    {
        if (_emitted.Contains(canonicalToken))
        {
            return true;
        }

        return TryParsePersonToken(canonicalToken, out var baseToken, out var variant)
            && variant != PseudonymizationConstants.PersonnelNumberVariant
            && _emittedPersonBases.Contains(baseToken);
    }

    private static bool TryParsePersonToken(string token, out string baseToken, out string? variant)
    {
        baseToken = string.Empty;
        variant = null;
        var separator = token.IndexOf(PseudonymizationConstants.TokenSeparator);
        if (separator <= 0 || !PseudonymPrefixes.TryGetKind(token[..separator], out var kind)
            || kind is not (PseudonymKind.Employee or PseudonymKind.Customer or PseudonymKind.User))
        {
            return false;
        }

        var variantSeparator = token.IndexOf(PseudonymizationConstants.TokenSeparator, separator + 1);
        baseToken = variantSeparator < 0 ? token : token[..variantSeparator];
        variant = variantSeparator < 0 ? null : token[(variantSeparator + 1)..];
        return true;
    }
}
