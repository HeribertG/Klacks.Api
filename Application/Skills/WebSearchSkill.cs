// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.WebSearch;

namespace Klacks.Api.Application.Skills;

public class WebSearchSkill : BaseSkill
{
    private readonly WebSearchProviderFactory _providerFactory;

    public override string Name => "web_search";

    public override string Description =>
        "Searches the internet for information. Use this to look up email provider settings " +
        "(SMTP/IMAP server, ports, SSL), technical documentation, or any other factual information.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("query", "Search query string", SkillParameterType.String, Required: true),
        new SkillParameter("maxResults", "Maximum number of results to return (default: 5)", SkillParameterType.String, Required: false)
    };

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
        var maxResultsStr = GetParameter<string>(parameters, "maxResults", "5");
        var maxResults = int.TryParse(maxResultsStr, out var parsed) ? parsed : 5;

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
