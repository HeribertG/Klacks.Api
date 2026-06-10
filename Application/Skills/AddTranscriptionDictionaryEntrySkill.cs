// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds an entry to the transcription dictionary so the speech-to-text pipeline corrects
/// misheard words to the correct term. Phonetic variants are optional because the fuzzy
/// phonetic matcher also catches unlisted sound-alike words; listing known mishearings
/// makes the correction deterministic.
/// </summary>
/// <param name="correctTerm">Required. The correct spelling of the term.</param>
/// <param name="phoneticVariants">Optional. Known mishearings, comma/semicolon separated or as JSON array.</param>
/// <param name="category">Optional. Category grouping (e.g. product, person, place).</param>
/// <param name="description">Optional. Short description of the term.</param>
/// <param name="language">Optional. Language code (e.g. "de") selecting the phonetic encoder.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_transcription_dictionary_entry")]
public class AddTranscriptionDictionaryEntrySkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public AddTranscriptionDictionaryEntrySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var correctTerm = GetParameter<string>(parameters, "correctTerm");
        if (string.IsNullOrWhiteSpace(correctTerm))
        {
            return SkillResult.Error("Missing required parameter 'correctTerm'.");
        }

        var variants = SkillStringListParser.ParseStringList(parameters, "phoneticVariants") ?? [];
        var category = GetParameter<string>(parameters, "category");
        var description = GetParameter<string>(parameters, "description");
        var language = GetParameter<string>(parameters, "language");

        var command = new CreateTranscriptionDictionaryEntryCommand
        {
            CorrectTerm = correctTerm.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            PhoneticVariants = variants,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Language = string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToLowerInvariant()
        };

        var created = await _mediator.Send(command, cancellationToken);

        return SkillResult.SuccessResult(
            new { created.Id, created.CorrectTerm, created.Category, created.PhoneticVariants, created.Description, created.Language },
            $"Transcription dictionary entry '{created.CorrectTerm}' created (id {created.Id}) with {created.PhoneticVariants.Count} phonetic variant(s).");
    }
}
