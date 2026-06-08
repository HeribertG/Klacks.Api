// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Data-driven phonetic encoder: applies an ordered list of sound-equivalence
/// replacement rules (from the language pack's phonetics.json), then normalizes
/// to letters and collapses consecutive duplicates. Lets a plugin language define
/// its phonetics purely as data, without new code.
/// </summary>
namespace Klacks.Api.Domain.Services.Assistant.Phonetics;

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class RuleBasedPhoneticEncoder : IPhoneticEncoder
{
    private readonly IReadOnlyList<PhoneticRule> _rules;

    public RuleBasedPhoneticEncoder(IReadOnlyList<PhoneticRule> rules)
    {
        _rules = rules;
    }

    public string Encode(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var letters = new StringBuilder();
        foreach (var ch in word.ToLowerInvariant())
        {
            if (char.IsLetter(ch))
                letters.Append(ch);
        }

        var value = letters.ToString();
        foreach (var rule in _rules)
        {
            if (!string.IsNullOrEmpty(rule.From))
                value = value.Replace(rule.From, rule.To);
        }

        var result = new StringBuilder();
        char? previous = null;
        foreach (var ch in value)
        {
            if (ch != previous)
                result.Append(ch);
            previous = ch;
        }

        return result.ToString();
    }
}
