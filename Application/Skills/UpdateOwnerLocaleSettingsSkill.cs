// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for updating the company/owner's locale settings: country, state/canton, time zone and the
/// global holiday calendar used for scheduling. The writes run inside a transaction and every written
/// setting row is re-read through a no-tracking query afterwards; a mismatch rolls the whole batch back.
/// </summary>
/// <param name="country">Country abbreviation (e.g. CH, DE, AT)</param>
/// <param name="state">State or canton abbreviation (e.g. BE, ZH)</param>
/// <param name="timeZone">IANA time zone identifier the company operates in (e.g. Europe/Zurich)</param>
/// <param name="calendarId">ID of the global holiday calendar to apply for scheduling</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_owner_locale_settings")]
public class UpdateOwnerLocaleSettingsSkill : BaseSkillImplementation
{
    private const string SkillName = "update_owner_locale_settings";

    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOwnerLocaleSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var updatedFields = new List<string>();
        var plannedWrites = new List<(string Type, string Value)>();

        var country = GetParameter<string>(parameters, "country");
        if (!string.IsNullOrWhiteSpace(country))
        {
            plannedWrites.Add((Settings.APP_ADDRESS_COUNTRY, country!));
            plannedWrites.Add((SettingKeys.GlobalCalendarCountry, country!));
            updatedFields.Add("country");
        }

        var state = GetParameter<string>(parameters, "state");
        if (!string.IsNullOrWhiteSpace(state))
        {
            plannedWrites.Add((Settings.APP_ADDRESS_STATE, state!));
            plannedWrites.Add((SettingKeys.GlobalCalendarState, state!));
            updatedFields.Add("state");
        }

        var timeZone = GetParameter<string>(parameters, "timeZone");
        if (!string.IsNullOrWhiteSpace(timeZone))
        {
            plannedWrites.Add((Settings.APP_ADDRESS_TIMEZONE, timeZone!));
            updatedFields.Add("timeZone");
        }

        var calendarId = GetParameter<string>(parameters, "calendarId");
        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            plannedWrites.Add((SettingKeys.GlobalCalendarSelectionId, calendarId!));
            updatedFields.Add("calendarId");
        }

        if (updatedFields.Count == 0)
        {
            return SkillResult.Error("No locale settings parameters provided to update.");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in plannedWrites)
                {
                    await UpsertSetting(type, value);
                }

                await _unitOfWork.CompleteAsync();

                foreach (var (type, value) in plannedWrites)
                {
                    await ConfirmPersistedAsync(
                        SkillName,
                        () => _settingsRepository.GetSettingNoTracking(type),
                        persisted => persisted.Value == value,
                        $"the owner locale setting '{type}' = '{value}'");
                }

                return plannedWrites.Count;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields },
            $"Owner locale settings updated ({updatedFields.Count} fields): {string.Join(", ", updatedFields)} " +
            "— all values confirmed in the database (verified).");
    }

    private async Task UpsertSetting(string settingType, string value)
    {
        var existing = await _settingsRepository.GetSetting(settingType);
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            var newSetting = new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = settingType,
                Value = value
            };
            await _settingsRepository.AddSetting(newSetting);
        }
    }
}
