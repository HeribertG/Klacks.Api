// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Straight-through order import: for every enabled drop point, lists pending files in its
/// object storage prefix, parses each one, resolves the customer and upserts the order as an
/// unsealed draft. A SealedOrder hit for the same external reference is handed to
/// OrderSupersessionService, which decides whether the payload actually changed anything.
/// Files are moved to a processed/ or error/ sub-prefix after handling so a re-poll never
/// reprocesses them; a file goes to error/ as soon as a single order was rejected or failed,
/// with one erp_import_exception row per affected order.
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
        var nextRunSetting = await _settingsRepository.GetSetting(ErpImportSettingsTypes.NextRunUtc);
        var nowUtc = DateTime.UtcNow;

        DateTime? nextRunUtc = DateTime.TryParse(
            nextRunSetting?.Value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed)
            ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
            : null;

        if (nextRunUtc == null)
        {
            await SaveNextRunAsync(nextRunSetting, CronSchedule.GetNextOccurrenceUtc(cronExpression, timeZoneId, nowUtc));
            return false;
        }

        var decision = new ScheduledTaskDuePolicy().Decide(nextRunUtc, nowUtc, CatchUpWindow);
        if (decision == ScheduledTaskRunDecision.NotDue)
        {
            return false;
        }

        await SaveNextRunAsync(nextRunSetting, CronSchedule.GetNextOccurrenceUtc(cronExpression, timeZoneId, nowUtc));
        return decision == ScheduledTaskRunDecision.Fire;
    }

    private async Task SaveNextRunAsync(Settings? existing, DateTime? nextRunUtc)
    {
        var value = nextRunUtc?.ToString("O") ?? string.Empty;
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Settings { Id = Guid.NewGuid(), Type = ErpImportSettingsTypes.NextRunUtc, Value = value });
        }
    }

    private async Task ProcessDropPointAsync(string sourceSystemId, string bucketPrefix, CancellationToken cancellationToken)
    {
        var prefix = ErpImportStorageKeys.NormalizePrefix(bucketPrefix);
        var keys = await _objectStorageService.ListAsync(prefix, cancellationToken);
        var pendingKeys = keys.Where(k => !IsInSubSegment(k, prefix, ProcessedSegment) && !IsInSubSegment(k, prefix, ErrorSegment));

        foreach (var key in pendingKeys)
        {
            await ProcessFileAsync(sourceSystemId, prefix, key, cancellationToken);
        }
    }

    private async Task ProcessFileAsync(string sourceSystemId, string prefix, string key, CancellationToken cancellationToken)
    {
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
            _logger.LogWarning("ERP import: order {Reference} in {Key} rejected: {Errors}", rejectedOrder.Key, key, reason);
            await RecordExceptionAsync(sourceSystemId, key, rejectedOrder.Key, reason, cancellationToken);
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
                _logger.LogError(ex, "ERP import: order {Reference} in {Key} failed", order.ExternalOrderReference, key);
                await RecordExceptionAsync(sourceSystemId, key, order.ExternalOrderReference, ex.Message, cancellationToken);
            }
        }

        await MoveToAsync(prefix, key, hasOrderFailures ? ErrorSegment : ProcessedSegment, cancellationToken);
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

    private async Task MoveToAsync(string prefix, string key, string segment, CancellationToken cancellationToken)
    {
        var fileName = key[prefix.Length..];
        var destination = ErpImportStorageKeys.SegmentPrefix(prefix, segment) + fileName;
        await _objectStorageService.MoveAsync(key, destination, cancellationToken);
    }
}
