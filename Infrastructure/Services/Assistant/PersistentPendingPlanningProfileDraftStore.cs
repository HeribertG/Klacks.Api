// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed store for planning-profile drafts paused between dialog turns, so a draft started with
/// start_planning_profile_setup survives a backend restart instead of living only in an in-memory
/// singleton. Each operation runs in its OWN service scope (via IServiceScopeFactory) rather than a
/// request-scoped DbContext, mirroring PersistentPendingCompanyRuleDraftStore: the chat pipeline can
/// touch the request context concurrently, so a shared context would race. The
/// IPendingPlanningProfileDraftStore interface is synchronous because callers (the planning-profile
/// skills) invoke it synchronously, so the short single-row repository calls are awaited via
/// GetAwaiter().GetResult(); ASP.NET Core has no synchronization context, so this cannot deadlock.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingPlanningProfileDraftStore : IPendingPlanningProfileDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentPendingPlanningProfileDraftStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Set(Guid userId, string conversationId, PlanningProfileDraft draft)
    {
        var now = DateTime.UtcNow;
        draft.ExpiresAtUtc = now.AddMinutes(PlanningProfileDraftDefaults.PendingDraftTtlMinutes);

        var row = new PendingPlanningProfileDraftRow
        {
            UserId = userId,
            ConversationId = conversationId,
            DraftJson = JsonSerializer.Serialize(draft, JsonOptions),
            ExpiresAtUtc = draft.ExpiresAtUtc
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingPlanningProfileDraftRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    public PlanningProfileDraft? Get(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingPlanningProfileDraftRepository>();

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

        return DeserializeDraft(row.DraftJson);
    }

    public void Clear(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingPlanningProfileDraftRepository>();
        repository.DeleteAsync(userId, conversationId).GetAwaiter().GetResult();
    }

    private static PlanningProfileDraft? DeserializeDraft(string? draftJson)
    {
        if (string.IsNullOrWhiteSpace(draftJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PlanningProfileDraft>(draftJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
