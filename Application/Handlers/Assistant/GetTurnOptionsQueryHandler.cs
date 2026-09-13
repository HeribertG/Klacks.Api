// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the skills that were offered to the model in one captured turn and turns them into the options
/// of the correction menu (C1). The raw message is hashed here rather than in the browser, so
/// MessageNormalizer stays the single source of the utterance key and no user text ever travels in a
/// URL. The lookup is scoped to the caller, so a turn of another user is invisible instead of refused:
/// the same sentence is typed by many users, and whose turn was captured last must not decide whether a
/// correction menu opens. Always-on plumbing and the skill the model actually chose are dropped: neither
/// answers "which skill should it have been", and an expected_skill naming plumbing would send the
/// description sharpener after a tool no user ever wants.
/// </summary>
/// <param name="trajectories">Trajectory store, queried by caller id and utterance hash</param>
/// <param name="skillCache">Enabled skills of every agent, used for the description of an option</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetTurnOptionsQueryHandler : IRequestHandler<GetTurnOptionsQuery, TurnOptionsResult>
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ISkillSelectionTrajectoryRepository _trajectories;
    private readonly ISkillCacheService _skillCache;

    public GetTurnOptionsQueryHandler(
        ISkillSelectionTrajectoryRepository trajectories,
        ISkillCacheService skillCache)
    {
        _trajectories = trajectories;
        _skillCache = skillCache;
    }

    public async Task<TurnOptionsResult> Handle(GetTurnOptionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId must be provided.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            throw new ArgumentException("UserMessage must be provided.", nameof(request));
        }

        var hash = MessageNormalizer.Hash(request.UserMessage);
        var trajectory = await _trajectories.FindMostRecentByUserAndHashAsync(
            request.UserId, hash, cancellationToken);

        if (trajectory == null)
        {
            return new TurnOptionsResult { Outcome = TurnOptionsOutcome.NotFound };
        }

        var descriptions = await LoadDescriptionsAsync(cancellationToken);

        return new TurnOptionsResult
        {
            Outcome = TurnOptionsOutcome.Found,
            Options = BuildOptions(trajectory, descriptions)
        };
    }

    private async Task<Dictionary<string, string>> LoadDescriptionsAsync(CancellationToken cancellationToken)
    {
        var skills = await _skillCache.GetAllEnabledSkillsAsync(cancellationToken);
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var skill in skills)
        {
            descriptions[skill.Name] = skill.Description;
        }

        return descriptions;
    }

    private static List<TurnOptionDto> BuildOptions(
        SkillSelectionTrajectory trajectory, IReadOnlyDictionary<string, string> descriptions)
    {
        return Deserialize(trajectory.KnowledgeIndexCandidatesJson)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Where(candidate => !string.Equals(
                candidate.Source, nameof(ToolsetSkillSource.AlwaysOn), StringComparison.Ordinal))
            .Where(candidate => !string.Equals(
                candidate.Name, trajectory.LlmChosenSkill, StringComparison.Ordinal))
            .OrderBy(candidate => candidate.Rank)
            .Take(TurnOptionsDefaults.MaxOptions)
            .Select(candidate => new TurnOptionDto
            {
                SkillName = candidate.Name!,
                DisplayName = SkillNameHumanizer.ToDisplayName(candidate.Name),
                Description = descriptions.TryGetValue(candidate.Name!, out var description)
                    ? description
                    : string.Empty
            })
            .ToList();
    }

    // A trajectory row is telemetry, not a contract: a column that cannot be parsed must cost the menu
    // its options, never the whole request.
    private static List<TurnToolsetCandidate> Deserialize(string? candidatesJson)
    {
        if (string.IsNullOrWhiteSpace(candidatesJson))
        {
            return new List<TurnToolsetCandidate>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<TurnToolsetCandidate>>(candidatesJson, SerializerOptions)
                ?? new List<TurnToolsetCandidate>();
        }
        catch (JsonException)
        {
            return new List<TurnToolsetCandidate>();
        }
    }
}
