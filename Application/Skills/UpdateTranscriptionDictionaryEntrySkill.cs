// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing transcription dictionary entry. Only supplied fields change: the
/// correct term must stay non-empty, a non-empty variant list replaces the existing variants,
/// and an empty string clears category, description or language. Use
/// list_transcription_dictionary_entries first to find the entryId.
/// </summary>
/// <param name="entryId">Required. UUID of the dictionary entry to update.</param>
/// <param name="correctTerm">Optional. New correct spelling of the term.</param>
/// <param name="phoneticVariants">Optional. Replacement variant list, comma/semicolon separated or as JSON array.</param>
/// <param name="category">Optional. New category; empty string clears it.</param>
/// <param name="description">Optional. New description; empty string clears it.</param>
/// <param name="language">Optional. New language code; empty string clears it.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_transcription_dictionary_entry")]
public class UpdateTranscriptionDictionaryEntrySkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateTranscriptionDictionaryEntrySkill(IMediator mediator)
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

        var correctTerm = GetParameter<string>(parameters, "correctTerm");
        if (correctTerm != null && string.IsNullOrWhiteSpace(correctTerm))
        {
            return SkillResult.Error("Parameter 'correctTerm' must not be empty.");
        }

        var variants = SkillStringListParser.ParseStringList(parameters, "phoneticVariants");
        var category = GetParameter<string>(parameters, "category");
        var description = GetParameter<string>(parameters, "description");
        var language = GetParameter<string>(parameters, "language");

        if (correctTerm == null && variants == null && category == null && description == null && language == null)
        {
            return SkillResult.Error("Nothing to update. Supply at least one of: correctTerm, phoneticVariants, category, description, language.");
        }

        var command = new UpdateTranscriptionDictionaryEntryCommand
        {
            Id = entryId,
            CorrectTerm = correctTerm?.Trim(),
            Category = category?.Trim(),
            PhoneticVariants = variants,
            Description = description?.Trim(),
            Language = language?.Trim().ToLowerInvariant()
        };

        var updated = await _mediator.Send(command, cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Transcription dictionary entry {entryId} not found.");
        }

        return SkillResult.SuccessResult(
            new { updated.Id, updated.CorrectTerm, updated.Category, updated.PhoneticVariants, updated.Description, updated.Language },
            $"Transcription dictionary entry '{updated.CorrectTerm}' updated (id {updated.Id}).");
    }
}
