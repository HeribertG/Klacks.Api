// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Teaches Klacksy new navigation phrasings: ADDS synonyms to one navigation target and
/// locale (existing synonyms are kept, duplicates ignored). AI-taught synonyms always set
/// the review status to "needs-review" so a human confirms them on the training page —
/// the skill never marks targets as "reviewed" itself (self-training guard). The write is
/// verified by re-reading the target.
/// </summary>
/// <param name="targetId">Required. ID of the navigation target (from list_navigation_targets).</param>
/// <param name="locale">Required. Locale the synonyms belong to (e.g. de, en, fr, it).</param>
/// <param name="synonyms">Required. Comma-separated phrases to add (lowercased and trimmed).</param>

using Klacks.Api.Application.Handlers.Klacksy;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_navigation_synonyms")]
public class UpdateNavigationSynonymsSkill : BaseSkillImplementation
{
    private const string NeedsReviewStatus = "needs-review";

    private readonly IMediator _mediator;

    public UpdateNavigationSynonymsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var targetId = GetRequiredString(parameters, "targetId");
        var locale = GetRequiredString(parameters, "locale").Trim().ToLowerInvariant();
        var synonymsRaw = GetRequiredString(parameters, "synonyms");

        var newSynonyms = synonymsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .Where(s => s.Length > 0)
            .Distinct()
            .ToList();
        if (newSynonyms.Count == 0)
        {
            return SkillResult.Error("synonyms must contain at least one non-empty phrase.");
        }

        var targets = await _mediator.Send(new GetNavigationTargetsQuery(null, locale), cancellationToken);
        var target = targets.FirstOrDefault(t => string.Equals(t.TargetId, targetId, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            var known = string.Join(", ", targets.Where(t => !t.Obsolete).Select(t => t.TargetId).Take(30));
            return SkillResult.Error(
                $"Navigation target '{targetId}' not found. Known targets: {known}. " +
                "Use list_navigation_targets and pick a real target id — do not invent one.");
        }

        var existing = target.Synonyms.TryGetValue(locale, out var current)
            ? current.ToList()
            : new List<string>();
        var merged = existing
            .Concat(newSynonyms)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actuallyAdded = merged.Length - existing.Count;

        if (actuallyAdded == 0)
        {
            return SkillResult.SuccessResult(
                new { TargetId = target.TargetId, Locale = locale, Synonyms = merged },
                $"All supplied synonyms already exist on '{target.TargetId}' ({locale}) — nothing to add.");
        }

        var success = await _mediator.Send(
            new UpdateNavigationTargetSynonymsCommand(target.TargetId, locale, merged, NeedsReviewStatus),
            cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Updating synonyms of '{target.TargetId}' failed.");
        }

        var persistedTargets = await _mediator.Send(new GetNavigationTargetsQuery(null, locale), cancellationToken);
        var persisted = persistedTargets.FirstOrDefault(t => t.TargetId == target.TargetId);
        var persistedSynonyms = persisted != null && persisted.Synonyms.TryGetValue(locale, out var check)
            ? check
            : Array.Empty<string>();
        if (persisted == null || newSynonyms.Any(s => !persistedSynonyms.Contains(s, StringComparer.OrdinalIgnoreCase)))
        {
            return SkillResult.Error(
                $"Database verification failed: the new synonyms are not on '{target.TargetId}' ({locale}) after " +
                "the write — treat the change as not persisted.");
        }

        return SkillResult.SuccessResult(
            new
            {
                TargetId = target.TargetId,
                Locale = locale,
                Added = actuallyAdded,
                Synonyms = persistedSynonyms,
                Status = NeedsReviewStatus
            },
            $"Added {actuallyAdded} synonym(s) to navigation target '{target.TargetId}' ({locale}) and confirmed " +
            $"in the database (verified). The target is now marked '{NeedsReviewStatus}' — an administrator " +
            "confirms it on the Klacksy training page before it counts as reviewed.");
    }
}
