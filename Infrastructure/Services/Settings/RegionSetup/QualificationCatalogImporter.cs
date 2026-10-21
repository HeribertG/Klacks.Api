// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of Qualification catalog rows from industryProfiles.*.qualificationCatalog:
/// validates the entries into desired rows (multilingual name, category derived from the industry),
/// plans insert/update/skip against the stored rows and applies the decisions inside the import
/// transaction. Industry is classification, re-applied but not hashed. Keep the hashed fields
/// (ComputeContentHash), the applied fields (Apply) and QualificationCatalogImportValues in sync.
/// </summary>
/// <param name="qualificationImportRepository">Reads and stages imported Qualification rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class QualificationCatalogImporter
{
    private const string LanguageDe = "de";
    private const string LanguageEn = "en";
    private const string LanguageFr = "fr";
    private const string LanguageIt = "it";
    private const string CoreLanguageListSeparator = "/";

    private readonly IQualificationImportRepository _qualificationImportRepository;
    private readonly ILogger<QualificationCatalogImporter> _logger;

    public QualificationCatalogImporter(IQualificationImportRepository qualificationImportRepository, ILogger<QualificationCatalogImporter> logger)
    {
        _qualificationImportRepository = qualificationImportRepository;
        _logger = logger;
    }

    internal static EntityImportDesired<QualificationCatalogImportValues> BuildDesired(
        string industry, RegionSetupQualificationCatalogEntry entry, HashSet<string> seenKeys)
    {
        var fieldPrefix = $"industryProfiles.{industry}.qualificationCatalog";
        var names = new Dictionary<string, string>();
        foreach (var (rawLanguage, text) in entry.Name ?? [])
        {
            var language = rawLanguage.Trim().ToLowerInvariant();
            if (!MultiLanguage.CoreLanguages.Contains(language))
            {
                throw new InvalidRequestException(
                    $"Region setup: {fieldPrefix}.name language '{rawLanguage}' is not a core language ({string.Join(CoreLanguageListSeparator, MultiLanguage.CoreLanguages)}).");
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                names[language] = text.Trim();
            }
        }

        if (names.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix} entry requires a 'name' with at least one non-empty core-language value.");
        }

        var keyName = names.GetValueOrDefault(LanguageDe)
            ?? names.GetValueOrDefault(LanguageEn)
            ?? names.GetValueOrDefault(LanguageFr)
            ?? names[LanguageIt];
        var sourceKey = $"region-setup:industryProfiles:{industry}:qualification:{RegionSetupFieldParser.BuildImportSlug(keyName)}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles contains more than one qualification catalog entry resolving to key '{sourceKey}'.");
        }

        var values = new QualificationCatalogImportValues(
            names.GetValueOrDefault(LanguageDe),
            names.GetValueOrDefault(LanguageEn),
            names.GetValueOrDefault(LanguageFr),
            names.GetValueOrDefault(LanguageIt),
            entry.IsTimeLimited ?? false,
            IndustryQualificationCategoryMap.Resolve(industry),
            industry);

        return new EntityImportDesired<QualificationCatalogImportValues>(
            sourceKey,
            ComputeContentHash(values.NameDe, values.NameEn, values.NameFr, values.NameIt, values.IsTimeLimited, values.Category),
            values);
    }

    public async Task<EntityImportFamilyPlan<QualificationCatalogImportValues, Qualification>> PlanAsync(
        IReadOnlyList<EntityImportDesired<QualificationCatalogImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<QualificationCatalogImportValues, Qualification>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _qualificationImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(q => q.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            q => q.ImportSourceKey,
            q => ComputeContentHash(q.Name.De, q.Name.En, q.Name.Fr, q.Name.It, q.IsTimeLimited, q.Category) == q.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<QualificationCatalogImportValues, Qualification>(decisions, existingBySourceKey);
    }

    public void Apply(EntityImportFamilyPlan<QualificationCatalogImportValues, Qualification> plan)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _qualificationImportRepository.Add(new Qualification
                    {
                        Id = Guid.NewGuid(),
                        Name = new MultiLanguage
                        {
                            De = decision.Values.NameDe,
                            En = decision.Values.NameEn,
                            Fr = decision.Values.NameFr,
                            It = decision.Values.NameIt,
                        },
                        IsTimeLimited = decision.Values.IsTimeLimited,
                        Category = decision.Values.Category,
                        Industry = decision.Values.Industry,
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.Name.De = decision.Values.NameDe;
                    existing.Name.En = decision.Values.NameEn;
                    existing.Name.Fr = decision.Values.NameFr;
                    existing.Name.It = decision.Values.NameIt;
                    existing.IsTimeLimited = decision.Values.IsTimeLimited;
                    existing.Category = decision.Values.Category;
                    existing.Industry = decision.Values.Industry;
                    existing.ImportContentHash = decision.ContentHash;
                    _qualificationImportRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: qualification catalog entry '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static string ComputeContentHash(
        string? nameDe, string? nameEn, string? nameFr, string? nameIt, bool isTimeLimited, QualificationCategory category)
    {
        return ImportContentHasher.ComputeHash(
            nameDe ?? string.Empty,
            nameEn ?? string.Empty,
            nameFr ?? string.Empty,
            nameIt ?? string.Empty,
            ImportFieldFormatter.Bool(isTimeLimited),
            category.ToString());
    }
}
