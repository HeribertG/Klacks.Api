// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Counts, per qualification, how many clients of the requested entity type currently hold it (valid
/// today per the company clock), using the exact same validity predicate as
/// ClientSearchRepository's qualification filter so the count matches what fill_group_by_criteria
/// would actually add. Qualifications overlap, so a client can contribute to several buckets — every
/// number here is computed, never guessed by the caller.
/// </summary>
/// <param name="clientRepository">Loads clients with their qualifications for the requested entity type</param>
/// <param name="groupRepository">Loads existing groups, to flag qualifications that already have a matching group</param>
/// <param name="mediator">Loads the qualification master list, for display names</param>
/// <param name="companyClock">Resolves the company's current date, for qualification validity</param>

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class EvaluateGroupingByQualificationQueryHandler
    : IRequestHandler<EvaluateGroupingByQualificationQuery, QualificationGroupCandidatesResult>
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public EvaluateGroupingByQualificationQueryHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IMediator mediator,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _mediator = mediator;
        _companyClock = companyClock;
    }

    public async Task<QualificationGroupCandidatesResult> Handle(
        EvaluateGroupingByQualificationQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken));

        var groups = (await _groupRepository.List())
            .Where(g => !g.IsDeleted)
            .ToList();
        var groupsByUniqueName = CustomerGroupingPlanner.BuildUniqueNameIndex(groups);

        var qualificationsById = (await _mediator.Send(new ListQuery(), cancellationToken))
            .ToDictionary(q => q.Id);

        var clients = await _clientRepository.GetByTypeWithQualificationsAsync(
            request.EntityType, cancellationToken);

        var withoutValidQualification = 0;
        var qualificationCounts = new Dictionary<Guid, int>();

        foreach (var client in clients)
        {
            var validQualificationIds = client.Qualifications
                .Where(q => !q.IsDeleted
                    && (q.ValidFrom == null || q.ValidFrom <= today)
                    && (q.ValidUntil == null || q.ValidUntil >= today))
                .Select(q => q.QualificationId)
                .Distinct()
                .ToList();

            if (validQualificationIds.Count == 0)
            {
                withoutValidQualification++;
                continue;
            }

            foreach (var qualificationId in validQualificationIds)
            {
                qualificationCounts[qualificationId] = qualificationCounts.GetValueOrDefault(qualificationId) + 1;
            }
        }

        var candidates = new List<QualificationGroupCandidate>();
        var nearThreshold = new List<QualificationGroupCandidate>();
        var alreadyCovered = new List<string>();

        foreach (var (qualificationId, count) in qualificationCounts)
        {
            var name = qualificationsById.TryGetValue(qualificationId, out var qualification)
                ? QualificationResolver.DisplayName(qualification)
                : qualificationId.ToString();

            if (groupsByUniqueName.ContainsKey(name.Trim()))
            {
                alreadyCovered.Add(name);
                continue;
            }

            var bucket = new QualificationGroupCandidate(
                qualificationId, name, count, count >= GroupingAdvisoryDefaults.MinViableGroupSize);
            (bucket.IsViable ? candidates : nearThreshold).Add(bucket);
        }

        candidates = candidates.OrderByDescending(c => c.ClientCount).ToList();
        nearThreshold = nearThreshold.OrderByDescending(c => c.ClientCount).ToList();

        return new QualificationGroupCandidatesResult(
            EntityType: request.EntityType.ToString(),
            TotalClientsEvaluated: clients.Count,
            Candidates: candidates,
            NearThresholdCandidates: nearThreshold,
            ClientsWithoutValidQualification: withoutValidQualification,
            QualificationsAlreadyCovered: alreadyCovered,
            Recommendation: BuildRecommendation(clients.Count, candidates, nearThreshold, withoutValidQualification));
    }

    private static string BuildRecommendation(
        int totalClients,
        IReadOnlyList<QualificationGroupCandidate> candidates,
        IReadOnlyList<QualificationGroupCandidate> nearThreshold,
        int withoutValidQualification)
    {
        if (candidates.Count == 0 && nearThreshold.Count == 0)
        {
            return withoutValidQualification > 0
                ? $"No qualification without an existing matching group has any client; {withoutValidQualification} of {totalClients} client(s) hold no qualification valid today."
                : "No qualification without an existing matching group has enough clients to justify a new group.";
        }

        var parts = new List<string>();
        if (candidates.Count > 0)
        {
            var listed = string.Join(", ", candidates.Select(c => $"{c.QualificationName}: {c.ClientCount}"));
            parts.Add($"{candidates.Count} qualification{(candidates.Count == 1 ? "" : "s")} justify a new group ({listed})");
        }

        if (nearThreshold.Count > 0)
        {
            var listed = string.Join(", ", nearThreshold.Select(c => $"{c.QualificationName}: {c.ClientCount}"));
            parts.Add($"{nearThreshold.Count} fall short of the minimum ({listed})");
        }

        if (withoutValidQualification > 0)
        {
            parts.Add($"{withoutValidQualification} of {totalClients} client(s) hold no qualification valid today");
        }

        parts.Add("clients can hold more than one qualification, so counts are not a partition of the population");

        return string.Join("; ", parts) + ". Creating a group is a separate, manual step (create_group).";
    }
}
