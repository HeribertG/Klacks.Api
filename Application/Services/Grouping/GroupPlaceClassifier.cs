// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a group represents a real geographic place (city/town/village) versus a non-place
/// node (region/canton container, qualification, team, ...). Uses the LLM default model with the group
/// name and its parent hierarchy as context; when the verdict is uncertain and a web search provider is
/// configured, it augments the prompt with a single web search before re-asking. Always degrades to
/// "not a place" on any error or malformed output (a false positive would feed a wrong customer
/// assignment downstream), so the caller can safely treat a low result as "leave it".
/// </summary>
/// <param name="llmRepository">Resolves the configured default LLM model.</param>
/// <param name="providerFactory">Resolves the provider that runs the chosen model.</param>
/// <param name="webSearchProviderFactory">Resolves the configured web search provider (may be none).</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Infrastructure.WebSearch;

namespace Klacks.Api.Application.Services.Grouping;

public class GroupPlaceClassifier : IGroupPlaceClassifier
{
    private const double AugmentLowerBound = 0.35;
    private const double AugmentUpperBound = 0.75;
    private const int MaxTokens = 1024;
    private const int MaxAttempts = 2;
    private const int WebSearchResults = 3;

    private const string SystemPrompt =
        "You classify whether an organizational group represents a real geographic place (a city, town " +
        "or village) versus a non-place node (a country/region/state/canton container, a qualification, " +
        "a team, or any administrative grouping). You are given the group name and its parent hierarchy " +
        "(outermost first); a real place typically sits under a region/state/canton. Respond with ONLY a " +
        "JSON object: {\"isPlace\": boolean, \"canonicalName\": string|null, \"region\": string|null, " +
        "\"confidence\": number between 0 and 1}. canonicalName is the official place name to geocode; " +
        "region is the state/canton/district if known. Be conservative: if it is not unambiguously a " +
        "city/town/village, set isPlace=false. confidence is how certain you are of the verdict.";

    private readonly ILLMRepository _llmRepository;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly WebSearchProviderFactory _webSearchProviderFactory;
    private readonly ILogger<GroupPlaceClassifier> _logger;

    public GroupPlaceClassifier(
        ILLMRepository llmRepository,
        ILLMProviderFactory providerFactory,
        WebSearchProviderFactory webSearchProviderFactory,
        ILogger<GroupPlaceClassifier> logger)
    {
        _llmRepository = llmRepository;
        _providerFactory = providerFactory;
        _webSearchProviderFactory = webSearchProviderFactory;
        _logger = logger;
    }

    public async Task<GroupPlaceClassification> ClassifyAsync(
        string groupName,
        IReadOnlyList<string> parentHierarchy,
        CancellationToken cancellationToken = default)
    {
        var model = await _llmRepository.GetDefaultModelAsync();
        if (model == null)
        {
            return GroupPlaceClassification.NotAPlace;
        }

        var provider = await _providerFactory.GetProviderForModelAsync(model.ModelId);
        if (provider == null)
        {
            return GroupPlaceClassification.NotAPlace;
        }

        var hierarchy = parentHierarchy.Count > 0 ? string.Join(" > ", parentHierarchy) : "(none)";
        var classification = await AskAsync(provider, model.ApiModelId, groupName, hierarchy, null, cancellationToken);

        if (classification.Confidence >= AugmentLowerBound && classification.Confidence < AugmentUpperBound)
        {
            var snippet = await TryWebSearchAsync(groupName, classification.Region, cancellationToken);
            if (!string.IsNullOrWhiteSpace(snippet))
            {
                classification = await AskAsync(provider, model.ApiModelId, groupName, hierarchy, snippet, cancellationToken);
            }
        }

        return classification;
    }

    private async Task<GroupPlaceClassification> AskAsync(
        ILLMProvider provider,
        string apiModelId,
        string groupName,
        string hierarchy,
        string? webContext,
        CancellationToken cancellationToken)
    {
        var message = $"Group name: \"{groupName}\"\nParent hierarchy (outermost first): {hierarchy}";
        if (!string.IsNullOrWhiteSpace(webContext))
        {
            message += $"\n\nWeb search context:\n{webContext}";
        }
        message += "\n\nClassify.";

        var request = new LLMProviderRequest
        {
            Message = message,
            SystemPrompt = SystemPrompt,
            ModelId = apiModelId,
            Temperature = 0.0,
            MaxTokens = MaxTokens
        };

        var last = GroupPlaceClassification.NotAPlace;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await provider.ProcessAsync(request, cancellationToken);
                var content = response.Content ?? string.Empty;
                if (!response.Success || string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning(
                        "[GroupPlaceClassifier] '{GroupName}' attempt={Attempt} empty (success={Success}, len={Len}).",
                        groupName, attempt, response.Success, content.Length);
                    continue;
                }

                last = GroupPlaceClassificationParser.Parse(content);
                _logger.LogInformation(
                    "[GroupPlaceClassifier] '{GroupName}' attempt={Attempt} len={Len} -> isPlace={IsPlace} confidence={Confidence} canonical={Canonical}",
                    groupName, attempt, content.Length, last.IsPlace, last.Confidence, last.CanonicalName);

                if (last.IsPlace && last.Confidence > 0)
                {
                    return last;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GroupPlaceClassifier] '{GroupName}' attempt={Attempt} threw.", groupName, attempt);
            }
        }

        return last;
    }

    private async Task<string?> TryWebSearchAsync(string groupName, string? region, CancellationToken cancellationToken)
    {
        try
        {
            var provider = await _webSearchProviderFactory.CreateAsync(cancellationToken);
            if (provider == null)
            {
                return null;
            }

            var query = string.IsNullOrWhiteSpace(region)
                ? $"Is \"{groupName}\" a city, town or village?"
                : $"Is \"{groupName}\" a city, town or village in {region}?";
            var result = await provider.SearchAsync(query, WebSearchResults, cancellationToken);
            if (result == null || !result.Success || result.Results.Count == 0)
            {
                return null;
            }

            return string.Join("\n", result.Results.Select(r => $"- {r.Title}: {r.Snippet}"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Web search augmentation failed for '{GroupName}'.", groupName);
            return null;
        }
    }
}
