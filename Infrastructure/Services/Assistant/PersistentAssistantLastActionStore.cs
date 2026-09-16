// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF-backed store for the previous-action record, so the anchor of a correction survives a backend
/// restart. Each operation runs in its OWN service scope (via IServiceScopeFactory) rather than on the
/// request-scoped DbContext, mirroring PersistentPendingRecipeStore: the chat pipeline launches
/// fire-and-forget tasks that touch the request context concurrently, and these calls run synchronously
/// on the same request thread, so a shared context would race. The interface is synchronous because the
/// caller (LLMService, both chat entry points) invokes it synchronously; the short single-row repository
/// calls are awaited via GetAwaiter().GetResult(), which cannot deadlock without a synchronization
/// context. Every stored text is capped here, not only in the column configuration.
/// </summary>
/// <param name="scopeFactory">Creates an isolated service scope (and DbContext) per store operation.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PersistentAssistantLastActionStore : IAssistantLastActionStore
{
    private const string EmptyJsonArray = GracefulCorrectionDefaults.EmptyJsonArray;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public PersistentAssistantLastActionStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Save(AssistantLastAction action)
    {
        var now = DateTime.UtcNow;
        var row = new AssistantLastActionRow
        {
            UserId = action.UserId,
            ConversationId = action.ConversationId,
            UserMessage = Cap(action.UserMessage, GracefulCorrectionDefaults.UserMessageMaxLength),
            CallsJson = JsonSerializer.Serialize(CapCalls(action.Calls), JsonOptions),
            AssistantAnswerExcerpt = Cap(action.AssistantAnswerExcerpt, GracefulCorrectionDefaults.AnswerExcerptMaxLength),
            ClarificationSkillsJson = JsonSerializer.Serialize(action.ClarificationSkillNames, JsonOptions),
            CreateTimeUtc = action.CreateTimeUtc == default ? now : action.CreateTimeUtc,
            SupersededAtUtc = action.SupersededAtUtc,
            ExpiresAtUtc = now.AddMinutes(GracefulCorrectionDefaults.LastActionTtlMinutes)
        };

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAssistantLastActionRepository>();
        repository.PruneExpiredAsync(now).GetAwaiter().GetResult();
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    public AssistantLastAction? Peek(Guid userId, string conversationId)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAssistantLastActionRepository>();

        var row = repository.GetAsync(userId, conversationId).GetAwaiter().GetResult();
        if (row == null)
        {
            return null;
        }

        if (row.ExpiresAtUtc < DateTime.UtcNow)
        {
            repository.PruneExpiredAsync(DateTime.UtcNow).GetAwaiter().GetResult();
            return null;
        }

        return new AssistantLastAction
        {
            UserId = row.UserId,
            ConversationId = row.ConversationId,
            UserMessage = row.UserMessage,
            Calls = Deserialize<List<AssistantLastActionCall>>(row.CallsJson) ?? [],
            AssistantAnswerExcerpt = row.AssistantAnswerExcerpt,
            CreateTimeUtc = row.CreateTimeUtc,
            SupersededAtUtc = row.SupersededAtUtc,
            ClarificationSkillNames = Deserialize<List<string>>(row.ClarificationSkillsJson) ?? []
        };
    }

    public void MarkSuperseded(Guid userId, string conversationId)
    {
        Mutate(userId, conversationId, createWhenMissing: false, row => row.SupersededAtUtc = DateTime.UtcNow);
    }

    /// <summary>
    /// Records the two options a clarification offered and stops the record from anchoring the NEXT
    /// correction - its pins are still read, which is exactly what CanAnchorCorrection separates. Never
    /// creates a row: a clarification always follows a correction, and a correction always had an anchor
    /// (gate G0), so "no row" means the caller is wrong rather than that a row is missing.
    /// </summary>
    public void SaveClarificationCandidates(Guid userId, string conversationId, IReadOnlyList<string> skillNames)
    {
        var json = JsonSerializer.Serialize(skillNames, JsonOptions);
        Mutate(userId, conversationId, createWhenMissing: false, row =>
        {
            row.ClarificationSkillsJson = json;
            row.SupersededAtUtc = DateTime.UtcNow;
        });
    }

    /// <summary>
    /// Read-modify-write inside ONE scope, so the row EF hands back is the very instance that is written
    /// again - a second scope would produce a detached copy and an identity conflict on save.
    /// </summary>
    private void Mutate(Guid userId, string conversationId, bool createWhenMissing, Action<AssistantLastActionRow> mutate)
    {
        var now = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAssistantLastActionRepository>();

        var row = repository.GetAsync(userId, conversationId).GetAwaiter().GetResult();
        if (row == null)
        {
            if (!createWhenMissing)
            {
                return;
            }

            row = new AssistantLastActionRow
            {
                UserId = userId,
                ConversationId = conversationId,
                CallsJson = EmptyJsonArray,
                ClarificationSkillsJson = EmptyJsonArray,
                CreateTimeUtc = now,
                ExpiresAtUtc = now.AddMinutes(GracefulCorrectionDefaults.LastActionTtlMinutes)
            };
        }

        mutate(row);
        repository.UpsertAsync(row).GetAwaiter().GetResult();
    }

    private static List<AssistantLastActionCall> CapCalls(IReadOnlyList<AssistantLastActionCall> calls)
    {
        var capped = new List<AssistantLastActionCall>(calls.Count);
        foreach (var call in calls)
        {
            capped.Add(new AssistantLastActionCall
            {
                SkillName = call.SkillName,
                ArgumentsJson = Cap(call.ArgumentsJson, GracefulCorrectionDefaults.CallJsonMaxLength),
                ResultDataJson = Cap(call.ResultDataJson, GracefulCorrectionDefaults.CallJsonMaxLength),
                IsReadOnly = call.IsReadOnly,
                Success = call.Success
            });
        }

        return capped;
    }

    private static string Cap(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
