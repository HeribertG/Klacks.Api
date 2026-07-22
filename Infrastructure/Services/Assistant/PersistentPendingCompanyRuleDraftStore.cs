// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed store for company-rule drafts paused between dialog turns, so a draft started with
/// start_company_rule survives a backend restart instead of living only in an in-memory singleton. Each
/// operation runs in its OWN service scope (via IServiceScopeFactory) rather than a request-scoped
/// DbContext, mirroring PersistentPendingRecipeStore and PersistentPendingConfirmationStore: the chat
/// pipeline can touch the request context concurrently, so a shared context would race. The
/// IPendingCompanyRuleDraftStore interface is synchronous because callers (the company-rule skills)
/// invoke it synchronously, so the short single-row repository calls are awaited via
/// GetAwaiter().GetResult(); ASP.NET Core has no synchronization context, so this cannot deadlock.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingCompanyRuleDraftStore : IPendingCompanyRuleDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentPendingCompanyRuleDraftStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Set(Guid userId, string conversationId, CompanyRuleDraft draft)
    {
        var now = DateTime.UtcNow;
        draft.ExpiresAtUtc = now.AddMinutes(CompanyRuleDraftDefaults.PendingDraftTtlMinutes);

        var row = new PendingCompanyRuleDraftRow
        {
            UserId = userId,
            ConversationId = conversationId,
            DraftJson = JsonSerializer.Serialize(draft, JsonOptions),
            ExpiresAtUtc = draft.ExpiresAtUtc
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingCompanyRuleDraftRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    public CompanyRuleDraft? Get(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingCompanyRuleDraftRepository>();

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
        var repository = scope.ServiceProvider.GetRequiredService<IPendingCompanyRuleDraftRepository>();
        repository.DeleteAsync(userId, conversationId).GetAwaiter().GetResult();
    }

    private static CompanyRuleDraft? DeserializeDraft(string? draftJson)
    {
        if (string.IsNullOrWhiteSpace(draftJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CompanyRuleDraft>(draftJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
