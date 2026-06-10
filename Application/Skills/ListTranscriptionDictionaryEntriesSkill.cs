// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the transcription dictionary entries used by the speech-to-text fuzzy phonetic
/// correction. Each entry carries the correct term, optional category, the known phonetic
/// variants, an optional description and an optional language code. Use this to find entry
/// IDs before update_transcription_dictionary_entry / delete_transcription_dictionary_entry.
/// </summary>
/// <param name="language">Optional. Filters entries by language code (e.g. "de").</param>
/// <param name="search">Optional. Case-insensitive substring filter on the correct term.</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_transcription_dictionary_entries")]
public class ListTranscriptionDictionaryEntriesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListTranscriptionDictionaryEntriesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entries = await _mediator.Send(new GetTranscriptionDictionaryEntriesQuery(), cancellationToken);

        var language = GetParameter<string>(parameters, "language");
        if (!string.IsNullOrWhiteSpace(language))
        {
            entries = entries
                .Where(e => string.Equals(e.Language, language.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var search = GetParameter<string>(parameters, "search");
        if (!string.IsNullOrWhiteSpace(search))
        {
            entries = entries
                .Where(e => e.CorrectTerm.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var projected = entries
            .Select(e => new { e.Id, e.CorrectTerm, e.Category, e.PhoneticVariants, e.Description, e.Language })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Entries = projected },
            $"Found {projected.Count} transcription dictionary entries.");
    }
}
