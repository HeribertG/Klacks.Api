// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a transcription dictionary entry so its term is no longer corrected during
/// speech-to-text transcription. Use list_transcription_dictionary_entries first to find
/// the entryId.
/// </summary>
/// <param name="entryId">Required. UUID of the dictionary entry to delete.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_transcription_dictionary_entry")]
public class DeleteTranscriptionDictionaryEntrySkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteTranscriptionDictionaryEntrySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entryIdRaw = GetParameter<string>(parameters, "entryId");
        if (string.IsNullOrWhiteSpace(entryIdRaw) || !Guid.TryParse(entryIdRaw, out var entryId))
        {
            return SkillResult.Error("Missing or invalid required parameter 'entryId' (UUID). Use list_transcription_dictionary_entries to find it.");
        }

        var deleted = await _mediator.Send(new DeleteTranscriptionDictionaryEntryCommand { Id = entryId }, cancellationToken);
        if (!deleted)
        {
            return SkillResult.Error($"Transcription dictionary entry {entryId} not found.");
        }

        return SkillResult.SuccessResult(
            new { Id = entryId },
            $"Transcription dictionary entry {entryId} deleted.");
    }
}
