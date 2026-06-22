// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("manage_pending_notes")]
public class ManagePendingNotesSkill : BaseSkillImplementation
{
    private const string ActionRead = "read";
    private const string ActionMarkDelivered = "mark_delivered";

    private readonly IPendingUserNoteRepository _noteRepository;
    private readonly IAgentRepository _agentRepository;

    public ManagePendingNotesSkill(
        IPendingUserNoteRepository noteRepository,
        IAgentRepository agentRepository)
    {
        _noteRepository = noteRepository;
        _agentRepository = agentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var action = (GetParameter<string>(parameters, "action") ?? ActionRead).Trim().ToLowerInvariant();

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.SuccessResult(new { Notes = Array.Empty<object>(), Count = 0 }, "No agent configured.");
        }

        return action switch
        {
            ActionMarkDelivered => await MarkDeliveredAsync(agent.Id, context.UserId, parameters, cancellationToken),
            _ => await ReadAsync(agent.Id, context.UserId, cancellationToken)
        };
    }

    private async Task<SkillResult> ReadAsync(Guid agentId, Guid userId, CancellationToken cancellationToken)
    {
        var notes = await _noteRepository.GetPendingAsync(agentId, userId, cancellationToken);
        var result = notes
            .Select(n => new { n.Id, n.Content, n.Topic, CreatedAt = n.CreateTime, Broadcast = n.UserId == null })
            .ToList();

        return SkillResult.SuccessResult(
            new { Notes = result, Count = result.Count },
            result.Count == 0
                ? "No pending notes for this user."
                : $"{result.Count} pending note(s) for this user. After relaying them to the user, call manage_pending_notes again with action 'mark_delivered' and their ids.");
    }

    private async Task<SkillResult> MarkDeliveredAsync(Guid agentId, Guid userId, Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var noteIdsRaw = GetParameter<string>(parameters, "noteIds");
        if (string.IsNullOrWhiteSpace(noteIdsRaw))
        {
            return SkillResult.Error("Parameter 'noteIds' is required when action is 'mark_delivered'.");
        }

        var ids = noteIdsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return SkillResult.Error("No valid note id provided in 'noteIds'.");
        }

        var marked = await _noteRepository.MarkDeliveredAsync(agentId, userId, ids, cancellationToken);

        return SkillResult.SuccessResult(
            new { MarkedDelivered = marked, RequestedIds = ids.Count },
            $"Marked {marked} note(s) as delivered. They are now archived and no longer pending.");
    }
}
