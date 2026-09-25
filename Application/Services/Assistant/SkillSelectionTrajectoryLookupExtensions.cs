// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one rule by which every user-facing turn endpoint (correction, thumbs, correction-menu options) finds
/// the trajectory of the turn a user means. A turn id names the turn exactly and is the only key once the
/// client sends one; it never falls back to the message hash, because an id that resolves to nothing (turn
/// not persisted yet, no trajectory captured) says nothing about which of two same-text turns was meant, and
/// the hash would pick the newest - the very ambiguity the id removes. Only a client without an id (older
/// Ui, history messages, turns stopped before stream_start) uses the hash. The owner is part of both lookups.
/// </summary>
/// <param name="repository">Trajectory store, queried by owner plus turn id or plus message hash</param>
/// <param name="userId">Owner of the turn; a trajectory of anyone else is never returned</param>
/// <param name="turnId">Server-assigned id of the turn, or null when the client sent none</param>
/// <param name="userMessage">Raw user message, hashed by MessageNormalizer for the fallback lookup</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public static class SkillSelectionTrajectoryLookupExtensions
{
    public static Task<SkillSelectionTrajectory?> FindByTurnIdOrMessageAsync(
        this ISkillSelectionTrajectoryRepository repository,
        string userId,
        Guid? turnId,
        string userMessage,
        CancellationToken cancellationToken)
    {
        return turnId.HasValue
            ? repository.FindByUserAndTurnIdAsync(userId, turnId.Value, cancellationToken)
            : repository.FindMostRecentByUserAndHashAsync(userId, MessageNormalizer.Hash(userMessage), cancellationToken);
    }
}
