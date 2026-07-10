// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates the note text of an existing client annotation. The annotation is loaded via
/// GetQuery&lt;AnnotationResource&gt; and saved via PutCommand&lt;AnnotationResource&gt;.
/// The write is self-verifying: the annotation is re-read after the update and must hold the
/// new note text before success is reported.
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

        AnnotationResource persisted;
        try
        {
            persisted = await _mediator.Send(new GetQuery<AnnotationResource>(annotationId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error(
                $"Database verification failed: annotation '{annotationId}' could not be re-read after the update.");
        }

        if (persisted.Note != note)
        {
            return SkillResult.Error(
                $"Database verification failed: annotation '{annotationId}' does not hold the new note text " +
                "after re-reading — the update did not persist as requested.");
        }

        return SkillResult.SuccessResult(
            new { AnnotationId = annotationId, persisted.ClientId, persisted.Note },
            "Annotation updated and confirmed in the database (verified).");
    }
}
