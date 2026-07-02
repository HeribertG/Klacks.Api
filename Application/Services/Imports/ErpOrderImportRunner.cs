// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Straight-through order import: for every enabled drop point, lists pending files in its
/// object storage prefix, parses each one, resolves the customer and upserts the order as an
/// unsealed draft. A SealedOrder hit for the same external reference is left untouched here --
/// that supersession decision belongs to OrderSupersessionService (a later phase). Files are
/// moved to a processed/ or error/ sub-prefix after handling so a re-poll never reprocesses them.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Services.Imports;

public class ErpOrderImportRunner : IErpOrderImportRunner
{
    private const string ProcessedSegment = "processed";
    private const string ErrorSegment = "error";
    private static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(1);

    private readonly IErpDropPointRepository _dropPointRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IOrderImportParser _parser;
    private readonly ErpCustomerResolver _customerResolver;
    private readonly IShiftRepository _shiftRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ErpOrderImportRunner> _logger;

    public ErpOrderImportRunner(
        IErpDropPointRepository dropPointRepository,
        IObjectStorageService objectStorageService,
        IOrderImportParser parser,
        ErpCustomerResolver customerResolver,
        IShiftRepository shiftRepository,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<ErpOrderImportRunner> logger)
    {
        _dropPointRepository = dropPointRepository;
        _objectStorageService = objectStorageService;
        _parser = parser;
        _customerResolver = customerResolver;
        _shiftRepository = shiftRepository;
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
            await ProcessDropPointAsync(dropPoint.Id, dropPoint.BucketPrefix, cancellationToken);
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

    private async Task ProcessDropPointAsync(Guid dropPointId, string bucketPrefix, CancellationToken cancellationToken)
    {
        var prefix = bucketPrefix.TrimEnd('/') + "/";
        var keys = await _objectStorageService.ListAsync(prefix, cancellationToken);
        var pendingKeys = keys.Where(k => !IsInSubSegment(k, prefix, ProcessedSegment) && !IsInSubSegment(k, prefix, ErrorSegment));

        foreach (var key in pendingKeys)
        {
            await ProcessFileAsync(prefix, key, cancellationToken);
        }
    }

    private async Task ProcessFileAsync(string prefix, string key, CancellationToken cancellationToken)
    {
        OrderImportParseResult result;
        await using (var stream = await _objectStorageService.DownloadAsync(key, cancellationToken))
        {
            result = _parser.Parse(stream);
        }

        if (result.Orders.Count == 0 && result.HasErrors)
        {
            _logger.LogWarning(
                "ERP import: {Key} failed root validation: {Errors}",
                key, string.Join("; ", result.Errors.Select(e => e.Message)));
            await MoveToAsync(prefix, key, ErrorSegment, cancellationToken);
            return;
        }

        foreach (var order in result.Orders)
        {
            try
            {
                await ProcessOrderAsync(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERP import: order {Reference} in {Key} failed", order.ExternalOrderReference, key);
            }
        }

        if (result.HasErrors)
        {
            _logger.LogWarning("ERP import: {Key} had {Count} order-level validation error(s)", key, result.Errors.Count);
        }

        await MoveToAsync(prefix, key, ProcessedSegment, cancellationToken);
    }

    private async Task ProcessOrderAsync(ImportedOrderPayload order, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var client = await _customerResolver.ResolveAsync(order.Customer, cancellationToken);

            var existing = await _shiftRepository.FindActiveByExternalReferenceAsync(order.SourceSystemId, order.ExternalOrderReference, cancellationToken);

            if (existing == null)
            {
                await _shiftRepository.AddWithSealedOrderHandling(BuildShift(order, client.Id));
            }
            else if (existing.Status == ShiftStatus.SealedOrder)
            {
                _logger.LogInformation(
                    "ERP import: order {Reference} is already sealed; update deferred to supersession handling",
                    order.ExternalOrderReference);
            }
            else
            {
                ApplyToExistingDraft(existing, order, client.Id);
                await _shiftRepository.PutWithSealedOrderHandling(existing);
            }

            await _unitOfWork.CompleteAsync();
            return true;
        });
    }

    private static Shift BuildShift(ImportedOrderPayload order, Guid clientId)
    {
        var shift = new Shift { Id = Guid.NewGuid(), Status = ShiftStatus.OriginalOrder };
        ApplyToExistingDraft(shift, order, clientId);
        return shift;
    }

    private static void ApplyToExistingDraft(Shift shift, ImportedOrderPayload order, Guid clientId)
    {
        shift.ClientId = clientId;
        shift.SourceSystemId = order.SourceSystemId;
        shift.ExternalOrderReference = order.ExternalOrderReference;
        shift.Name = $"ERP {order.ExternalOrderReference}";
        shift.Abbreviation = order.ExternalOrderReference;
        shift.FromDate = order.FromDate;
        shift.UntilDate = order.UntilDate;
        shift.StartShift = order.StartTime;
        shift.EndShift = order.EndTime;
        shift.IsTimeRange = order.IsTimeRange;
        shift.IsMonday = order.IsMonday;
        shift.IsTuesday = order.IsTuesday;
        shift.IsWednesday = order.IsWednesday;
        shift.IsThursday = order.IsThursday;
        shift.IsFriday = order.IsFriday;
        shift.IsSaturday = order.IsSaturday;
        shift.IsSunday = order.IsSunday;
        shift.Quantity = order.Quantity;
        shift.SumEmployees = order.SumEmployees;
    }

    private static bool IsInSubSegment(string key, string prefix, string segment)
    {
        return key.StartsWith(prefix + segment + "/", StringComparison.Ordinal);
    }

    private async Task MoveToAsync(string prefix, string key, string segment, CancellationToken cancellationToken)
    {
        var fileName = key[prefix.Length..];
        var destination = $"{prefix}{segment}/{fileName}";
        await _objectStorageService.MoveAsync(key, destination, cancellationToken);
    }
}
