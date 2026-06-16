// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates the note text of an existing client annotation. The annotation is loaded via
/// GetQuery&lt;AnnotationResource&gt; and saved via PutCommand&lt;AnnotationResource&gt;.
/// </summary>
/// <param name="annotationId">Required. UUID of the annotation to update.</param>
/// <param name="note">Required. New note text.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_annotation")]
public class UpdateAnnotationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateAnnotationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var annotationId = GetRequiredGuid(parameters, "annotationId");
        var note = GetRequiredString(parameters, "note");

        AnnotationResource annotation;
        try
        {
            annotation = await _mediator.Send(new GetQuery<AnnotationResource>(annotationId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Annotation '{annotationId}' not found.");
        }

        if (note == annotation.Note)
        {
            return SkillResult.SuccessResult(
                new { AnnotationId = annotationId },
                "Note unchanged — supplied text equals the current note.");
        }

        annotation.Note = note;

        var updated = await _mediator.Send(new PutCommand<AnnotationResource>(annotation), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating annotation '{annotationId}' failed.");
        }

        return SkillResult.SuccessResult(
            new { AnnotationId = annotationId, updated.ClientId, updated.Note },
            "Annotation updated.");
    }
}
