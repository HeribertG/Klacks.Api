// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the navigation targets of the Klacksy training page: route, category, synonym
/// review status (pending / generated / reviewed / needs-review) and the synonyms of a
/// requested locale — the basis for teaching Klacksy new navigation phrasings.
/// </summary>
/// <param name="status">Optional. Filter by synonym status: pending, generated, reviewed or needs-review.</param>
/// <param name="locale">Optional. Locale whose synonyms are shown (e.g. de, en, fr, it); default de.</param>
/// <param name="limit">Optional. Maximum number of targets to return (default 30).</param>

using Klacks.Api.Application.Handlers.Klacksy;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_navigation_targets")]
public class ListNavigationTargetsSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 30;
    private const string DefaultLocale = "de";

    private readonly IMediator _mediator;

    public ListNavigationTargetsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var status = GetParameter<string>(parameters, "status");
        var locale = (GetParameter<string>(parameters, "locale") ?? DefaultLocale).Trim().ToLowerInvariant();
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var targets = await _mediator.Send(
            new GetNavigationTargetsQuery(status, locale), cancellationToken);

        var active = targets.Where(t => !t.Obsolete).ToList();
        var byStatus = active
            .GroupBy(t => t.SynonymStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        var listed = active
            .OrderBy(t => t.TargetId, StringComparer.Ordinal)
            .Take(limit)
            .Select(t => new
            {
                t.TargetId,
                t.Route,
                t.Category,
                t.SynonymStatus,
                Synonyms = t.Synonyms.TryGetValue(locale, out var syns) ? syns : Array.Empty<string>()
            })
            .ToList();

        var truncatedNote = active.Count > limit
            ? $" Showing the first {limit} of {active.Count} targets."
            : string.Empty;
        var statusSummary = byStatus.Count > 0
            ? string.Join(", ", byStatus.OrderBy(kv => kv.Key).Select(kv => $"{kv.Value} {kv.Key}"))
            : "none";

        return SkillResult.SuccessResult(
            new
            {
                Locale = locale,
                StatusFilter = status,
                TotalTargets = active.Count,
                ByStatus = byStatus,
                Targets = listed
            },
            $"{active.Count} navigation target(s){(status != null ? $" with status '{status}'" : string.Empty)} " +
            $"({statusSummary}); synonyms shown for locale '{locale}'.{truncatedNote} " +
            "Use update_navigation_synonyms to teach new phrasings.");
    }
}
