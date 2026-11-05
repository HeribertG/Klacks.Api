// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the default ERP drop point via GetDefaultQuery: name, source system, bucket prefix, whether
/// it is switched on, when it was last polled and the last error it reported -- plus the resolved
/// absolute on-disk folder path files must be copied to (on Docker also the host folder bound onto it, when the compose
/// file reports it), and the import's poll schedule. Files stay in
/// that folder after their import (no sub-folders); their state lives in the erp_import_file ledger and is
/// reported by get_erp_import_status. The path is resolved through the same object storage
/// resolver the runner and the folder-health check use (ErpDropFolderResolver, shared with the Import settings card). The result also
/// carries the installation kind (native or docker) and the hand-over channels that fit it, so the assistant
/// advises how an ERP gets files to Klacks without guessing: ErpHandoverAdvisor decides, the skill only reports.
/// </summary>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_erp_drop_point_settings")]
public class GetErpDropPointSettingsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<GetErpDropPointSettingsSkill> _logger;
    private readonly ErpCronTimeZoneDriftNotifier _driftNotifier;
    private readonly ErpDropFolderResolver _folderResolver;

    public GetErpDropPointSettingsSkill(
        IMediator mediator,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        ILogger<GetErpDropPointSettingsSkill> logger,
        ErpCronTimeZoneDriftNotifier driftNotifier,
        ErpDropFolderResolver folderResolver)
    {
        _mediator = mediator;
        _settingsReader = settingsReader;
        _logger = logger;
        _driftNotifier = driftNotifier;
        _companyClock = companyClock;
        _folderResolver = folderResolver;
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

        var folder = _folderResolver.Resolve(dropPoint.BucketPrefix);
        var absolutePath = folder.FolderPath;

        var cronExpression = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = await ErpImportCronTimeZone.ResolveAsync(_settingsReader, _companyClock, _logger, _driftNotifier, cancellationToken);

        var guidance = folder.Guidance;
        var stabilitySeconds = (int)ErpImportStorageTiming.WriteStabilityWindow.TotalSeconds;

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
            folder.HostFolderPath,
            CronExpression = cronExpression,
            TimeZone = timeZoneId,
            guidance.Installation,
            guidance.RecommendedChannels,
            guidance.FolderDropRequirement
        };

        var enabledState = dropPoint.IsEnabled ? "switched on" : "switched off";
        var message = $"Default drop point '{dropPoint.Name}' ({enabledState}). Folder as the Klacks server sees it: " +
            $"'{absolutePath}' -- files placed there are picked up at the next scheduled run (cron [{cronExpression}] " +
            $"{timeZoneId}, or trigger it now with trigger_erp_import_run) once they have been unchanged for " +
            $"{stabilitySeconds} seconds. Only XML in the ErpOrderImport format is imported, as unsealed order drafts " +
            "that a planner seals. The files stay in that folder; each one's import state (waiting, importing, " +
            "processed, failed) is tracked per file -- use get_erp_import_status for the current counts. " +
            guidance.Advice;

        return SkillResult.SuccessResult(data, message);
    }
}
