// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Confirms which answer-name candidates denote real clients: exact client search per candidate,
/// then a strict first-plus-last-name match (both candidate tokens must strictly match the
/// client's given and family name in either order — no fuzzy repository fallback, no phonetics).
/// Resolution failures resolve to "not a client name" because the grounding evaluator must
/// prefer silence over a false finding. Resolved names are compared in memory only and are never
/// persisted by the caller's lesson path.
/// </summary>
/// <param name="clientSearchRepository">Exact client search used to look up each candidate.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Grounding;

namespace Klacks.Api.Application.Services.Assistant;

public class AnswerGroundingNameResolver : IAnswerGroundingNameResolver
{
    private const int SearchLimitPerCandidate = 3;

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly ILogger<AnswerGroundingNameResolver> _logger;

    public AnswerGroundingNameResolver(
        IClientSearchRepository clientSearchRepository,
        ILogger<AnswerGroundingNameResolver> logger)
    {
        _clientSearchRepository = clientSearchRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ResolveClientNamesAsync(
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken = default)
    {
        var resolved = new List<string>();
        foreach (var candidate in candidates)
        {
            try
            {
                if (await ResolvesToClientAsync(candidate, cancellationToken))
                {
                    resolved.Add(candidate);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Answer grounding name resolution failed for a candidate; treating it as unresolved");
            }
        }

        return resolved;
    }

    private async Task<bool> ResolvesToClientAsync(string candidate, CancellationToken cancellationToken)
    {
        var candidateTokens = AnswerNameMatching.Tokenize(candidate);
        if (candidateTokens.Length != 2)
        {
            return false;
        }

        var result = await _clientSearchRepository.SearchAsync(
            candidate, limit: SearchLimitPerCandidate, cancellationToken: cancellationToken);

        return result.Items.Any(item => MatchesFirstAndLastName(candidateTokens, item.FirstName, item.LastName));
    }

    private static bool MatchesFirstAndLastName(string[] candidateTokens, string? firstName, string? lastName)
    {
        var first = AnswerNameMatching.Normalize(firstName);
        var last = AnswerNameMatching.Normalize(lastName);
        if (first.Length == 0 || last.Length == 0)
        {
            return false;
        }

        return (AnswerNameMatching.StrictEquals(candidateTokens[0], first) && AnswerNameMatching.StrictEquals(candidateTokens[1], last))
            || (AnswerNameMatching.StrictEquals(candidateTokens[0], last) && AnswerNameMatching.StrictEquals(candidateTokens[1], first));
    }
}
