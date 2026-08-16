// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Counts, per city, how many clients of the requested entity type would be caught by a new location
/// group there. Reuses CustomerGroupingPlanner's address-selection and name-uniqueness rules so the
/// count matches what propose_grouping would actually assign once such a group exists. Every number
/// here is computed, never guessed by the caller.
/// </summary>
/// <param name="clientRepository">Loads clients with their addresses for the requested entity type</param>
/// <param name="groupRepository">Loads existing groups, to exclude cities that already have a matching anchor</param>

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class EvaluateLocationGroupCandidatesQueryHandler
    : IRequestHandler<EvaluateLocationGroupCandidatesQuery, LocationGroupCandidatesResult>
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;

    public EvaluateLocationGroupCandidatesQueryHandler(
        IClientRepository clientRepository, IGroupRepository groupRepository)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
    }

    public async Task<LocationGroupCandidatesResult> Handle(
        EvaluateLocationGroupCandidatesQuery request, CancellationToken cancellationToken)
    {
        var groups = (await _groupRepository.List())
            .Where(g => !g.IsDeleted)
            .ToList();
        var groupsByUniqueName = CustomerGroupingPlanner.BuildUniqueNameIndex(groups);

        var clients = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            request.EntityType, cancellationToken);

        var withoutUsableAddress = 0;
        var cityCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var client in clients)
        {
            var address = CustomerGroupingPlanner.SelectPreferredAddress(client, CustomerGroupingPlanner.HasCity);
            if (address == null)
            {
                withoutUsableAddress++;
                continue;
            }

            var city = address.City.Trim();
            cityCounts[city] = cityCounts.GetValueOrDefault(city) + 1;
        }

        var candidates = new List<LocationGroupCandidate>();
        var nearThreshold = new List<LocationGroupCandidate>();
        var inExistingGroup = 0;

        foreach (var (city, count) in cityCounts)
        {
            if (groupsByUniqueName.ContainsKey(city))
            {
                inExistingGroup += count;
                continue;
            }

            var bucket = new LocationGroupCandidate(city, count, count >= GroupingAdvisoryDefaults.MinViableGroupSize);
            (bucket.IsViable ? candidates : nearThreshold).Add(bucket);
        }

        candidates = candidates.OrderByDescending(c => c.ClientCount).ToList();
        nearThreshold = nearThreshold.OrderByDescending(c => c.ClientCount).ToList();

        return new LocationGroupCandidatesResult(
            EntityType: request.EntityType.ToString(),
            Candidates: candidates,
            NearThresholdCandidates: nearThreshold,
            ClientsWithoutUsableAddress: withoutUsableAddress,
            ClientsInExistingLocationGroup: inExistingGroup,
            Recommendation: BuildRecommendation(candidates, nearThreshold, withoutUsableAddress));
    }

    private static string BuildRecommendation(
        IReadOnlyList<LocationGroupCandidate> candidates,
        IReadOnlyList<LocationGroupCandidate> nearThreshold,
        int withoutUsableAddress)
    {
        if (candidates.Count == 0 && nearThreshold.Count == 0)
        {
            return withoutUsableAddress > 0
                ? $"No city has any client without an existing matching group; {withoutUsableAddress} client(s) have no usable address city."
                : "No city has enough clients without an existing matching group to justify a new location group.";
        }

        var parts = new List<string>();
        if (candidates.Count > 0)
        {
            var listed = string.Join(", ", candidates.Select(c => $"{c.City}: {c.ClientCount}"));
            parts.Add($"{candidates.Count} cit{(candidates.Count == 1 ? "y" : "ies")} justify a new location group ({listed})");
        }

        if (nearThreshold.Count > 0)
        {
            var listed = string.Join(", ", nearThreshold.Select(c => $"{c.City}: {c.ClientCount}"));
            parts.Add($"{nearThreshold.Count} fall short of the minimum ({listed})");
        }

        if (withoutUsableAddress > 0)
        {
            parts.Add($"{withoutUsableAddress} client(s) have no usable address city");
        }

        return string.Join("; ", parts) + ". Creating a group is a separate, manual step (create_group).";
    }
}
