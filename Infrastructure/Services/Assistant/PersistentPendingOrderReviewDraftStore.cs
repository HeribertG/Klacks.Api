// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF-backed store for order-review drafts paused between dialog turns, so a draft started with
/// start_order_review survives a backend restart instead of living only in an in-memory
/// singleton. Each operation runs in its OWN service scope (via IServiceScopeFactory) rather than a
/// request-scoped DbContext, mirroring PersistentPendingCompanyRuleDraftStore: the chat pipeline can
/// touch the request context concurrently, so a shared context would race. The
/// IPendingOrderReviewDraftStore interface is synchronous because callers (the order-review
/// skills) invoke it synchronously, so the short single-row repository calls are awaited via
/// GetAwaiter().GetResult(); ASP.NET Core has no synchronization context, so this cannot deadlock.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentPendingOrderReviewDraftStore : IPendingOrderReviewDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentPendingOrderReviewDraftStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Set(Guid userId, string conversationId, OrderReviewDraft draft)
    {
        var now = DateTime.UtcNow;
        draft.ExpiresAtUtc = now.AddMinutes(OrderReviewDefaults.PendingDraftTtlMinutes);

        var row = new PendingOrderReviewDraftRow
        {
            UserId = userId,
            ConversationId = conversationId,
            DraftJson = JsonSerializer.Serialize(draft, JsonOptions),
            ExpiresAtUtc = draft.ExpiresAtUtc
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingOrderReviewDraftRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    public OrderReviewDraft? Get(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingOrderReviewDraftRepository>();

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

    public bool HasDraft(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingOrderReviewDraftRepository>();
        return repository.ExistsAsync(userId, conversationId, DateTime.UtcNow).GetAwaiter().GetResult();
    }

    public void Clear(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingOrderReviewDraftRepository>();
        repository.DeleteAsync(userId, conversationId).GetAwaiter().GetResult();
    }

    private static OrderReviewDraft? DeserializeDraft(string? draftJson)
    {
        if (string.IsNullOrWhiteSpace(draftJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OrderReviewDraft>(draftJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
