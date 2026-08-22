// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the skill-retrieval query from the current user message anchored with conversation history,
/// shared by the streaming orchestrator and the non-streaming command handler so both paths interpret
/// mid-workflow turns identically: without the history anchor, a bare confirmation like "yes, correct"
/// embeds against nothing meaningful and the task skill of the earlier turns is lost from the tool set.
/// </summary>
/// <param name="userMessage">The raw user message of the current turn</param>
/// <param name="conversationId">Conversation whose user turns anchor the query (null for a fresh chat)</param>
/// <param name="userId">Owner of the conversation; history from another owner never anchors the query</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class RetrievalQueryBuilder : IRetrievalQueryBuilder
{
    // Number of most recent user messages prepended to the skill-retrieval query so that
    // mid-workflow turns (e.g. a bare "yes, correct") still retrieve the task skill the
    // earlier turns were about (e.g. create_employee).
    private const int RecentMessagesForRetrieval = 4;

    private const string UserRoleName = "user";

    private readonly LLMConversationManager _conversationManager;
    private readonly ILogger<RetrievalQueryBuilder> _logger;

    public RetrievalQueryBuilder(
        LLMConversationManager conversationManager,
        ILogger<RetrievalQueryBuilder> logger)
    {
        _conversationManager = conversationManager;
        _logger = logger;
    }

    public async Task<string> BuildAsync(string userMessage, string? conversationId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return userMessage;
        }

        try
        {
            var history = await _conversationManager.GetConversationHistoryAsync(conversationId, userId);
            if (history.Count == 0)
            {
                return userMessage;
            }

            // Only user messages enter the retrieval query: long assistant answers dilute the
            // embedding so badly that follow-up questions stop retrieving the relevant skill.
            var userMessages = history
                .Where(m => string.Equals(m.Role, UserRoleName, StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(m.Content))
                .ToList();

            var parts = new List<string>();

            // Anchor with the first user message: it carries the workflow intent
            // (e.g. "create a new employee") which must keep the task skill retrievable
            // through long multi-turn flows, even when recent turns only discuss sub-details.
            var firstUserMessage = userMessages.FirstOrDefault();
            if (firstUserMessage != null)
            {
                parts.Add(firstUserMessage.Content);
            }

            parts.AddRange(userMessages
                .Skip(1)
                .TakeLast(RecentMessagesForRetrieval)
                .Select(m => m.Content));

            parts.Add(userMessage);

            return string.Join("\n", parts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich retrieval query with conversation history; using current message only");
            return userMessage;
        }
    }
}
