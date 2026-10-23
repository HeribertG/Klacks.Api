// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Detects person-name candidates in the user message (capitalized token pairs, hyphenated
/// names and tokens following title markers like Herr/Frau/Mr/Mme; for short all-lowercase
/// voice transcripts, trailing token bigrams under a stricter validation), resolves them via
/// the client text search and, when that finds nobody, its fuzzy (trigram/phonetic) search — which
/// supplies the misheard-name second chance and keeps the result scoped to the caller's visible groups —
/// validates hits with normalized fuzzy plus Kölner Phonetik name matching and renders the grounding block
/// with canonical spellings and visible id numbers. Only people the message really names (ClientNameMatch,
/// the Strict name policy) are offered as spellings to copy; a person found only through a similar name
/// ("Mayer" for "Meier") is listed separately as a name the user has to confirm first, because the write
/// skills resolve the copied spelling strictly and would otherwise act on a name the user never said.
/// Because the rendered block is injected into the system prompt, it must never carry a client the caller
/// may not see. Purely additive: failures are swallowed and leave the context untouched.
/// </summary>

using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Services.Assistant;

public partial class ClientNameCandidateGrounder : IEntityCandidateGrounder
{
    private const int MaxCandidateTerms = 3;
    private const int MaxGroundedEntities = 5;
    private const int SearchLimitPerTerm = 5;
    private const int MinTermLength = 2;
    private const int BigramTokenCount = 2;
    private const int MaxTokensForLowercaseScan = 12;

    private const string BlockHeader = "KNOWN_MATCHING_PEOPLE (deterministically resolved from this message):";
    private const string BlockInstruction =
        "When calling a tool for one of these people, copy EXACTLY this spelling into firstName/lastName. "
        + "If several entries could be meant, ask the user which one instead of picking silently.";

    private const string SimilarBlockHeader =
        "SIMILAR_NAMED_PEOPLE (the message does NOT contain these names, only similar ones):";

    private const string SimilarBlockInstruction =
        "Before changing anything for one of these people, ask the user to confirm the name; do not pass "
        + "their spelling to a tool that changes data until the user has confirmed it.";

    private static readonly string[] TitleMarkers =
    {
        "herr", "frau", "mr", "mrs", "ms", "mme", "monsieur", "madame", "signor", "signora"
    };

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly ILogger<ClientNameCandidateGrounder> _logger;

    public ClientNameCandidateGrounder(
        IClientSearchRepository clientSearchRepository,
        ILogger<ClientNameCandidateGrounder> logger)
    {
        _clientSearchRepository = clientSearchRepository;
        _logger = logger;
    }

    public async Task GroundAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var terms = ExtractCandidateTerms(context.Message);
            var strictValidation = false;
            if (terms.Count == 0)
            {
                // Voice STT often delivers names all-lowercase, and the LLM re-capitalization
                // is skipped for server-side engines — probe trailing bigrams of short messages
                // instead, under a stricter (exact/phonetic) validation to keep noise out.
                terms = ExtractLowercaseFallbackTerms(context.Message);
                strictValidation = true;
            }

            if (terms.Count == 0)
            {
                return;
            }

            var grounded = new List<(int? IdNumber, string? FirstName, string? LastName, bool Named)>();
            var seen = new HashSet<Guid>();

            foreach (var term in terms)
            {
                var items = await SearchTermAsync(term, cancellationToken);

                foreach (var item in items)
                {
                    if (!seen.Add(item.Id) || !IsPlausibleMatch(term, item.FirstName, item.LastName, strictValidation))
                    {
                        continue;
                    }

                    var named = terms.Any(candidate => ClientNameMatch.Partition([item], candidate).Matches.Count > 0);
                    grounded.Add((item.IdNumber, item.FirstName, item.LastName, named));
                    if (grounded.Count >= MaxGroundedEntities)
                    {
                        break;
                    }
                }

                if (grounded.Count >= MaxGroundedEntities)
                {
                    break;
                }
            }

            if (grounded.Count == 0)
            {
                return;
            }

