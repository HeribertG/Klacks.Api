// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects person-name candidates in the user message (capitalized token pairs,
/// hyphenated names and tokens following title markers like Herr/Frau/Mr/Mme), resolves
/// them via the client search repository, validates hits with the shared fuzzy name
/// matching and renders the grounding block with canonical spellings and visible id
/// numbers. Purely additive: failures are swallowed and leave the context untouched.
/// </summary>

using System.Text;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public partial class ClientNameCandidateGrounder : IEntityCandidateGrounder
{
    private const int MaxCandidateTerms = 3;
    private const int MaxGroundedEntities = 5;
    private const int SearchLimitPerTerm = 5;
    private const int MinTermLength = 2;

    private const string BlockHeader = "KNOWN_MATCHING_PEOPLE (deterministically resolved from this message):";
    private const string BlockInstruction =
        "When calling a tool for one of these people, copy EXACTLY this spelling into firstName/lastName. "
        + "If several entries could be meant, ask the user which one instead of picking silently.";

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
            if (terms.Count == 0)
            {
                return;
            }

            var grounded = new List<(int? IdNumber, string? FirstName, string? LastName)>();
            var seen = new HashSet<Guid>();

            foreach (var term in terms)
            {
                var result = await _clientSearchRepository.SearchAsync(
                    term, limit: SearchLimitPerTerm, cancellationToken: cancellationToken);

                foreach (var item in result.Items)
                {
                    if (!seen.Add(item.Id) || !IsPlausibleMatch(term, item.FirstName, item.LastName))
                    {
                        continue;
                    }

                    grounded.Add((item.IdNumber, item.FirstName, item.LastName));
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

    private static bool IsPlausibleMatch(string term, string? firstName, string? lastName)
    {
        var parts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var names = new[] { firstName, lastName, $"{firstName} {lastName}".Trim() }
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        return parts.Any(part => names.Any(name => NameMatching.FuzzyEquals(part, name!)))
            || names.Any(name => NameMatching.FuzzyEquals(term, name!));
    }

    private static string RenderBlock(List<(int? IdNumber, string? FirstName, string? LastName)> grounded)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader);
        foreach (var entry in grounded)
        {
            var idSuffix = entry.IdNumber.HasValue ? $" (#{entry.IdNumber.Value})" : string.Empty;
            sb.AppendLine($"- {entry.LastName}, {entry.FirstName}{idSuffix}");
        }

        sb.Append(BlockInstruction);
        return sb.ToString();
    }

    [GeneratedRegex(@"[\p{L}][\p{L}'-]*", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
