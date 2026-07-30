// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the requesting user's own goal candidates for the Phase 2 read-only inbox, newest first.
/// When no status filter is given, only non-terminal candidates (Shadow/Proposed) are returned so
/// decided or expired candidates fall out of view without a client-side filter. Maps each entity to
/// GoalCandidateDto, which deliberately omits OwnerPermissionsCsv — that field is an internal
/// authorization snapshot, not something the client needs or should see. The dto also resolves the
/// candidate's goal type against GoalTypeCatalog so the client receives the i18n keys it renders the
/// proposal from; a candidate created before the catalogue existed resolves to no keys and the client
/// falls back to its stored English text.
/// </summary>
/// <param name="goalCandidateRepository">Persistence of GoalCandidate entities.</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetGoalCandidatesQueryHandler : IRequestHandler<GetGoalCandidatesQuery, IReadOnlyList<GoalCandidateDto>>
{
    private readonly IGoalCandidateRepository _goalCandidateRepository;

    public GetGoalCandidatesQueryHandler(IGoalCandidateRepository goalCandidateRepository)
    {
        _goalCandidateRepository = goalCandidateRepository;
    }

    public async Task<IReadOnlyList<GoalCandidateDto>> Handle(GetGoalCandidatesQuery request, CancellationToken cancellationToken)
    {
        var take = NormalizeTake(request.Take);
        var candidates = await _goalCandidateRepository.GetForUserAsync(request.UserId, request.Status, take, cancellationToken);
        return candidates.Select(ToDto).ToList();
    }

    private static int NormalizeTake(int? take)
    {
        if (take is not int value || value <= 0)
        {
            return GoalCandidateInboxDefaults.DefaultListTake;
        }

        return Math.Min(value, GoalCandidateInboxDefaults.MaxListTake);
    }

    private static GoalCandidateDto ToDto(GoalCandidate candidate)
    {
        var definition = GoalTypeCatalog.Find(candidate.GoalType);

        return new GoalCandidateDto
        {
            Id = candidate.Id,
            GoalType = candidate.GoalType,
            TitleKey = definition?.TitleKey,
            RationaleKey = definition?.RationaleKey,
            RationaleParams = ParseRationaleParams(candidate.RationaleParamsJson),
            Title = candidate.Title,
            Rationale = candidate.Rationale,
            Confidence = candidate.Confidence,
            SignalSource = candidate.SignalSource,
            Status = candidate.Status,
            CreatedUtc = candidate.CreateTime,
            DecidedUtc = candidate.DecidedUtc,
            PlanId = candidate.PlanId
        };
    }

    private static IReadOnlyDictionary<string, string>? ParseRationaleParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (JsonException)
        {
            // A candidate whose parameters cannot be read still lists; the frontend then renders the
            // catalogue text without interpolation rather than the row disappearing from the inbox.
            return null;
        }
    }
}
