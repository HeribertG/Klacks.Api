// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores how many days before a period starts planning has to be finished (announcement + delivery +
/// review), which widens the window in which the next-period detector reports or starts planning. The
/// lead is computed here from the three inputs — never taken from a model-relayed sum — with the legal
/// minimum publication lead as a floor for the announcement days. The user's consent is not asked for in
/// this class: the skill is classified Sensitive, so the confirmation gate holds every call until the user
/// has said yes, even when the recipe engine forces the step. Zero inputs store 0, which removes the
/// deadline and restores the default window.
/// </summary>
/// <param name="settingsRepository">Reads the compliance minimum and, through the base class, writes the setting</param>
/// <param name="unitOfWork">Commits the write</param>
/// <param name="encryptionService">Storage transform of the settings store</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_planning_deadline_lead")]
public class SetPlanningDeadlineLeadSkill : SettingsWriterSkillBase
{
    private readonly ISettingsRepository _settingsRepository;

    public SetPlanningDeadlineLeadSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var announcement = GetParameter<int?>(parameters, PlanningDeadlineParameters.AnnouncementDays);
        var review = GetParameter<int?>(parameters, PlanningDeadlineParameters.ReviewDays);
        var transit = GetParameter<int?>(parameters, PlanningDeadlineParameters.TransitDays) ?? 0;
        if (announcement is null || review is null)
        {
            return SkillResult.Error(
                $"Missing required value(s): {PlanningDeadlineParameters.AnnouncementDays}, {PlanningDeadlineParameters.ReviewDays}.");
        }

        if (!PlanningDeadlineCalculator.IsValidDays(announcement.Value)
            || !PlanningDeadlineCalculator.IsValidDays(transit)
            || !PlanningDeadlineCalculator.IsValidDays(review.Value))
        {
            return SkillResult.Error(
                $"Every value must be a whole number of days between 0 and {PlanningDeadlineCalculator.MaxDays}.");
        }

        var complianceMin = await ReadPositiveIntAsync(SettingKeys.ComplianceRosterPublicationMinLeadDays);
        var effectiveAnnouncement = PlanningDeadlineCalculator.EffectiveAnnouncement(announcement.Value, complianceMin);
        var leadDays = PlanningDeadlineCalculator.LeadDays(effectiveAnnouncement, transit, review.Value);

        var pending = new List<PendingSetting>
        {
            new(PlanningDeadlineParameters.LeadDays, SettingKeys.PlanningDeadlineLeadDays,
                leadDays.ToString(CultureInfo.InvariantCulture))
        };

        return await PersistAsync(pending, $"Planning deadline lead ({leadDays} days)");
    }

    private async Task<int> ReadPositiveIntAsync(string key)
    {
        var setting = await _settingsRepository.GetSetting(key);
        if (setting?.Value != null
            && int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value > 0)
        {
            return value;
        }

        return 0;
    }
}
