// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Discovers candidate LLM providers from the curated catalog and (optionally) web search,
/// removes any that already exist, and tags each surviving candidate with a connectivity
/// status from a parallel "models"-endpoint test call.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DiscoverProvidersQueryHandler
    : IRequestHandler<DiscoverProvidersQuery, List<ProviderCandidateResource>>
{
    private readonly ILLMRepository _repository;
    private readonly IProviderWebDiscovery _webDiscovery;
    private readonly IProviderConnectivityTester _connectivityTester;
    private readonly ILogger<DiscoverProvidersQueryHandler> _logger;

    public DiscoverProvidersQueryHandler(
        ILLMRepository repository,
        IProviderWebDiscovery webDiscovery,
        IProviderConnectivityTester connectivityTester,
        ILogger<DiscoverProvidersQueryHandler> logger)
    {
        _repository = repository;
        _webDiscovery = webDiscovery;
        _connectivityTester = connectivityTester;
        _logger = logger;
    }

    public async Task<List<ProviderCandidateResource>> Handle(
        DiscoverProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetProvidersAsync();
        var seenIds = new HashSet<string>(
            existing.Select(p => p.ProviderId.ToLowerInvariant()));
        var seenUrls = new HashSet<string>(
            existing.Select(p => ProviderUrlHelper.NormalizeForComparison(p.BaseUrl)));

        var candidates = new List<ProviderCandidateResource>();

        foreach (var entry in KnownLlmProviderCatalog.Entries)
        {
            if (TryReserve(seenIds, seenUrls, entry.ProviderId, entry.BaseUrl))
            {
                candidates.Add(new ProviderCandidateResource
                {
                    ProviderId = entry.ProviderId,
                    ProviderName = entry.ProviderName,
                    BaseUrl = entry.BaseUrl,
                    ApiVersion = entry.ApiVersion,
                    RequiresApiKey = entry.RequiresApiKey,
                    DocsUrl = entry.DocsUrl,
                    Source = ProviderCandidateSource.Catalog,
                    Connectivity = ProviderConnectivityStatus.Unknown
                });
            }
        }

        var webCandidates = await _webDiscovery.DiscoverAsync(cancellationToken);
        foreach (var candidate in webCandidates)
        {
            if (TryReserve(seenIds, seenUrls, candidate.ProviderId, candidate.BaseUrl))
            {
                candidates.Add(candidate);
            }
        }

        await TestConnectivityAsync(candidates, cancellationToken);

        _logger.LogInformation(
            "Provider discovery produced {Count} candidates ({Catalog} catalog, {Web} web).",
            candidates.Count,
            candidates.Count(c => c.Source == ProviderCandidateSource.Catalog),
            candidates.Count(c => c.Source == ProviderCandidateSource.Web));

        return candidates;
    }

    private async Task TestConnectivityAsync(
        List<ProviderCandidateResource> candidates,
        CancellationToken cancellationToken)
    {
        var tests = candidates.Select(async candidate =>
        {
            candidate.Connectivity = await _connectivityTester.TestAsync(
                candidate.BaseUrl, null, cancellationToken);
        });

        await Task.WhenAll(tests);
    }

    private static bool TryReserve(
        HashSet<string> seenIds,
        HashSet<string> seenUrls,
        string providerId,
        string baseUrl)
    {
        var id = providerId.ToLowerInvariant();
        var url = ProviderUrlHelper.NormalizeForComparison(baseUrl);

        if (seenIds.Contains(id) || seenUrls.Contains(url))
        {
            return false;
        }

        seenIds.Add(id);
        seenUrls.Add(url);
        return true;
    }
}
