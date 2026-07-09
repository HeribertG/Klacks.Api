// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Correlates skill usage records with the raw user message of the same turn. There is no
/// foreign key between the two tables, so the join goes over the string conversation id
/// plus a time window, keeps only the first skill record per turn (later records are
/// follow-up iterations of the multi-turn loop) and drops turns whose trajectory was
/// marked as corrected — a corrected turn is not golden.
/// </summary>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class TurnGoldsetCandidateRepository : ITurnGoldsetCandidateRepository
{
    private const string UserRole = "user";
    private const int MessageWindowSeconds = 120;
    private const int CorrectionWindowSeconds = 180;
    private const int OversampleFactor = 3;

    private readonly DataBaseContext _context;

    public TurnGoldsetCandidateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TurnGoldsetCandidate>> GetCandidatesAsync(
        DateTime fromDate,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var records = await _context.SkillUsageRecords
            .Where(r => r.Success && r.SessionId != null && r.ParametersJson != null && r.Timestamp >= fromDate)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit * OversampleFactor)
            .Select(r => new { r.SessionId, r.SkillName, r.ParametersJson, r.Timestamp, r.UserId })
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return Array.Empty<TurnGoldsetCandidate>();
        }

        var sessionIds = records.Select(r => r.SessionId!).Distinct().ToList();

        var conversations = await _context.LLMConversations
            .Where(c => sessionIds.Contains(c.ConversationId))
            .Select(c => new { c.Id, c.ConversationId })
            .ToListAsync(cancellationToken);
        var conversationsByStringId = conversations.ToDictionary(c => c.ConversationId, c => c.Id);
        var conversationGuids = conversations.Select(c => c.Id).ToList();

        var messageFloor = fromDate.AddSeconds(-MessageWindowSeconds);
        var messages = await _context.LLMMessages
            .Where(m => conversationGuids.Contains(m.ConversationId) && m.Role == UserRole && m.CreateTime >= messageFloor)
            .Select(m => new { m.ConversationId, m.Content, CreateTime = m.CreateTime ?? DateTime.MinValue })
            .ToListAsync(cancellationToken);
        var messagesByConversation = messages
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreateTime).ToList());

        var correctedTurns = await _context.SkillSelectionTrajectories
            .Where(t => t.WasCorrected && t.CreateTime >= messageFloor)
            .Select(t => new { t.UserId, CreateTime = t.CreateTime ?? DateTime.MinValue })
            .ToListAsync(cancellationToken);

        var candidatesByTurn = new Dictionary<(Guid ConversationId, DateTime MessageTime), TurnGoldsetCandidate>();

        foreach (var record in records.OrderBy(r => r.Timestamp))
        {
            if (!conversationsByStringId.TryGetValue(record.SessionId!, out var conversationGuid)
                || !messagesByConversation.TryGetValue(conversationGuid, out var conversationMessages))
            {
                continue;
            }

            var message = conversationMessages.FirstOrDefault(m =>
                m.CreateTime <= record.Timestamp
                && m.CreateTime >= record.Timestamp.AddSeconds(-MessageWindowSeconds));
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            var recordUserId = record.UserId.ToString();
            var wasCorrected = correctedTurns.Any(t =>
                string.Equals(t.UserId, recordUserId, StringComparison.OrdinalIgnoreCase)
                && Math.Abs((t.CreateTime - message.CreateTime).TotalSeconds) <= CorrectionWindowSeconds);
            if (wasCorrected)
            {
                continue;
            }

            var turnKey = (conversationGuid, message.CreateTime);
            if (!candidatesByTurn.ContainsKey(turnKey))
            {
                candidatesByTurn[turnKey] = new TurnGoldsetCandidate(
                    message.Content, record.SkillName, record.ParametersJson, record.SessionId!, record.Timestamp);
            }
        }

        return candidatesByTurn.Values
            .OrderByDescending(c => c.Timestamp)
            .Take(limit)
            .ToList();
    }
}
