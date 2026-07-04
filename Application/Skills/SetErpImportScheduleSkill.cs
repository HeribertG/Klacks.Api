// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill setting the cron schedule (and time zone) for the automatic ERP order import poll.
/// </summary>
/// <param name="cronExpression">Standard 5-field cron expression, e.g. '*/15 * * * *' for every 15 minutes.</param>
/// <param name="timeZoneId">IANA time zone id; defaults to the currently configured time zone when omitted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_erp_import_schedule")]
public class SetErpImportScheduleSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetErpImportScheduleSkill(ISettingsRepository settingsRepository, IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var cronExpression = GetRequiredString(parameters, "cronExpression").Trim();
        var timeZoneId = GetParameter<string>(parameters, "timeZoneId")?.Trim();

        if (!CronSchedule.IsValidExpression(cronExpression))
        {
            return SkillResult.Error(
                $"Invalid cron expression '{cronExpression}'. Use standard 5-field cron, e.g. '*/15 * * * *' for every 15 minutes.");
        }

        var resolvedTimeZone = string.IsNullOrWhiteSpace(timeZoneId)
            ? (await _settingsRepository.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value ?? ErpImportSettingsTypes.DefaultTimeZoneId
            : timeZoneId;

        if (!CronSchedule.IsValidTimeZone(resolvedTimeZone))
        {
            return SkillResult.Error($"Unknown time zone '{resolvedTimeZone}'. Use an IANA id such as 'Europe/Zurich'.");
        }

        var nextRunUtc = CronSchedule.GetNextOccurrenceUtc(cronExpression, resolvedTimeZone, DateTime.UtcNow);
        if (nextRunUtc is null)
        {
            return SkillResult.Error("This schedule has no upcoming occurrence; adjust the cron expression.");
        }

        await UpsertSettingAsync(ErpImportSettingsTypes.CronExpression, cronExpression);
        await UpsertSettingAsync(ErpImportSettingsTypes.CronTimeZoneId, resolvedTimeZone);
        await UpsertSettingAsync(ErpImportSettingsTypes.NextRunUtc, nextRunUtc.Value.ToString("O"));
        await _unitOfWork.CompleteAsync();

        var nextRunLocal = CronSchedule.FormatLocal(nextRunUtc.Value, resolvedTimeZone);

        return SkillResult.SuccessResult(
            new { cronExpression, timeZone = resolvedTimeZone, nextRun = nextRunLocal },
            $"ERP import schedule set to [{cronExpression}] in {resolvedTimeZone}. Next run: {nextRunLocal}.");
    }

    private async Task UpsertSettingAsync(string type, string value)
    {
        var existing = await _settingsRepository.GetSetting(type);
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = type,
                Value = value
            });
        }
    }
}
