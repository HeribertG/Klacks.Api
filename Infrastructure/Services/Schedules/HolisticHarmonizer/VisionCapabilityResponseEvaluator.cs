// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Judges one answer to the Wizard 3 vision capability check. Passed when the expected token comes back, either
/// in the requested {"token":"..."} JSON or - for models that ignore the format - as the only upper-case word of
/// token length in plain text. Misread when the model answered without the token (wrong token, empty token, prose).
/// Inconclusive when the answer says nothing about vision: a provider error, or no visible answer because the
/// output budget went into reasoning. Only misreads may count towards a "not vision-capable" verdict.
/// </summary>
/// <param name="response">The provider response to the capability request</param>
/// <param name="expectedToken">The upper-case token painted into the test image</param>
/// <param name="tokenAlphabet">Letters tokens are drawn from; plain-text words with other letters (PNG, JSON) are ignored. Null = any letter</param>

using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

public static class VisionCapabilityResponseEvaluator
{
    private const string TokenProperty = "token";
    private const int ResponsePreviewLength = 120;
    private const string PreviewEllipsis = "...";
    private const string ProviderRejectedError = "Provider rejected the request.";
    private const string NoVisibleAnswerError = "Model returned no visible answer (output budget likely consumed by internal reasoning).";
    private const string EmptyTokenError = "Model returned an empty token - it does not see the attached image.";
    private const string WrongTokenErrorFormat = "Model read token '{0}' but the image showed '{1}'.";
    private const string NoTokenErrorFormat = "Model answered without the image token. Preview: {0}";
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static VisionCapabilityVerdict Evaluate(LLMProviderResponse response, string expectedToken, string? tokenAlphabet = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedToken);

        if (!response.Success)
        {
            return new VisionCapabilityVerdict(VisionCapabilityOutcome.Inconclusive, response.Error ?? ProviderRejectedError);
        }

        var content = response.Content ?? string.Empty;
        var jsonToken = TryReadJsonToken(content);
        if (jsonToken is not null)
        {
            return JudgeJsonToken(jsonToken, expectedToken);
        }

        if (PlainTextNamesOnlyToken(content, expectedToken, tokenAlphabet))
        {
            return new VisionCapabilityVerdict(VisionCapabilityOutcome.Passed, null);
        }

        if (string.IsNullOrWhiteSpace(content) || response.ReasoningWithoutContent || response.OutputTruncated)
        {
            return new VisionCapabilityVerdict(VisionCapabilityOutcome.Inconclusive, NoVisibleAnswerError);
        }

        return new VisionCapabilityVerdict(VisionCapabilityOutcome.Misread, string.Format(NoTokenErrorFormat, Preview(content)));
    }

    internal static string NormaliseToken(string raw)
    {
        Span<char> buffer = stackalloc char[raw.Length];
        var length = 0;
        foreach (var c in raw)
        {
            if (char.IsLetterOrDigit(c))
            {
                buffer[length++] = char.ToUpperInvariant(c);
            }
        }

        return new string(buffer[..length]);
    }

    private static VisionCapabilityVerdict JudgeJsonToken(string actualToken, string expectedToken)
    {
        var normalised = NormaliseToken(actualToken);
        if (normalised.Length == 0)
        {
            return new VisionCapabilityVerdict(VisionCapabilityOutcome.Misread, EmptyTokenError);
        }

        return string.Equals(normalised, expectedToken, StringComparison.Ordinal)
            ? new VisionCapabilityVerdict(VisionCapabilityOutcome.Passed, null)
            : new VisionCapabilityVerdict(VisionCapabilityOutcome.Misread, string.Format(WrongTokenErrorFormat, actualToken, expectedToken));
    }

    private static string? TryReadJsonToken(string content)
    {
        var json = HarmonyJsonParser.ExtractJsonObject(content);
        if (json is null)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(TokenProperty, out var tokenElement)
                && tokenElement.ValueKind == JsonValueKind.String
                ? tokenElement.GetString() ?? string.Empty
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool PlainTextNamesOnlyToken(string content, string expectedToken, string? tokenAlphabet)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var pattern = $@"\b[A-Z]{{{expectedToken.Length}}}\b";
        var candidates = Regex.Matches(content, pattern, RegexOptions.None, RegexTimeout)
            .Select(m => m.Value)
            .Where(word => tokenAlphabet is null || word.All(tokenAlphabet.Contains))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return candidates.Count == 1 && string.Equals(candidates[0], expectedToken, StringComparison.Ordinal);
    }

    private static string Preview(string content) =>
        content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + PreviewEllipsis : content;
}
