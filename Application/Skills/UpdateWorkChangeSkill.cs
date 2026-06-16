// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing WorkChange (correction / replacement / travel / briefing on a Work entry). Only
/// fields supplied as parameters are changed. Loads the entry via GetQuery&lt;WorkChangeResource&gt;,
/// mutates it and saves via PutCommand&lt;WorkChangeResource&gt; so notifications and period-hour
/// recalculation fire.
/// </summary>
/// <param name="workChangeId">Required. UUID of the WorkChange to update.</param>
/// <param name="type">Optional. New WorkChangeType (CorrectionEnd / ReplacementStart / TravelStart / Briefing etc.).</param>
/// <param name="startTime">Optional. New start time HH:mm.</param>
/// <param name="endTime">Optional. New end time HH:mm.</param>
/// <param name="changeTime">Optional. New hours delta.</param>
/// <param name="surcharges">Optional. New surcharge value.</param>
/// <param name="description">Optional. New free-text description.</param>
/// <param name="toInvoice">Optional. Whether this change is billable.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_workchange")]
public class UpdateWorkChangeSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateWorkChangeSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workChangeId = GetRequiredGuid(parameters, "workChangeId");

        WorkChangeResource workChange;
        try
        {
            workChange = await _mediator.Send(new GetQuery<WorkChangeResource>(workChangeId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"WorkChange '{workChangeId}' not found.");
        }

        var changed = new List<string>();

        var typeStr = GetParameter<string>(parameters, "type");
        if (!string.IsNullOrWhiteSpace(typeStr))
        {
            if (!Enum.TryParse<WorkChangeType>(typeStr, ignoreCase: true, out var type))
            {
                return SkillResult.Error(
                    $"Invalid WorkChange type '{typeStr}'. Expected one of: " +
                    string.Join(", ", Enum.GetNames<WorkChangeType>()));
            }
            if (type != workChange.Type)
            {
                workChange.Type = type;
                changed.Add("type");
            }
        }

        var startTimeRaw = GetParameter<string>(parameters, "startTime");
        if (!string.IsNullOrWhiteSpace(startTimeRaw))
        {
            if (!TimeOnly.TryParse(startTimeRaw, out var startTime))
            {
                return SkillResult.Error($"Invalid startTime '{startTimeRaw}'. Expected HH:mm.");
            }
            if (startTime != workChange.StartTime)
            {
                workChange.StartTime = startTime;
                changed.Add("startTime");
            }
        }

        var endTimeRaw = GetParameter<string>(parameters, "endTime");
        if (!string.IsNullOrWhiteSpace(endTimeRaw))
        {
            if (!TimeOnly.TryParse(endTimeRaw, out var endTime))
            {
                return SkillResult.Error($"Invalid endTime '{endTimeRaw}'. Expected HH:mm.");
            }
            if (endTime != workChange.EndTime)
            {
                workChange.EndTime = endTime;
                changed.Add("endTime");
            }
        }

        var changeTime = GetParameter<decimal?>(parameters, "changeTime");
        if (changeTime.HasValue && changeTime.Value != workChange.ChangeTime)
        {
            workChange.ChangeTime = changeTime.Value;
            changed.Add("changeTime");
        }

        var surcharges = GetParameter<decimal?>(parameters, "surcharges");
        if (surcharges.HasValue && surcharges.Value != workChange.Surcharges)
        {
            workChange.Surcharges = surcharges.Value;
            changed.Add("surcharges");
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description != workChange.Description)
        {
            workChange.Description = description;
            changed.Add("description");
        }

        var toInvoice = GetParameter<bool?>(parameters, "toInvoice");
        if (toInvoice.HasValue && toInvoice.Value != workChange.ToInvoice)
        {
            workChange.ToInvoice = toInvoice.Value;
            changed.Add("toInvoice");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { WorkChangeId = workChangeId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — WorkChange left unchanged.");
        }

        var isReplacement = workChange.Type is WorkChangeType.ReplacementStart
            or WorkChangeType.ReplacementEnd
            or WorkChangeType.ReplacementWithin;
        if (isReplacement && workChange.ReplaceClientId == null)
        {
            return SkillResult.Error($"WorkChange type '{workChange.Type}' requires a replacement client.");
        }

        var updated = await _mediator.Send(new PutCommand<WorkChangeResource>(workChange), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating WorkChange '{workChangeId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                WorkChangeId = workChangeId,
                ChangedFields = changed,
                updated.WorkId,
                Type = updated.Type.ToString(),
                updated.StartTime,
                updated.EndTime,
                updated.ChangeTime,
                updated.ToInvoice
            },
            $"WorkChange updated ({string.Join(", ", changed)}).");
    }
}
