// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that adds a WorkChange (correction / replacement / travel / briefing) to an existing Work entry.
/// Wraps PostCommand&lt;WorkChangeResource&gt; so SignalR notifications and period-hour recalculation fire.
/// </summary>
/// <param name="workId">UUID of the parent Work entry.</param>
/// <param name="type">WorkChangeType: CorrectionEnd / CorrectionStart / ReplacementStart / ReplacementEnd / ReplacementWithin / TravelStart / TravelEnd / TravelWithin / Briefing / Debriefing.</param>
/// <param name="startTime">HH:mm.</param>
/// <param name="endTime">HH:mm.</param>
/// <param name="changeTime">Hours delta (e.g. additional or saved hours).</param>
/// <param name="surcharges">Optional surcharge value.</param>
/// <param name="replaceClientId">Required only for Replacement* types — the client taking over.</param>
/// <param name="description">Free-text description.</param>
/// <param name="toInvoice">Whether this change is billable.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_workchange")]
public class AddWorkChangeSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IWorkRepository _workRepository;
    private readonly IClientRepository _clientRepository;

    public AddWorkChangeSkill(
        IMediator mediator,
        IWorkRepository workRepository,
        IClientRepository clientRepository)
    {
        _mediator = mediator;
        _workRepository = workRepository;
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");
        var typeStr = GetRequiredString(parameters, "type");
        var startTimeStr = GetRequiredString(parameters, "startTime");
        var endTimeStr = GetRequiredString(parameters, "endTime");
        var changeTime = GetParameter<decimal?>(parameters, "changeTime") ?? 0m;
        var surcharges = GetParameter<decimal?>(parameters, "surcharges") ?? 0m;
        var replaceClientIdRaw = GetParameter<string>(parameters, "replaceClientId");
        var description = GetParameter<string>(parameters, "description") ?? string.Empty;
        var toInvoice = GetParameter<bool?>(parameters, "toInvoice") ?? false;

        if (!Enum.TryParse<WorkChangeType>(typeStr, ignoreCase: true, out var type))
        {
            return SkillResult.Error(
                $"Invalid WorkChange type '{typeStr}'. Expected one of: " +
                string.Join(", ", Enum.GetNames<WorkChangeType>()));
        }

        if (!TimeOnly.TryParse(startTimeStr, out var startTime))
        {
            return SkillResult.Error($"Invalid startTime '{startTimeStr}'. Expected HH:mm.");
        }
        if (!TimeOnly.TryParse(endTimeStr, out var endTime))
        {
            return SkillResult.Error($"Invalid endTime '{endTimeStr}'. Expected HH:mm.");
        }

        var work = await _workRepository.Get(workId);
        if (work == null)
        {
            return SkillResult.Error($"Work {workId} not found.");
        }

        Guid? replaceClientId = null;
        if (!string.IsNullOrWhiteSpace(replaceClientIdRaw))
        {
            if (!Guid.TryParse(replaceClientIdRaw, out var parsed))
            {
                return SkillResult.Error($"Invalid replaceClientId UUID '{replaceClientIdRaw}'.");
            }
            if (!await _clientRepository.Exists(parsed))
            {
                return SkillResult.Error($"Replacement client {parsed} not found.");
            }
            replaceClientId = parsed;
        }

        var isReplacement = type is WorkChangeType.ReplacementStart
            or WorkChangeType.ReplacementEnd
            or WorkChangeType.ReplacementWithin;
        if (isReplacement && replaceClientId == null)
        {
            return SkillResult.Error($"WorkChange type '{type}' requires replaceClientId.");
        }

        var resource = new WorkChangeResource
        {
            WorkId = workId,
            Type = type,
            StartTime = startTime,
            EndTime = endTime,
            ChangeTime = changeTime,
            Surcharges = surcharges,
            ReplaceClientId = replaceClientId,
            Description = description,
            ToInvoice = toInvoice
        };

        var result = await _mediator.Send(new PostCommand<WorkChangeResource>(resource), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error("WorkChange creation returned null — verify the Work exists and the period is editable.");
        }

        return SkillResult.SuccessResult(
            new
            {
                Id = result.Id,
                WorkId = workId,
                Type = type.ToString(),
                StartTime = startTime,
                EndTime = endTime,
                ChangeTime = changeTime,
                Surcharges = surcharges,
                ReplaceClientId = replaceClientId,
                ToInvoice = toInvoice,
                PeriodStart = result.PeriodStart,
                PeriodEnd = result.PeriodEnd
            },
            $"WorkChange of type {type} added to work {workId}.");
    }
}
