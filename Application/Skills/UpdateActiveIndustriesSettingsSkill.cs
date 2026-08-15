// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets which industry profiles the installation runs on. Slugs are validated against the known ones
/// before writing, so a typo cannot silently empty the selection lists that filter on this value.
/// </summary>
/// <param name="industries">Comma-separated industry slugs, or the custom marker.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_active_industries_settings")]
public class UpdateActiveIndustriesSettingsSkill : SettingsWriterSkillBase
{
    public UpdateActiveIndustriesSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var raw = GetParameter<string>(parameters, "industries");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return SkillResult.Error("Parameter 'industries' is required.");
        }

        var requested = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(slug => slug.ToLowerInvariant())
            .Distinct()
            .ToList();

        var known = IndustrySlugs.All.ToHashSet(StringComparer.OrdinalIgnoreCase);
        known.Add(IndustrySlugs.Custom);
        var unknown = requested.Where(slug => !known.Contains(slug)).ToList();

        if (unknown.Count > 0)
        {
            return SkillResult.Error(
                $"Unknown industry slug(s): {string.Join(", ", unknown)}. " +
                $"Known slugs: {string.Join(", ", IndustrySlugs.All)}, {IndustrySlugs.Custom}.");
        }

        var pending = new List<PendingSetting>
        {
            new("industries", SettingKeys.ActiveIndustries, string.Join(",", requested))
        };

        return await PersistAsync(pending, "Active industries");
    }
}
