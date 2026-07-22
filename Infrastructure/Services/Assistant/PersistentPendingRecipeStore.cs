// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed store for recipes paused on an ask step, so a paused guided flow survives a backend
/// restart instead of living only in an in-memory singleton. Each operation runs in its OWN service
/// scope (via IServiceScopeFactory) rather than a request-scoped DbContext: the chat pipeline launches
/// fire-and-forget tasks that touch the request context concurrently, and Persist/Clear run
/// synchronously on that same request thread, so a shared context would race ("second operation started
/// on this context"). A fresh scope per call is fully isolated, mirroring RecipeEngineService's own
/// mitigation. The IPendingRecipeStore interface is synchronous because the caller (LLMService) invokes
/// it synchronously, so the short single-row repository calls are awaited via GetAwaiter().GetResult();
/// ASP.NET Core has no synchronization context, so this cannot deadlock.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingRecipeStore : IPendingRecipeStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentPendingRecipeStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Save(PendingRecipe pending)
    {
        var now = DateTime.UtcNow;
        pending.ExpiresAtUtc = now.AddMinutes(RecipeEngineDefaults.PendingRecipeTtlMinutes);

        var row = new PendingRecipeRow
        {
            UserId = pending.UserId,
            ConversationId = pending.ConversationId,
            RecipeName = pending.RecipeName,
            StepIndex = pending.StepIndex,
            SlotsJson = JsonSerializer.Serialize(pending.Slots, JsonOptions),
            ExpiresAtUtc = pending.ExpiresAtUtc,
            AwaitingConfirmation = pending.AwaitingConfirmation,
            CaptureRewindUsed = pending.CaptureRewindUsed
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingRecipeRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    public PendingRecipe? Peek(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingRecipeRepository>();

        var row = repository.GetAsync(userId, conversationId).GetAwaiter().GetResult();
        if (row == null)
        {
            return null;
        }

        if (row.ExpiresAtUtc < DateTime.UtcNow)
        {
            repository.DeleteAsync(userId, conversationId).GetAwaiter().GetResult();
            return null;
        }

        return new PendingRecipe
        {
            UserId = row.UserId,
            ConversationId = row.ConversationId,
            RecipeName = row.RecipeName,
            StepIndex = row.StepIndex,
            Slots = DeserializeSlots(row.SlotsJson),
            ExpiresAtUtc = row.ExpiresAtUtc,
            AwaitingConfirmation = row.AwaitingConfirmation,
            CaptureRewindUsed = row.CaptureRewindUsed
        };
    }

    public void Clear(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingRecipeRepository>();
        repository.DeleteAsync(userId, conversationId).GetAwaiter().GetResult();
    }

    private static Dictionary<string, string> DeserializeSlots(string? slotsJson)
    {
        if (string.IsNullOrWhiteSpace(slotsJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(slotsJson, JsonOptions);
            return parsed == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
