// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Playground;

/// <summary>
/// In-memory chat history of the public Playground. Every visitor is the shared demo admin, so the history is
/// kept per visitor (device, else IP bucket) and conversation in process memory only, with a sliding lifetime
/// and hard caps, and never reaches the database. IsActive is false on every other installation, where the
/// conversation manager keeps using the database exactly as before.
/// </summary>
public interface IPlaygroundConversationStore
{
    /// <summary>
    /// True when the instance is the Playground and chat history must stay out of the database.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Returns the stored conversation of the current visitor or starts an empty one; never persisted.
    /// </summary>
    /// <param name="conversationId">Client conversation id</param>
    /// <param name="userId">Owner of the conversation (the shared demo admin)</param>
    LLMConversation GetOrCreate(string conversationId, string userId);

    /// <summary>
    /// True when the conversation exists for the current visitor and already holds at least one message.
    /// </summary>
    /// <param name="conversationId">Client conversation id</param>
    /// <param name="userId">Owner of the conversation</param>
    bool IsOngoing(string conversationId, string userId);

    /// <summary>
    /// The newest stored messages of the conversation, oldest first; empty when it is unknown or expired.
    /// </summary>
    /// <param name="conversationId">Client conversation id</param>
    /// <param name="userId">Owner of the conversation</param>
    IReadOnlyList<LLMMessage> GetMessages(string conversationId, string userId);

    /// <summary>
    /// Appends one user and assistant exchange and refreshes the counters and the lifetime of the conversation.
    /// </summary>
    /// <param name="conversation">The conversation returned by GetOrCreate</param>
    /// <param name="userMessage">Text the visitor sent</param>
    /// <param name="assistantMessage">Answer that was given</param>
    /// <param name="modelId">Model that produced the answer</param>
    /// <param name="turnStartedUtc">Time stamp of the user message; the answer follows one microsecond later</param>
    /// <param name="title">Short title derived from the first words of the user message</param>
    void AppendTurn(
        LLMConversation conversation, string userMessage, string assistantMessage, string modelId, DateTime turnStartedUtc, string title);
}
