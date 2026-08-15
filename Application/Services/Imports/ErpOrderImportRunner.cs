// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Straight-through order import: for every enabled drop point, lists pending files in its
/// object storage prefix, parses each one, resolves the customer and upserts the order as an
/// unsealed draft. A SealedOrder hit for the same external reference is handed to
/// OrderSupersessionService, which decides whether the payload actually changed anything.
/// A file is claimed into a processing/ sub-prefix before it is parsed, so a concurrent run --
/// a second API instance sharing the drop point volume -- cannot import the same orders twice;
/// the claim is an atomic move, and the caller that loses it skips the file. After handling, the
/// file moves on to a processed/ or error/ sub-prefix, so a re-poll never reprocesses it; a file
/// goes to error/ as soon as a single order was rejected or failed, with one erp_import_exception
/// row per affected order. A crash between claim and completion parks the file under processing/,
/// where no later run picks it up again: unprocessed rather than imported twice. The drop point view
/// lists such a file among the errors so it stays visible, and it can be removed there and re-uploaded.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Services.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpOrderImportRunner : IErpOrderImportRunner
{
    private const string ProcessedSegment = ErpImportStorageKeys.ProcessedSegment;
    private const string ErrorSegment = ErpImportStorageKeys.ErrorSegment;
    private const string ProcessingSegment = ErpImportStorageKeys.ProcessingSegment;
    private const string ReasonSeparator = "; ";
    private static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(1);

    private readonly IErpDropPointRepository _dropPointRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IOrderImportParser _parser;
    private readonly ErpCustomerResolver _customerResolver;
    private readonly IShiftRepository _shiftRepository;
    private readonly OrderSupersessionService _supersessionService;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IAgentTriggerService _triggerService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ErpOrderImportRunner> _logger;

    public ErpOrderImportRunner(
        IErpDropPointRepository dropPointRepository,
        IObjectStorageService objectStorageService,
        IOrderImportParser parser,
        ErpCustomerResolver customerResolver,
        IShiftRepository shiftRepository,
        OrderSupersessionService supersessionService,
        IErpImportExceptionRepository exceptionRepository,
        IAgentTriggerService triggerService,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<ErpOrderImportRunner> logger)
    {
        _dropPointRepository = dropPointRepository;
        _objectStorageService = objectStorageService;
        _parser = parser;
        _customerResolver = customerResolver;
        _shiftRepository = shiftRepository;
        _supersessionService = supersessionService;
        _exceptionRepository = exceptionRepository;
        _triggerService = triggerService;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsDueAsync())
        {
            return;
        }

        var dropPoints = await _dropPointRepository.List();
        foreach (var dropPoint in dropPoints.Where(d => d.IsEnabled))
        {
            await ProcessDropPointAsync(dropPoint.SourceSystemId, dropPoint.BucketPrefix, cancellationToken);
        }
    }

    private async Task<bool> IsDueAsync()
    {
        var cronExpression = (await _settingsRepository.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = (await _settingsRepository.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value
            ?? ErpImportSettingsTypes.DefaultTimeZoneId;
        // Read untracked on purpose: the occurrence is claimed through a conditional update that
        // bypasses the change tracker, so a tracked instance would keep the pre-claim value and any
        // later save in the same scope would silently roll the claim back.
        var nextRunSetting = await _settingsRepository.GetSettingNoTracking(ErpImportSettingsTypes.NextRunUtc);
        var nowUtc = DateTime.UtcNow;

        DateTime? nextRunUtc = DateTime.TryParse(
            nextRunSetting?.Value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;

        var nextOccurrence = FormatNextRun(CronSchedule.GetNextOccurrenceUtc(cronExpression, timeZoneId, nowUtc));

        if (nextRunUtc == null)
        {
            await SeedNextRunAsync(nextRunSetting, nextOccurrence);
            return false;
        }

        var decision = new ScheduledTaskDuePolicy().Decide(nextRunUtc, nowUtc, CatchUpWindow);
        if (decision == ScheduledTaskRunDecision.NotDue)
        {
            return false;
        }

        var claimed = await _settingsRepository.TryAdvanceSettingAsync(
            ErpImportSettingsTypes.NextRunUtc,
            nextRunSetting!.Value,
            nextOccurrence);

        if (!claimed)
        {
            _logger.LogInformation("ERP import: this occurrence was already claimed elsewhere, skipping");
            return false;
        }

        return decision == ScheduledTaskRunDecision.Fire;
    }

    private static string FormatNextRun(DateTime? nextRunUtc)
    {
        return nextRunUtc?.ToString("O") ?? string.Empty;
    }

    private async Task SeedNextRunAsync(Settings? existing, string value)
    {
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Settings { Id = Guid.NewGuid(), Type = ErpImportSettingsTypes.NextRunUtc, Value = value });
        }

        await _unitOfWork.CompleteAsync();
    }

    private async Task ProcessDropPointAsync(string sourceSystemId, string bucketPrefix, CancellationToken cancellationToken)
    {
        var prefix = ErpImportStorageKeys.NormalizePrefix(bucketPrefix);
        var keys = await _objectStorageService.ListAsync(prefix, cancellationToken);
        var pendingKeys = keys.Where(k => !IsInSubSegment(k, prefix, ProcessedSegment)
            && !IsInSubSegment(k, prefix, ErrorSegment)
            && !IsInSubSegment(k, prefix, ProcessingSegment));

        foreach (var key in pendingKeys)
        {
            var fileName = key[prefix.Length..];
            var claimedKey = ErpImportStorageKeys.SegmentPrefix(prefix, ProcessingSegment) + fileName;

            if (!await _objectStorageService.TryClaimAsync(key, claimedKey, cancellationToken))
            {
                _logger.LogInformation("ERP import: file {Key} is already being processed elsewhere, skipping", key);
                continue;
            }

            await ProcessFileAsync(sourceSystemId, prefix, claimedKey, fileName, cancellationToken);
        }
    }

    private async Task ProcessFileAsync(string sourceSystemId, string prefix, string key, string fileName, CancellationToken cancellationToken)
    {
        // Exceptions are recorded against the key the file had before it was claimed: that is the key
        // the drop point view reconstructs from the error/ segment when it looks up a reason, and the
        // key an operator sees. The claimed key exists only for the duration of this run.
        var reportedKey = prefix + fileName;

        OrderImportParseResult result;
        await using (var stream = await _objectStorageService.DownloadAsync(key, cancellationToken))
        {
            result = _parser.Parse(stream);
        }

        var hasOrderFailures = false;

        foreach (var rejectedOrder in result.Errors.GroupBy(e => e.ExternalOrderReference))
        {
            hasOrderFailures = true;
            var reason = string.Join(ReasonSeparator, rejectedOrder.Select(e => e.Message));
            _logger.LogWarning("ERP import: order {Reference} in {Key} rejected: {Errors}", rejectedOrder.Key, reportedKey, reason);
            await RecordExceptionAsync(sourceSystemId, reportedKey, rejectedOrder.Key, reason, cancellationToken);
        }

        foreach (var order in result.Orders)
        {
            try
            {
                await ProcessOrderAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                hasOrderFailures = true;
                _logger.LogError(ex, "ERP import: order {Reference} in {Key} failed", order.ExternalOrderReference, reportedKey);
                await RecordExceptionAsync(sourceSystemId, reportedKey, order.ExternalOrderReference, ex.Message, cancellationToken);
            }
        }

        await MoveToAsync(prefix, key, fileName, hasOrderFailures ? ErrorSegment : ProcessedSegment, cancellationToken);
    }

    private async Task RecordExceptionAsync(string sourceSystemId, string key, string? externalOrderReference, string reason, CancellationToken cancellationToken)
    {
        var exception = new ErpImportException
        {
            Id = Guid.NewGuid(),
            SourceSystemId = sourceSystemId,
            FileKey = key,
            ExternalOrderReference = externalOrderReference,
            Reason = reason
        };

        await _exceptionRepository.Add(exception);
        await _unitOfWork.CompleteAsync();

        await _triggerService.OnEventAsync(new OrderImportFailedTriggerEvent(exception.Id, key, reason), cancellationToken);
    }

    private async Task ProcessOrderAsync(ImportedOrderPayload order, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.ExecuteInTransactionAsync(() => _customerResolver.ResolveAsync(order.Customer, cancellationToken));

        var existing = await _shiftRepository.FindActiveByExternalReferenceAsync(order.SourceSystemId, order.ExternalOrderReference, cancellationToken);

        if (existing == null)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _shiftRepository.AddWithSealedOrderHandling(ImportedOrderShiftMapper.BuildDraft(order, client.Id));
                await _unitOfWork.CompleteAsync();
                return true;
            });
        }
        else if (existing.Status == ShiftStatus.SealedOrder)
        {
            // OrderSupersessionService manages its own transaction -- must not nest inside another.
            await _supersessionService.HandleAsync(existing, order, client.Id, cancellationToken);
        }
        else
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                ImportedOrderShiftMapper.ApplyToDraft(existing, order, client.Id);
                await _shiftRepository.PutWithSealedOrderHandling(existing);
                await _unitOfWork.CompleteAsync();
                return true;
            });
        }
    }

    private static bool IsInSubSegment(string key, string prefix, string segment)
    {
        return key.StartsWith(ErpImportStorageKeys.SegmentPrefix(prefix, segment), StringComparison.Ordinal);
    }

    private async Task MoveToAsync(string prefix, string key, string fileName, string segment, CancellationToken cancellationToken)
    {
        var destination = ErpImportStorageKeys.SegmentPrefix(prefix, segment) + fileName;
        await _objectStorageService.MoveAsync(key, destination, cancellationToken);
    }
}
