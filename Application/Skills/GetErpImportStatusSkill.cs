// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill reporting the current status of the single ERP order import drop point: enabled
/// state, cron schedule, next scheduled run, and file counts in the inbox/processed/error
/// folders. Read-only.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_erp_import_status")]
public class GetErpImportStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly ISettingsReader _settingsReader;

    public GetErpImportStatusSkill(IMediator mediator, ISettingsReader settingsReader)
    {
        _mediator = mediator;
        _settingsReader = settingsReader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
        var files = await _mediator.Send(new GetDefaultFilesQuery(), cancellationToken);

        var cronExpression = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value
            ?? ErpImportSettingsTypes.DefaultTimeZoneId;
        var nextRunSetting = await _settingsReader.GetSetting(ErpImportSettingsTypes.NextRunUtc);

        string? nextRunLocal = null;
        if (DateTime.TryParse(
                nextRunSetting?.Value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var nextRunUtc))
        {
            nextRunLocal = CronSchedule.FormatLocal(DateTime.SpecifyKind(nextRunUtc, DateTimeKind.Utc), timeZoneId);
        }

        var status = new
        {
            enabled = dropPoint.IsEnabled,
            cronExpression,
            timeZone = timeZoneId,
            nextRun = nextRunLocal,
            pendingFiles = files.Pending.Count,
            processedFiles = files.Processed.Count,
            errorFiles = files.Error.Count,
            latestErrorReason = files.Error.FirstOrDefault()?.ErrorReason
        };

        var message = !dropPoint.IsEnabled
            ? "ERP import is currently disabled."
            : $"ERP import is enabled, schedule [{cronExpression}] ({timeZoneId})" +
              (nextRunLocal is null ? "." : $", next run {nextRunLocal}.") +
              $" Inbox: {files.Pending.Count} pending, {files.Processed.Count} processed, {files.Error.Count} failed.";

        return SkillResult.SuccessResult(status, message);
    }
}
