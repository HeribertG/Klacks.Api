// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the skills that were offered to the model in one captured turn and turns them into the options
/// of the correction menu (C1). The turn is found by its turn id when the client sends one (exact, survives
/// stop-and-resend of the same text), otherwise by the raw message, which is hashed here rather than in the
/// browser, so MessageNormalizer stays the single source of the utterance key and no user text ever travels in a
/// URL. The lookup is scoped to the caller, so a turn of another user is invisible instead of refused:
/// the same sentence is typed by many users, and whose turn was captured last must not decide whether a
/// correction menu opens. Always-on plumbing and the skill the model actually chose are dropped: neither
/// answers "which skill should it have been", and an expected_skill naming plumbing would send the
/// description sharpener after a tool no user ever wants. Plumbing is recognised twice - by the recorded
/// provenance and by the skill's own always-on flag - because a turn captured before the provenance
/// column existed carries no source, and a candidate without one would otherwise reach the menu.
/// </summary>
/// <param name="trajectories">Trajectory store, queried by caller id and turn id or utterance hash</param>
/// <param name="skillCache">Enabled skills of every agent, used for the description and the always-on
/// flag of an option</param>
/// <param name="logger">Logger for the lookup that found no captured turn of this caller</param>

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
    private readonly ILogger<GetTurnOptionsQueryHandler> _logger;

    public GetTurnOptionsQueryHandler(
        ISkillSelectionTrajectoryRepository trajectories,
        ISkillCacheService skillCache,
        ILogger<GetTurnOptionsQueryHandler> logger)
    {
        _trajectories = trajectories;
        _skillCache = skillCache;
        _logger = logger;
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

        var trajectory = await _trajectories.FindByTurnIdOrMessageAsync(
            request.UserId, request.TurnId, request.UserMessage, cancellationToken);

        if (trajectory == null)
        {
            _logger.LogInformation(
                "Turn options requested for user {UserId} but no matching trajectory was found (turn {TurnId}, hash {Hash})",
                request.UserId, request.TurnId, MessageNormalizer.Hash(request.UserMessage));
            return new TurnOptionsResult { Outcome = TurnOptionsOutcome.NotFound };
        }

        var catalogue = await LoadCatalogueAsync(cancellationToken);

        return new TurnOptionsResult
        {
            Outcome = TurnOptionsOutcome.Found,
            Options = BuildOptions(trajectory, catalogue)
        };
    }

    // The cache holds the enabled skills of every agent, so one name can arrive more than once. The
    // always-on flag is therefore folded with OR: a name that is plumbing for any agent stays plumbing
    // here, where a last-one-wins entry would let it back into the menu.
    private async Task<Dictionary<string, TurnOptionSkill>> LoadCatalogueAsync(CancellationToken cancellationToken)
    {
        var skills = await _skillCache.GetAllEnabledSkillsAsync(cancellationToken);
        var catalogue = new Dictionary<string, TurnOptionSkill>(StringComparer.Ordinal);

        foreach (var skill in skills)
        {
            var alwaysOn = skill.AlwaysOn
                || (catalogue.TryGetValue(skill.Name, out var known) && known.AlwaysOn);
            catalogue[skill.Name] = new TurnOptionSkill(skill.Description, alwaysOn);
        }

        return catalogue;
    }

    // The provenance string alone is not enough: it was added with the capture of W1.6, so a turn
    // recorded before it carries no source at all, and the skill's own flag is what still identifies
    // plumbing in such a row.
    private static List<TurnOptionDto> BuildOptions(
        SkillSelectionTrajectory trajectory, IReadOnlyDictionary<string, TurnOptionSkill> catalogue)
    {
        return Deserialize(trajectory.KnowledgeIndexCandidatesJson)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Where(candidate => !string.Equals(
                candidate.Source, nameof(ToolsetSkillSource.AlwaysOn), StringComparison.Ordinal))
            .Where(candidate => !IsAlwaysOn(candidate.Name!, catalogue))
            .Where(candidate => !string.Equals(
                candidate.Name, trajectory.LlmChosenSkill, StringComparison.Ordinal))
            .OrderBy(candidate => candidate.Rank)
            .Take(TurnOptionsDefaults.MaxOptions)
            .Select(candidate => new TurnOptionDto
            {
                SkillName = candidate.Name!,
                DisplayName = SkillNameHumanizer.ToDisplayName(candidate.Name),
                Description = catalogue.TryGetValue(candidate.Name!, out var skill)
                    ? skill.Description
                    : string.Empty
            })
            .ToList();
    }

    private static bool IsAlwaysOn(string name, IReadOnlyDictionary<string, TurnOptionSkill> catalogue) =>
        catalogue.TryGetValue(name, out var skill) && skill.AlwaysOn;

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

    private sealed record TurnOptionSkill(string Description, bool AlwaysOn);
}
