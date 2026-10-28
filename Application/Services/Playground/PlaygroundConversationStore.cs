// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Process-memory chat history for the Playground. Entries are keyed by visitor (device id, else IP bucket),
/// owner and conversation id, hold only the newest ConversationMessagesLimit messages, expire a fixed time after
/// their last use and are capped in number (least recently used goes first), so memory stays bounded. All access
/// runs under one lock; the store is small by construction. Nothing is written anywhere else.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>
/// <param name="timeProvider">Clock the lifetime is measured with, injected so a test can drive it</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Playground;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundConversationStore : IPlaygroundConversationStore
{
    private const int MessagesPerTurn = 2;

    private const int LLMMessageModelIdMaxLength = 50;

    private const long AssistantAfterUserTicks = TimeSpan.TicksPerMicrosecond;

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(PlaygroundHardeningConstants.ConversationTtlMinutes);

    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new();
    private readonly IPlaygroundMode _mode;
    private readonly TimeProvider _timeProvider;

    public PlaygroundConversationStore(IPlaygroundMode mode, TimeProvider timeProvider)
    {
        _mode = mode;
        _timeProvider = timeProvider;
    }

    public bool IsActive => _mode.IsActive;

    public LLMConversation GetOrCreate(string conversationId, string userId)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Prune(now);
            var key = BuildKey(conversationId, userId);
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry(NewConversation(conversationId, userId, now.UtcDateTime));
                _entries[key] = entry;
                EvictOverCap();
            }

            entry.LastUsed = now;
            return entry.Conversation;
        }
    }

    public bool IsOngoing(string conversationId, string userId)
    {
        lock (_gate)
        {
            Prune(_timeProvider.GetUtcNow());
            return _entries.TryGetValue(BuildKey(conversationId, userId), out var entry) && entry.Conversation.MessageCount > 0;
        }
    }

    public IReadOnlyList<LLMMessage> GetMessages(string conversationId, string userId)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Prune(now);
            if (!_entries.TryGetValue(BuildKey(conversationId, userId), out var entry))
            {
                return Array.Empty<LLMMessage>();
            }

            entry.LastUsed = now;
            return entry.Messages.ToList();
        }
    }

    public void AppendTurn(
        LLMConversation conversation, string userMessage, string assistantMessage, string modelId, DateTime turnStartedUtc, string title)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            Prune(now);
            var key = BuildKey(conversation.ConversationId, conversation.UserId);
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new Entry(conversation);
                _entries[key] = entry;
            }

            entry.Messages.Add(NewMessage(conversation, "user", userMessage, null, turnStartedUtc));
            entry.Messages.Add(NewMessage(conversation, "assistant", assistantMessage, modelId, turnStartedUtc.AddTicks(AssistantAfterUserTicks)));
            if (entry.Messages.Count > AssistantLimitDefaults.ConversationMessagesLimit)
            {
                entry.Messages.RemoveRange(0, entry.Messages.Count - AssistantLimitDefaults.ConversationMessagesLimit);
            }

            entry.Conversation.MessageCount += MessagesPerTurn;
            entry.Conversation.LastMessageAt = turnStartedUtc;
            entry.Conversation.LastModelId = modelId.Length > LLMMessageModelIdMaxLength ? modelId[..LLMMessageModelIdMaxLength] : modelId;
            entry.Conversation.Title ??= title;
            entry.LastUsed = now;
            EvictOverCap();
        }
    }

    private static string BuildKey(string conversationId, string userId)
    {
        var visitor = PlaygroundVisitorContext.Current;
        var visitorKey = visitor?.DeviceId ?? visitor?.IpBucketKey ?? PlaygroundHardeningConstants.AnonymousVisitorKey;
        var separator = PlaygroundHardeningConstants.ConversationKeySeparator;

        return $"{visitorKey}{separator}{userId}{separator}{conversationId}";
    }

    private static LLMConversation NewConversation(string conversationId, string userId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        UserId = userId,
        LastMessageAt = nowUtc,
        CreateTime = nowUtc,
    };

    private static LLMMessage NewMessage(LLMConversation conversation, string role, string content, string? modelId, DateTime createTime) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversation.Id,
        Role = role,
        Content = content,
        ModelId = modelId,
        CreateTime = createTime,
    };

    private void Prune(DateTimeOffset now)
    {
        var expired = _entries.Where(pair => now - pair.Value.LastUsed >= Lifetime).Select(pair => pair.Key).ToList();
        foreach (var key in expired)
        {
            _entries.Remove(key);
        }
    }

    private void EvictOverCap()
    {
        while (_entries.Count > PlaygroundHardeningConstants.MaxStoredConversations)
        {
            var oldest = _entries.MinBy(pair => pair.Value.LastUsed).Key;
            _entries.Remove(oldest);
        }
    }

    private sealed class Entry
    {
        public Entry(LLMConversation conversation)
        {
            Conversation = conversation;
        }

        public LLMConversation Conversation { get; }

        public List<LLMMessage> Messages { get; } = new();

        public DateTimeOffset LastUsed { get; set; }
    }
}
