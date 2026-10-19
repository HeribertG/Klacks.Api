// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill reporting the current status of the single ERP order import drop point: enabled
/// state, cron schedule, next scheduled run, the number of files per state from the erp_import_file
/// ledger (waiting in the inbox, importing, processed, failed -- uncapped, an import without heartbeat
/// for ErpImportFileTiming.StaleAfter counts as failed) and a summary of the latest finished file.
/// Read-only.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_erp_import_status")]
public class GetErpImportStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IErpImportFileRepository _fileRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<GetErpImportStatusSkill> _logger;
    private readonly ErpCronTimeZoneDriftNotifier _driftNotifier;

    public GetErpImportStatusSkill(
        IMediator mediator,
        IErpImportFileRepository fileRepository,
        TimeProvider timeProvider,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        ILogger<GetErpImportStatusSkill> logger,
        ErpCronTimeZoneDriftNotifier driftNotifier)
    {
        _mediator = mediator;
        _fileRepository = fileRepository;
        _timeProvider = timeProvider;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _logger = logger;
        _driftNotifier = driftNotifier;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
        var staleCutoff = _timeProvider.GetUtcNow().UtcDateTime - ErpImportFileTiming.StaleAfter;
        var counts = await _fileRepository.CountByStatusAsync(dropPoint.Id, staleCutoff, cancellationToken);
        var latestFinished = await _fileRepository.GetLatestFinishedAsync(dropPoint.Id, cancellationToken);

        var cronExpression = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = await ErpImportCronTimeZone.ResolveAsync(_settingsReader, _companyClock, _logger, _driftNotifier, cancellationToken);
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
            inboxFiles = counts.Received,
            importingFiles = counts.Importing,
            processedFiles = counts.Processed,
            failedFiles = counts.Failed,
            latestFinished = latestFinished == null ? null : new
            {
                latestFinished.FileName,
                Status = latestFinished.Status.ToString(),
                FinishedAt = latestFinished.FinishedAt is { } finishedAt ? CronSchedule.FormatLocal(finishedAt, timeZoneId) : null,
                latestFinished.TotalOrders,
                latestFinished.NewCount,
                latestFinished.UpdatedCount,
                latestFinished.UnchangedCount,
                latestFinished.SupersededCount,
                latestFinished.RejectedCount,
                latestFinished.FailureReason
            }
        };

        var fileSummary =
            $" Files: {counts.Received} waiting, {counts.Importing} importing, {counts.Processed} processed, {counts.Failed} failed." +
            DescribeLatestFinished(latestFinished, timeZoneId);

        var message = !dropPoint.IsEnabled
            ? "ERP import is currently disabled." + fileSummary
            : $"ERP import is enabled, schedule [{cronExpression}] ({timeZoneId})" +
              (nextRunLocal is null ? "." : $", next run {nextRunLocal}.") +
              fileSummary;

        return SkillResult.SuccessResult(status, message);
    }

    private static string DescribeLatestFinished(ErpImportFile? file, string timeZoneId)
    {
        if (file == null)
        {
            return string.Empty;
        }

        var finishedAt = file.FinishedAt is { } finished ? $" at {CronSchedule.FormatLocal(finished, timeZoneId)}" : string.Empty;
        var outcome = file.Status is ErpImportFileStatus.Failed or ErpImportFileStatus.Interrupted
            ? $": {file.FailureReason}"
            : $": {file.NewCount} new, {file.UpdatedCount} updated, {file.UnchangedCount} unchanged, " +
              $"{file.SupersededCount} superseded, {file.RejectedCount} rejected";

        return $" Latest finished file '{file.FileName}' {file.Status}{finishedAt}{outcome}.";
    }
}