            context.EntityGroundingBlock = RenderBlock(grounded);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entity candidate grounding failed; continuing without grounding block");
        }
    }

    internal static List<string> ExtractCandidateTerms(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new List<string>();
        }

        var terms = new List<string>();
        var tokens = TokenPattern().Matches(message).Select(m => m.Value).ToList();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (i + 1 < tokens.Count && IsCapitalized(token) && IsCapitalized(tokens[i + 1]))
            {
                AddTerm(terms, $"{token} {tokens[i + 1]}");
            }

            if (i + 1 < tokens.Count && TitleMarkers.Contains(token.ToLowerInvariant()))
            {
                AddTerm(terms, tokens[i + 1]);
            }

            if (token.Contains('-') && IsCapitalized(token))
            {
                AddTerm(terms, token);
            }
        }

        return terms.Take(MaxCandidateTerms).ToList();
    }

    private static void AddTerm(List<string> terms, string term)
    {
        var trimmed = term.Trim();
        if (trimmed.Length >= MinTermLength
            && !terms.Any(t => string.Equals(t, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            terms.Add(trimmed);
        }
    }

    private static bool IsCapitalized(string token) =>
        token.Length >= MinTermLength && char.IsUpper(token[0]) && token.Skip(1).Any(char.IsLower);

    internal static List<string> ExtractLowercaseFallbackTerms(string? message)
    {
        var terms = new List<string>();
        if (string.IsNullOrWhiteSpace(message))
        {
            return terms;
        }

        var tokens = TokenPattern().Matches(message).Select(m => m.Value).ToList();
        if (tokens.Count < BigramTokenCount || tokens.Count > MaxTokensForLowercaseScan)
        {
            return terms;
        }

        // Spoken search phrases put the name last ("suche petra meier"), so scan bigrams
        // from the end of the message.
        for (var i = tokens.Count - BigramTokenCount; i >= 0 && terms.Count < MaxCandidateTerms; i--)
        {
            if (tokens[i].Length >= MinTermLength && tokens[i + 1].Length >= MinTermLength)
            {
                AddTerm(terms, $"{tokens[i]} {tokens[i + 1]}");
            }
        }

        return terms;
    }

    // SearchByFuzzyNameAsync runs the trigram/phonetic ranking over a larger candidate pool and
    // scoped to the caller's visible groups; a direct call to the fuzzy service would be unscoped.
    private async Task<List<ClientSearchItem>> SearchTermAsync(string term, CancellationToken cancellationToken)
    {
        var result = await _clientSearchRepository.SearchAsync(
            term, limit: SearchLimitPerTerm, cancellationToken: cancellationToken);
        if (result.Items.Count == 0)
        {
            result = await _clientSearchRepository.SearchByFuzzyNameAsync(
                term, limit: SearchLimitPerTerm, cancellationToken: cancellationToken);
        }

        return result.Items.ToList();
    }

    private static bool IsPlausibleMatch(string term, string? firstName, string? lastName, bool strict)
    {
        var parts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var names = new[] { firstName, lastName, $"{firstName} {lastName}".Trim() }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        return parts.Any(part => names.Any(name => NamesMatch(part, name!, strict)))
            || names.Any(name => NamesMatch(term, name!, strict));
    }

    // Strict mode (lowercase voice fallback) only accepts exact normalized equality or an
    // identical phonetic code — Levenshtein tolerance on arbitrary lowercase bigrams would
    // ground unrelated words ("will ich" vs the surname "Willich") into the prompt.
    private static bool NamesMatch(string left, string right, bool strict)
    {
        var normalizedLeft = NameMatching.Normalize(left);
        var normalizedRight = NameMatching.Normalize(right);
        if (normalizedLeft.Length == 0 || normalizedRight.Length == 0)
        {
            return false;
        }

        if (strict
            ? normalizedLeft == normalizedRight
            : NameMatching.FuzzyEquals(normalizedLeft, normalizedRight))
        {
            return true;
        }

        var phoneticLeft = ClientPhoneticTokenBuilder.Build(left);
        return phoneticLeft != null && phoneticLeft == ClientPhoneticTokenBuilder.Build(right);
    }

    private static string RenderBlock(List<(int? IdNumber, string? FirstName, string? LastName, bool Named)> grounded)
    {
        var sb = new StringBuilder();
        AppendSection(sb, BlockHeader, BlockInstruction, grounded.Where(entry => entry.Named).ToList());
        AppendSection(sb, SimilarBlockHeader, SimilarBlockInstruction, grounded.Where(entry => !entry.Named).ToList());
        return sb.ToString().TrimEnd();
    }

    private static void AppendSection(
        StringBuilder sb,
        string header,
        string instruction,
        List<(int? IdNumber, string? FirstName, string? LastName, bool Named)> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        sb.AppendLine(header);
        foreach (var entry in entries)
        {
            var idSuffix = entry.IdNumber.HasValue ? $" (#{entry.IdNumber.Value})" : string.Empty;
            sb.AppendLine($"- {entry.LastName}, {entry.FirstName}{idSuffix}");
        }

        sb.AppendLine(instruction);
    }

    [GeneratedRegex(@"[\p{L}][\p{L}'-]*", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
