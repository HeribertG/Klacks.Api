// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing qualification master entry. Resolves the qualification by id or
/// unambiguous name, merges only the provided fields onto the current values and dispatches
/// an <see cref="Klacks.Api.Application.Commands.Qualifications.UpdateQualificationCommand"/>.
/// </summary>
/// <param name="qualificationId">Id of the qualification to update. Preferred over qualificationName.</param>
/// <param name="qualificationName">Exact qualification name used when qualificationId is omitted.</param>
/// <param name="name_de">New German qualification name</param>
/// <param name="name_en">New English qualification name</param>
/// <param name="name_fr">New French qualification name</param>
/// <param name="name_it">New Italian qualification name</param>
/// <param name="description_de">New German description</param>
/// <param name="description_en">New English description</param>
/// <param name="description_fr">New French description</param>
/// <param name="description_it">New Italian description</param>
/// <param name="emoji">New emoji character(s) representing this qualification</param>
/// <param name="isTimeLimited">Whether client assignments of this qualification carry an expiry date</param>
/// <param name="type">Qualification type: Language or Work</param>
/// <param name="category">Industry category (e.g. None, Spitex, Security, Logistics)</param>
/// <param name="countries">Comma-separated ISO country codes (e.g. "CH,DE")</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_qualification")]
public class UpdateQualificationSkill : BaseSkillImplementation
{
    private const char CountrySeparator = ',';

    private readonly IMediator _mediator;

    public UpdateQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var nameDe = GetParameter<string>(parameters, "name_de");
        var nameEn = GetParameter<string>(parameters, "name_en");
        var nameFr = GetParameter<string>(parameters, "name_fr");
        var nameIt = GetParameter<string>(parameters, "name_it");

        var descDe = GetParameter<string>(parameters, "description_de")
                     ?? GetParameter<string>(parameters, "description");
        var descEn = GetParameter<string>(parameters, "description_en");
        var descFr = GetParameter<string>(parameters, "description_fr");
        var descIt = GetParameter<string>(parameters, "description_it");

        var emoji = GetParameter<string>(parameters, "emoji");
        var isTimeLimited = GetParameter<bool?>(parameters, "isTimeLimited");
        var typeRaw = GetParameter<string>(parameters, "type");
        var categoryRaw = GetParameter<string>(parameters, "category");
        var countriesRaw = GetParameter<string>(parameters, "countries");

        var hasUpdate = isTimeLimited.HasValue
                        || new[] { nameDe, nameEn, nameFr, nameIt, descDe, descEn, descFr, descIt, emoji, typeRaw, categoryRaw, countriesRaw }
                            .Any(value => !string.IsNullOrWhiteSpace(value));
        if (!hasUpdate)
        {
            return SkillResult.Error("Nothing to update. Provide at least one field to change.");
        }

        QualificationType? type = null;
        if (!string.IsNullOrWhiteSpace(typeRaw))
        {
            if (!Enum.TryParse(typeRaw.Trim(), true, out QualificationType parsedType))
            {
                return SkillResult.Error($"'{typeRaw}' is not a valid qualification type. Valid values: {string.Join(", ", Enum.GetNames<QualificationType>())}.");
            }

            type = parsedType;
        }

        QualificationCategory? category = null;
        if (!string.IsNullOrWhiteSpace(categoryRaw))
        {
            if (!Enum.TryParse(categoryRaw.Trim(), true, out QualificationCategory parsedCategory))
            {
                return SkillResult.Error($"'{categoryRaw}' is not a valid qualification category. Valid values: {string.Join(", ", Enum.GetNames<QualificationCategory>())}.");
            }

            category = parsedCategory;
        }

        var (existing, resolveError) = await QualificationResolver.ResolveAsync(
            _mediator,
            GetParameter<string>(parameters, "qualificationId"),
            GetParameter<string>(parameters, "qualificationName"),
            cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var mlName = existing.Name;
        ApplyLanguageOverrides(mlName, nameDe, nameEn, nameFr, nameIt);

        var mlDescription = existing.Description;
        if (!string.IsNullOrWhiteSpace(descDe) || !string.IsNullOrWhiteSpace(descEn)
            || !string.IsNullOrWhiteSpace(descFr) || !string.IsNullOrWhiteSpace(descIt))
        {
            mlDescription ??= new MultiLanguage();
            ApplyLanguageOverrides(mlDescription, descDe, descEn, descFr, descIt);
        }

        IList<string> countries = existing.Countries;
        if (!string.IsNullOrWhiteSpace(countriesRaw))
        {
            countries = countriesRaw
                .Split(CountrySeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        var updated = await _mediator.Send(
            new UpdateQualificationCommand(
                existing.Id,
                mlName,
                mlDescription,
                string.IsNullOrWhiteSpace(emoji) ? existing.Emoji : emoji.Trim(),
                isTimeLimited ?? existing.IsTimeLimited,
                type ?? existing.Type,
                category ?? existing.Category,
                countries),
            cancellationToken);

        return SkillResult.SuccessResult(
            new { updated.Id, updated.Name },
            $"Qualification '{QualificationResolver.DisplayName(updated)}' updated (id {updated.Id}).");
    }

    private static void ApplyLanguageOverrides(MultiLanguage target, string? de, string? en, string? fr, string? it)
    {
        if (!string.IsNullOrWhiteSpace(de))
        {
            target.De = de.Trim();
        }

        if (!string.IsNullOrWhiteSpace(en))
        {
            target.En = en.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fr))
        {
            target.Fr = fr.Trim();
        }

        if (!string.IsNullOrWhiteSpace(it))
        {
            target.It = it.Trim();
        }
    }
}
