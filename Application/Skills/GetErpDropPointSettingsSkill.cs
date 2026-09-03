// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the default ERP drop point via GetDefaultQuery: name, source system, bucket prefix, whether
/// it is switched on, when it was last polled and the last error it reported -- plus the resolved
/// absolute on-disk folder path files must be copied to, its processing/processed/error sub-folder
/// names, and the import's poll schedule. The path is resolved through the same object storage
/// resolver the runner and the folder-health check use, so the answer always matches reality.
/// </summary>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_erp_drop_point_settings")]
public class GetErpDropPointSettingsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ISettingsReader _settingsReader;

    public GetErpDropPointSettingsSkill(
        IMediator mediator,
        IObjectStorageService objectStorageService,
        ISettingsReader settingsReader)
    {
        _mediator = mediator;
        _objectStorageService = objectStorageService;
        _settingsReader = settingsReader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);

        if (dropPoint == null)
        {
            return SkillResult.Error("No default drop point is configured.");
        }

        var normalizedPrefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var absolutePath = _objectStorageService.ResolvePath(normalizedPrefix);

        var cronExpression = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value
            ?? ErpImportSettingsTypes.DefaultTimeZoneId;

        var data = new
        {
            dropPoint.Id,
            dropPoint.Name,
            dropPoint.SourceSystemId,
            dropPoint.BucketPrefix,
            dropPoint.IsEnabled,
            dropPoint.LastPolledAt,
            dropPoint.LastError,
            AbsolutePath = absolutePath,
            SubFolders = new
            {
                Processing = ErpImportStorageKeys.ProcessingSegment,
                Processed = ErpImportStorageKeys.ProcessedSegment,
                Error = ErpImportStorageKeys.ErrorSegment
            },
            CronExpression = cronExpression,
            TimeZone = timeZoneId
        };

        var enabledState = dropPoint.IsEnabled ? "switched on" : "switched off";
        var message = $"Default drop point '{dropPoint.Name}' ({enabledState}). Copy ERP order XML files to " +
            $"'{absolutePath}' -- they are picked up at the next scheduled run (cron [{cronExpression}] " +
            $"{timeZoneId}, or trigger it now with trigger_erp_import_run) and end up in its " +
            $"{ErpImportStorageKeys.ProcessedSegment}/ or {ErpImportStorageKeys.ErrorSegment}/ sub-folder; use " +
            "get_erp_import_status for the current counts.";

        return SkillResult.SuccessResult(data, message);
    }
}
