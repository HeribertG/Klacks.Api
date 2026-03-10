// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.WebSearch;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("web_search")]
public class WebSearchSkill : BaseSkillImplementation
{
    private readonly WebSearchProviderFactory _providerFactory;

    public WebSearchSkill(WebSearchProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var query = GetRequiredString(parameters, "query");
        var maxResults = GetParameter<int?>(parameters, "maxResults") ?? 5;

        var provider = await _providerFactory.CreateAsync(cancellationToken);
        if (provider == null)
        {
            return SkillResult.Error(
                "Web search is not configured. Set WEB_SEARCH_PROVIDER and WEB_SEARCH_API_KEY in settings.");
        }

        var result = await provider.SearchAsync(query, maxResults, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage ?? "Web search failed.");
        }

        if (result.Results.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { Query = query, ResultCount = 0, Results = Array.Empty<object>() },
                $"No results found for: {query}");
        }

        var formattedResults = result.Results.Select((r, i) => new
        {
            Index = i + 1,
            r.Title,
            r.Snippet,
            r.Url
        }).ToList();

        return SkillResult.SuccessResult(
            new { Query = query, ResultCount = formattedResults.Count, Results = formattedResults },
            $"Found {formattedResults.Count} results for: {query}");
    }
}
