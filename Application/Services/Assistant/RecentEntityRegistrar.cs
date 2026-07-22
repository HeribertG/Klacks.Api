// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records the entity a successful skill created or updated into the per-conversation recent-entity ring.
/// Runs the conservative <see cref="RecentEntityExtractor"/> against the skill result and persists a row
/// only when a single entity is unambiguously identifiable; anything else is silently skipped so the ring
/// never advertises a guessed entity. Called fire-and-forget-style (await + swallow) after skill execution.
/// </summary>
/// <param name="repository">Persists and bounds the per-conversation recent-entity ring.</param>
/// <param name="logger">Logger instance.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class RecentEntityRegistrar : IRecentEntityRegistrar
{
    private readonly IRecentEntityRepository _repository;
    private readonly ILogger<RecentEntityRegistrar> _logger;

    public RecentEntityRegistrar(
        IRecentEntityRepository repository,
        ILogger<RecentEntityRegistrar> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RegisterAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken = default)
    {
        if (context.UserId == Guid.Empty || string.IsNullOrWhiteSpace(context.SessionId))
        {
            return;
        }

        if (!RecentEntityExtractor.TryExtract(descriptor.Name, result, out var extracted) || extracted is null)
        {
            return;
        }

        var row = new RecentEntityRow
        {
            UserId = context.UserId,
            ConversationId = context.SessionId!,
            EntityType = extracted.EntityType,
            EntityId = extracted.EntityId,
            DisplayName = extracted.DisplayName,
            Action = extracted.Action,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(row, cancellationToken);

        _logger.LogDebug(
            "Recent entity registered for user {UserId} conversation {ConversationId}: {EntityType} {EntityId} ({Action}) via {SkillName}",
            context.UserId, context.SessionId, extracted.EntityType, extracted.EntityId, extracted.Action, descriptor.Name);
    }
}
