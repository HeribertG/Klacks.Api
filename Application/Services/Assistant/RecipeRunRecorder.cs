// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Best-effort recipe-run telemetry (W1.5). Every write runs in its OWN service scope, mirroring
/// RecipeEngineService and PersistentPendingRecipeStore: the chat pipeline launches fire-and-forget
/// tasks that touch the request-scoped DataBaseContext concurrently, so a telemetry write on the
/// shared context would race with them. All failures are logged and swallowed — telemetry must never
/// break the turn it describes.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class RecipeRunRecorder : IRecipeRunRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecipeRunRecorder> _logger;

    public RecipeRunRecorder(IServiceScopeFactory scopeFactory, ILogger<RecipeRunRecorder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<RecipeRunHandle?> BeginOrResumeAsync(
        string recipeName,
        Guid userId,
        string conversationId,
        Guid? turnId,
        int stepIndex,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRunRepository>();
            var now = DateTime.UtcNow;

            await repository.ExpireStaleRunsAsync(
                userId, conversationId, now.AddMinutes(-RecipeEngineDefaults.PendingRecipeTtlMinutes), cancellationToken);

            var existing = await repository.FindRunningAsync(userId, conversationId, recipeName, cancellationToken);
            if (existing != null)
            {
                existing.TurnIdsJson = AppendTurnId(existing.TurnIdsJson, turnId);
                existing.LastStep = Math.Max(existing.LastStep, stepIndex);
                existing.UpdateTime = now;
                await repository.UpdateAsync(existing, cancellationToken);
                return new RecipeRunHandle(existing.Id, recipeName, userId, conversationId);
            }

            var run = new RecipeRun
            {
                Id = Guid.NewGuid(),
                RecipeName = recipeName,
                UserId = userId,
                ConversationId = conversationId,
                Status = RecipeRunStatus.Running,
                LastStep = stepIndex,
                TurnIdsJson = AppendTurnId("[]", turnId),
                CreateTime = now,
                UpdateTime = now
            };
            await repository.AddAsync(run, cancellationToken);
            return new RecipeRunHandle(run.Id, recipeName, userId, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe-run begin/resume telemetry failed for {Recipe} in conversation {Conversation}",
                recipeName, conversationId);
            return null;
        }
    }

    public async Task UpdateStepAsync(RecipeRunHandle handle, int stepIndex, CancellationToken cancellationToken = default)
    {
        if (stepIndex < 0)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRunRepository>();
            var run = await repository.GetByIdAsync(handle.RunId, cancellationToken);
            if (run == null || run.Status != RecipeRunStatus.Running)
            {
                return;
            }

            run.LastStep = Math.Max(run.LastStep, stepIndex);
            run.UpdateTime = DateTime.UtcNow;
            await repository.UpdateAsync(run, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe-run step telemetry failed for run {RunId}", handle.RunId);
        }
    }

    public async Task CompleteAsync(RecipeRunHandle handle, CancellationToken cancellationToken = default)
    {
        await CloseAsync(handle, RecipeRunStatus.Completed, null, cancellationToken);
    }

    public async Task AbortAsync(RecipeRunHandle handle, string reason, CancellationToken cancellationToken = default)
    {
        await CloseAsync(handle, RecipeRunStatus.Aborted, reason, cancellationToken);
    }

    public async Task AbortRunningAsync(
        string recipeName,
        Guid userId,
        string conversationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRunRepository>();
            var run = await repository.FindRunningAsync(userId, conversationId, recipeName, cancellationToken);
            if (run == null)
            {
                return;
            }

            run.Status = RecipeRunStatus.Aborted;
            run.AbortReason = Truncate(reason);
            run.UpdateTime = DateTime.UtcNow;
            await repository.UpdateAsync(run, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe-run abort telemetry failed for {Recipe} in conversation {Conversation}",
                recipeName, conversationId);
        }
    }

    private async Task CloseAsync(
        RecipeRunHandle handle, RecipeRunStatus status, string? reason, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRecipeRunRepository>();
            var run = await repository.GetByIdAsync(handle.RunId, cancellationToken);
            if (run == null || run.Status != RecipeRunStatus.Running)
            {
                return;
            }

            run.Status = status;
            run.AbortReason = reason == null ? null : Truncate(reason);
            run.UpdateTime = DateTime.UtcNow;
            await repository.UpdateAsync(run, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe-run close telemetry failed for run {RunId}", handle.RunId);
        }
    }

    private static string AppendTurnId(string turnIdsJson, Guid? turnId)
    {
        if (!turnId.HasValue)
        {
            return turnIdsJson;
        }

        var ids = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(turnIdsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<Guid>>(turnIdsJson, JsonOptions);
                if (parsed != null)
                {
                    ids.AddRange(parsed);
                }
            }
            catch (JsonException)
            {
                ids.Clear();
            }
        }

        if (!ids.Contains(turnId.Value))
        {
            ids.Add(turnId.Value);
        }

        return JsonSerializer.Serialize(ids);
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }
}
