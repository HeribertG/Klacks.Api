// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fans the industryProfiles map of a region-setup profile out into desired entity rows: scheduling rule
/// presets with their rate revisions, qualification catalog rows and the industry-bound period caps,
/// rest-day rotations and counter rules, which need exactly one preset per industry as binding target.
/// </summary>
/// <param name="industryProfiles">industryProfiles map of the profile; null or empty yields no rows</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class IndustryProfileImportDesiredBuilder
{
    internal static IndustryProfileDesired Build(Dictionary<string, RegionSetupIndustryProfile>? industryProfiles)
    {
        var rulePresets = new List<EntityImportDesired<SchedulingRulePresetImportValues>>();
        var rateRevisions = new List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>>();
        var qualifications = new List<EntityImportDesired<QualificationCatalogImportValues>>();
        var boundPeriodCaps = new List<EntityImportDesired<PeriodCapRuleImportValues>>();
        var boundRotations = new List<EntityImportDesired<RestDayRotationImportValues>>();
        var boundCounterRules = new List<EntityImportDesired<CounterRuleImportValues>>();
        var ruleSourceKeyByBoundEntityKey = new Dictionary<string, string>();

        if (industryProfiles == null || industryProfiles.Count == 0)
        {
            return new IndustryProfileDesired(rulePresets, rateRevisions, qualifications, boundPeriodCaps, boundRotations, boundCounterRules, ruleSourceKeyByBoundEntityKey);
        }

        var seenRuleKeys = new HashSet<string>();
        var seenRevisionKeys = new HashSet<string>();
        var seenQualificationKeys = new HashSet<string>();
        var seenBoundEntityKeys = new HashSet<string>();

        foreach (var (rawIndustry, profile) in industryProfiles)
        {
            var industry = RegionSetupFieldParser.BuildImportSlug(rawIndustry);
            if (industry.Length == 0)
            {
                throw new InvalidRequestException("Region setup: industryProfiles contains an entry with an empty industry key.");
            }

            if (profile == null)
            {
                continue;
            }

            var blockRuleKeys = new List<string>();
            foreach (var preset in profile.SchedulingRulePresets ?? [])
            {
                var desired = SchedulingRulePresetImporter.BuildDesired(industry, preset, seenRuleKeys);
                blockRuleKeys.Add(desired.SourceKey);
                rulePresets.Add(desired);

                foreach (var revision in SchedulingRuleRateRevisionImporter.BuildDesired(industry, preset, desired.SourceKey, seenRevisionKeys))
                {
                    ruleSourceKeyByBoundEntityKey[revision.SourceKey] = desired.SourceKey;
                    rateRevisions.Add(revision);
                }
            }

            foreach (var entry in profile.QualificationCatalog ?? [])
            {
                qualifications.Add(QualificationCatalogImporter.BuildDesired(industry, entry, seenQualificationKeys));
            }

            var hasBoundEntities = (profile.PeriodCaps?.Count ?? 0) > 0 || (profile.RestDayRotations?.Count ?? 0) > 0
                || (profile.CounterRules?.Count ?? 0) > 0;
            if (!hasBoundEntities)
            {
                continue;
            }

            if (blockRuleKeys.Count != 1)
            {
                throw new InvalidRequestException(
                    $"Region setup: industryProfiles.{industry} carries periodCaps/restDayRotations but {blockRuleKeys.Count} scheduling rule presets - industry-scoped rules need exactly ONE preset as their binding target.");
            }

            var ruleSourceKey = blockRuleKeys[0];

            var caps = PeriodCapRuleImporter.BuildDesired(
                profile.PeriodCaps,
                $"industryProfiles.{industry}.periodCaps",
                $"region-setup:industryProfiles:{industry}:periodCap",
                seenBoundEntityKeys);
            foreach (var cap in caps)
            {
                ruleSourceKeyByBoundEntityKey[cap.SourceKey] = ruleSourceKey;
                boundPeriodCaps.Add(cap);
            }

            var rotations = RestDayRotationRuleImporter.BuildDesired(
                profile.RestDayRotations,
                $"industryProfiles.{industry}.restDayRotations",
                $"region-setup:industryProfiles:{industry}:restDayRotation",
                seenBoundEntityKeys);
            foreach (var rotation in rotations)
            {
                ruleSourceKeyByBoundEntityKey[rotation.SourceKey] = ruleSourceKey;
                boundRotations.Add(rotation);
            }

            var counters = CounterRuleImporter.BuildDesired(
                profile.CounterRules,
                $"industryProfiles.{industry}.counterRules",
                $"region-setup:industryProfiles:{industry}:counterRule",
                seenBoundEntityKeys);
            foreach (var counter in counters)
            {
                ruleSourceKeyByBoundEntityKey[counter.SourceKey] = ruleSourceKey;
                boundCounterRules.Add(counter);
            }
        }

        return new IndustryProfileDesired(rulePresets, rateRevisions, qualifications, boundPeriodCaps, boundRotations, boundCounterRules, ruleSourceKeyByBoundEntityKey);
    }
}
