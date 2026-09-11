// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill setting the cron schedule (and, optionally, time zone) for the automatic ERP order import poll.
/// Only an explicitly given time zone is persisted; when omitted, the currently configured time zone
/// (falling back to the company's own configured zone) is used to compute the next run without writing
/// it, so an installation that never configured a cron time zone keeps following its company zone even
/// if that zone changes later.
/// </summary>
/// <param name="cronExpression">Standard 5-field cron expression, e.g. '*/15 * * * *' for every 15 minutes.</param>
/// <param name="timeZoneId">IANA time zone id; when omitted, the currently configured (or company) time zone is used but not persisted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_erp_import_schedule")]
public class SetErpImportScheduleSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICompanyClock _companyClock;
    private readonly IUnitOfWork _unitOfWork;

    public SetErpImportScheduleSkill(
        ISettingsRepository settingsRepository, ICompanyClock companyClock, IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _companyClock = companyClock;
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

        var explicitTimeZoneGiven = !string.IsNullOrWhiteSpace(timeZoneId);
        var resolvedTimeZone = explicitTimeZoneGiven
            ? timeZoneId!
            : await ErpImportCronTimeZone.ResolveAsync(_settingsRepository, _companyClock, cancellationToken);

        if (!CronSchedule.TryNormalizeTimeZoneId(resolvedTimeZone, out var normalizedTimeZone))
        {
            return SkillResult.Error(
                $"Unknown time zone '{resolvedTimeZone}'. Use a valid IANA time zone id (e.g. 'Continent/City').");
        }

        resolvedTimeZone = normalizedTimeZone!;

        var nextRunUtc = CronSchedule.GetNextOccurrenceUtc(cronExpression, resolvedTimeZone, DateTime.UtcNow);
        if (nextRunUtc is null)
        {
            return SkillResult.Error("This schedule has no upcoming occurrence; adjust the cron expression.");
        }

        await UpsertSettingAsync(ErpImportSettingsTypes.CronExpression, cronExpression);
        if (explicitTimeZoneGiven)
        {
            await UpsertSettingAsync(ErpImportSettingsTypes.CronTimeZoneId, resolvedTimeZone);
        }

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
