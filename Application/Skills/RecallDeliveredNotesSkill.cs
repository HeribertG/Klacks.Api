// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("recall_delivered_notes")]
public class RecallDeliveredNotesSkill : BaseSkillImplementation
{
    private readonly IPendingUserNoteRepository _noteRepository;
    private readonly IAgentRepository _agentRepository;

    public RecallDeliveredNotesSkill(
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
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.SuccessResult(new { Notes = Array.Empty<object>(), Count = 0 }, "No agent configured.");
        }

        var notes = await _noteRepository.GetDeliveredAsync(agent.Id, context.UserId, cancellationToken);
        var result = notes
            .Select(n => new { n.Id, n.Content, n.Topic, CreatedAt = n.CreateTime, DeliveredAt = n.DeletedTime })
            .ToList();

        return SkillResult.SuccessResult(
            new { Notes = result, Count = result.Count },
            $"Found {result.Count} already-delivered note(s) for this user.");
    }
}
