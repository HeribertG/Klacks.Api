// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the unresolved navigation feedback of the Klacksy training page: utterances the
/// navigation matcher could not (confidently) map to a target, with timestamp, locale and
/// what — if anything — was matched. The raw material for teaching new synonyms via
/// update_navigation_synonyms.
/// </summary>
/// <param name="locale">Required. Locale to inspect (e.g. de, en, fr, it).</param>
/// <param name="limit">Optional. Maximum number of entries (default 25, newest first).</param>

using Klacks.Api.Application.Handlers.Klacksy;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_navigation_feedback")]
public class ListNavigationFeedbackSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 25;

    private readonly IMediator _mediator;

    public ListNavigationFeedbackSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var locale = GetRequiredString(parameters, "locale").Trim().ToLowerInvariant();
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var entries = await _mediator.Send(
            new GetNavigationFeedbackQuery(locale, limit), cancellationToken);

        var listed = entries
            .OrderByDescending(e => e.Timestamp)
            .Select(e => new
            {
                e.Utterance,
                e.Locale,
                e.MatchedTargetId,
                e.MatchedScore,
                e.UserAction,
                e.ActualRoute,
                Timestamp = e.Timestamp.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                Locale = locale,
                Count = listed.Count,
                Entries = listed
            },
            listed.Count == 0
                ? $"No unresolved navigation feedback for locale '{locale}' — the matcher understood everything."
                : $"{listed.Count} unresolved navigation utterance(s) for locale '{locale}'. " +
                  "Recurring phrasings are candidates for update_navigation_synonyms on the matching target.");
    }
}
