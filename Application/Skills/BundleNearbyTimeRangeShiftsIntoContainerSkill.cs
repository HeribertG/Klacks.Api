// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Self-verifying skill that bundles all nearby mobile TimeRange services around a location into ONE
/// new container shift: it creates a sealed container order (ShiftType.IsContainer), runs
/// IContainerAutofillService to select and route-optimize the matching TimeRange shifts for exactly one
/// weekday, and persists the resulting weekday template — all inside a single database transaction that
/// rolls back completely (no orphaned empty container) when nothing matches or the write cannot be
/// confirmed. For other weekdays, call this skill again with a different weekday and the same location.
/// </summary>
/// <param name="location">City/place/address used as both StartBase and EndBase of the route.</param>
/// <param name="weekday">ISO weekday 1=Monday..7=Sunday to fill the template for.</param>
/// <param name="fromTime">Start of the day's time budget (HH:mm).</param>
/// <param name="untilTime">End of the day's time budget (HH:mm); must be after fromTime.</param>
/// <param name="fromDate">Date from which the container order is valid (YYYY-MM-DD).</param>
/// <param name="untilDate">Optional end date of validity (YYYY-MM-DD).</param>
/// <param name="transportMode">Optional: car|bicycle|foot|mix; defaults to car. Non-car modes require a
/// configured OpenRouteService API key and OpenRouteService not being switched off by the operator.</param>
/// <param name="timeRangeTolerance">Optional strictness of the task TimeRange windows (1.0 strict, lower values widen
/// the window by (1 - value) x its length, 0 disables the check); defaults to 0.5.</param>
/// <param name="groupIds">Optional comma-separated group UUIDs restricting which candidate services are
/// considered; without any group the container has no group filter (all matching tasks are eligible).</param>
/// <param name="name">Optional container name; defaults to "Mobile Dienste {location}".</param>
/// <param name="groupVisibilityGuard">Answers a group a restricted caller may not see exactly like a missing group</param>
/// <param name="shiftVisibilityGuard">Drops autofill picks a restricted caller may not see, so a hidden task shift is
/// never written into the template, reported back or named in an error</param>
/// <param name="defaultShiftMacroResolver">Supplies the default Shift-category macro of the new container, exactly as a
/// container created in the shift editor receives it</param>

using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Extensions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("bundle_nearby_timerange_shifts_into_container")]
public class BundleNearbyTimeRangeShiftsIntoContainerSkill : BaseSkillImplementation
{
    private const string SkillName = "bundle_nearby_timerange_shifts_into_container";
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;
    private const int DefaultSumEmployees = 1;
    private const int DefaultQuantity = 1;
    private const double DefaultTimeRangeTolerance = 0.5;
    private const string TransportModeCar = "car";
    private const string NoMatchingServicesMessage =
        "No matching mobile TimeRange services were found around '{0}' within the given time budget — no container was created.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IContainerAutofillService _containerAutofillService;
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly IGeoServicesPolicy _geoServicesPolicy;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;

    public BundleNearbyTimeRangeShiftsIntoContainerSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        IUserService userService,
        IContainerAutofillService containerAutofillService,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        IGeoServicesPolicy geoServicesPolicy,
        IGroupVisibilityGuard groupVisibilityGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IDefaultShiftMacroResolver defaultShiftMacroResolver)
    {
        _geoServicesPolicy = geoServicesPolicy;
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _userService = userService;
        _containerAutofillService = containerAutofillService;
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
        _groupVisibilityGuard = groupVisibilityGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
    }

    private async Task<SkillResult?> CheckTransportModeAvailableAsync(ContainerTransportMode transportMode, string? transportModeStr)
    {
        if (transportMode == ContainerTransportMode.ByCar)
        {
            return null;
        }

        if (!_geoServicesPolicy.OpenRouteService.IsEnabled)
        {
            return SkillResult.Error(
                $"Cannot use transportMode '{transportModeStr}' — {GeoServiceMessages.OpenRouteServiceDisabled}, so only " +
                "transportMode=car is available. Call this skill again with transportMode=car.");
        }

        var apiKey = await _settingsReader.GetOpenRouteServiceApiKeyAsync(_encryptionService);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return SkillResult.Error(
                $"Cannot use transportMode '{transportModeStr}' — no OpenRouteService API key is configured, so only " +
                "transportMode=car (driving times via OSRM/estimation) is available. Configure the OpenRouteService " +
                "API key in Settings first, or call this skill again with transportMode=car.");
        }

        return null;
    }

    internal sealed record BundleOutcome(Guid ContainerId, string ContainerName, ContainerAutofillResult AutofillResult);

    private sealed record BundleRequest(
        string Location,
        int IsoWeekday,
        int StorageWeekday,
        TimeOnly FromTime,
        TimeOnly UntilTime,
        DateOnly FromDate,
        DateOnly? UntilDate,
        string TransportModeText,
        ContainerTransportMode TransportMode,
        double TimeRangeTolerance,
        string? GroupIdsRaw,
        string Name);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (request, requestError) = ParseRequest(parameters);
        if (request is null)
        {
            return SkillResult.Error(requestError!);
        }

        var unavailableTransportMode = await CheckTransportModeAvailableAsync(request.TransportMode, request.TransportModeText);
        if (unavailableTransportMode != null)
        {
            return unavailableTransportMode;
        }

        var (groupItems, groupError) = await ResolveGroupItemsAsync(request.GroupIdsRaw, request.FromDate, cancellationToken);
        if (groupItems is null)
        {
            return SkillResult.Error(groupError!);
        }

        var defaultMacroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);
        var containerShift = BuildContainerShift(request, groupItems, defaultMacroId, context.UserName);

        var instanceId = _userService.GetInstanceId() ?? string.Empty;

        BundleOutcome outcome;
        try
        {
            outcome = await _unitOfWork.ExecuteInTransactionAsync(
                () => CreateAndFillContainerAsync(request, containerShift, instanceId, cancellationToken));
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return BuildSuccessResult(request, outcome);
    }

    private static (BundleRequest? Request, string? Error) ParseRequest(Dictionary<string, object> parameters)
    {
        var location = GetRequiredString(parameters, "location");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var fromTimeStr = GetRequiredString(parameters, "fromTime");
        var untilTimeStr = GetRequiredString(parameters, "untilTime");
        var fromDateStr = GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var transportModeStr = GetParameter<string>(parameters, "transportMode") ?? TransportModeCar;
        var timeRangeTolerance = GetParameter<double>(parameters, "timeRangeTolerance", DefaultTimeRangeTolerance);
        var groupIdsRaw = GetParameter<string>(parameters, "groupIds");
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Mobile Dienste {location}";
        }

        if (isoWeekday < MinIsoWeekday || isoWeekday > MaxIsoWeekday)
        {
            return (null, "weekday must be between 1 (Monday) and 7 (Sunday).");
        }

        var (fromTime, untilTime, timeError) = ParseTimeBudget(fromTimeStr, untilTimeStr);
        if (timeError != null)
        {
            return (null, timeError);
        }

        var (fromDate, untilDate, dateError) = ParseValidity(fromDateStr, untilDateStr);
        if (dateError != null)
        {
            return (null, dateError);
        }

        var (transportMode, transportModeError) = ParseTransportMode(transportModeStr);
        if (transportMode is null)
        {
            return (null, transportModeError!);
        }

        var storageWeekday = isoWeekday == IsoSunday ? StorageSunday : isoWeekday;

        return (new BundleRequest(
            location, isoWeekday, storageWeekday, fromTime, untilTime, fromDate, untilDate,
            transportModeStr, transportMode.Value, timeRangeTolerance, groupIdsRaw, name), null);
    }

    private static (TimeOnly FromTime, TimeOnly UntilTime, string? Error) ParseTimeBudget(
        string fromTimeStr, string untilTimeStr)
    {
        if (!TimeOnly.TryParse(fromTimeStr, out var fromTime))
        {
            return (default, default, $"Invalid fromTime format: {fromTimeStr}. Expected HH:mm");
        }

        if (!TimeOnly.TryParse(untilTimeStr, out var untilTime))
        {
            return (default, default, $"Invalid untilTime format: {untilTimeStr}. Expected HH:mm");
        }

        if (untilTime.ToTimeSpan() <= fromTime.ToTimeSpan())
        {
            return (default, default, $"untilTime ({untilTimeStr}) must be after fromTime ({fromTimeStr}).");
        }

        return (fromTime, untilTime, null);
    }

    private static (DateOnly FromDate, DateOnly? UntilDate, string? Error) ParseValidity(
        string fromDateStr, string? untilDateStr)
    {
        if (!DateOnly.TryParse(fromDateStr, out var fromDate))
        {
            return (default, null, $"Invalid fromDate format: {fromDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrWhiteSpace(untilDateStr))
        {
            if (!DateOnly.TryParse(untilDateStr, out var parsedUntilDate))
            {
                return (default, null, $"Invalid untilDate format: {untilDateStr}. Expected YYYY-MM-DD.");
            }

            untilDate = parsedUntilDate;
            if (untilDate.Value < fromDate)
            {
                return (default, null,
                    $"untilDate ({untilDate:yyyy-MM-dd}) must not be before fromDate ({fromDate:yyyy-MM-dd}).");
            }
        }

        return (fromDate, untilDate, null);
    }

    private async Task<(List<GroupItem>? GroupItems, string? Error)> ResolveGroupItemsAsync(
        string? groupIdsRaw, DateOnly fromDate, CancellationToken cancellationToken)
    {
        var groupItems = new List<GroupItem>();
        if (!string.IsNullOrEmpty(groupIdsRaw))
        {
            var groupIdStrings = groupIdsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var groupIdStr in groupIdStrings)
            {
                if (!Guid.TryParse(groupIdStr, out var groupId))
                {
                    continue;
                }

                var group = await _groupRepository.Get(groupId);
                if (group == null || !await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
                {
                    return (null, $"Group with ID {groupId} not found.");
                }

                groupItems.Add(new GroupItem
                {
                    GroupId = groupId,
                    ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                });
            }
        }

        return (groupItems, null);
    }

    private static Shift BuildContainerShift(
        BundleRequest request, List<GroupItem> groupItems, Guid? defaultMacroId, string userName)
    {
        var (name, fromTime, untilTime) = (request.Name, request.FromTime, request.UntilTime);
        var (isMonday, isTuesday, isWednesday, isThursday, isFriday, isSaturday, isSunday) = BuildIsoWeekdayFlags(request.IsoWeekday);

        return new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = GenerateAbbreviation(name),
            Description = string.Empty,
            ClientId = null,
            MacroId = defaultMacroId,
            Status = ShiftStatus.SealedOrder,
            ShiftType = ShiftType.IsContainer,
            IsTimeRange = false,
            StartShift = fromTime,
            EndShift = untilTime,
            FromDate = request.FromDate,
            UntilDate = request.UntilDate,
            WorkTime = CalculateWorkTime(fromTime, untilTime),
            SumEmployees = DefaultSumEmployees,
            Quantity = DefaultQuantity,
            IsMonday = isMonday,
            IsTuesday = isTuesday,
            IsWednesday = isWednesday,
            IsThursday = isThursday,
            IsFriday = isFriday,
            IsSaturday = isSaturday,
            IsSunday = isSunday,
            GroupItems = groupItems,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = userName
        };
    }

    /// <summary>
    /// Runs inside the transaction: persists the container, autofills it, and writes and confirms its weekday
    /// template while holding the container lock. Any SkillVerificationException rolls the whole operation back.
    /// </summary>
    /// <param name="request">Validated skill input</param>
    /// <param name="containerShift">The sealed container order to persist</param>
    /// <param name="instanceId">Instance id the container lock is acquired under</param>
    private async Task<BundleOutcome> CreateAndFillContainerAsync(
        BundleRequest request, Shift containerShift, string instanceId, CancellationToken cancellationToken)
    {
        var resultShift = await _shiftRepository.AddWithSealedOrderHandling(containerShift);
        await _unitOfWork.CompleteAsync();

        var containerId = resultShift.Id;

        var autofillResult = await AutofillVisibleSelectionAsync(containerId, request, cancellationToken);
        if (autofillResult.SelectedShiftIds.Count == 0)
        {
            throw new SkillVerificationException(SkillName, string.Format(NoMatchingServicesMessage, request.Location));
        }

        var lockResult = await _mediator.Send(
            new AcquireContainerLockCommand(ContainerLockResourceTypes.ContainerTemplate, containerId, instanceId), cancellationToken);

        if (!lockResult.Acquired)
        {
            throw new SkillVerificationException(
                SkillName,
                "The newly created container could not be locked for writing its template — the operation was rolled back.");
        }

        try
        {
            await WriteAndConfirmTemplateAsync(containerId, request, autofillResult.SelectedShiftIds, cancellationToken);

            return new BundleOutcome(containerId, resultShift.Name, autofillResult);
        }
        finally
        {
            await ReleaseLockQuietlyAsync(lockResult.Id, cancellationToken);
        }
    }

    private async Task<ContainerAutofillResult> AutofillVisibleSelectionAsync(
        Guid containerId, BundleRequest request, CancellationToken cancellationToken)
    {
        var (storageWeekday, location) = (request.StorageWeekday, request.Location);
        var (fromTime, untilTime, timeRangeTolerance) = (request.FromTime, request.UntilTime, request.TimeRangeTolerance);

        var rawAutofillResult = await _containerAutofillService.AutofillAsync(new ContainerAutofillRequest(
            ContainerId: containerId,
            Weekday: storageWeekday,
            IsHoliday: false,
            StartBase: location,
            EndBase: location,
            FromTime: fromTime,
            UntilTime: untilTime,
            TransportMode: request.TransportMode,
            TimeRangeTolerance: timeRangeTolerance,
            CancellationToken: cancellationToken));

        return await KeepVisibleSelectionAsync(rawAutofillResult, cancellationToken);
    }

    private async Task WriteAndConfirmTemplateAsync(
        Guid containerId, BundleRequest request, List<Guid> selectedShiftIds, CancellationToken cancellationToken)
    {
        var (isoWeekday, storageWeekday, location) = (request.IsoWeekday, request.StorageWeekday, request.Location);

        var containerTemplateItems = await BuildTemplateItemsAsync(selectedShiftIds);

        var templateResource = new ContainerTemplateResource
        {
            Id = Guid.Empty,
            ContainerId = containerId,
            FromTime = request.FromTime,
            UntilTime = request.UntilTime,
            Weekday = storageWeekday,
            IsHoliday = false,
            IsWeekdayAndHoliday = false,
            StartBase = location,
            EndBase = location,
            TransportMode = request.TransportMode,
            ContainerTemplateItems = containerTemplateItems
        };

        await _mediator.Send(
            new PostContainerTemplatesCommand(containerId, new List<ContainerTemplateResource> { templateResource }),
            cancellationToken);

        await ConfirmPersistedAsync(
            SkillName,
            () => _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken),
            templates => IsTemplateFullyPersisted(templates, storageWeekday, selectedShiftIds),
            $"the container template for weekday {isoWeekday}");
    }

    private async Task<List<ContainerTemplateItemResource>> BuildTemplateItemsAsync(List<Guid> selectedShiftIds)
    {
        var containerTemplateItems = new List<ContainerTemplateItemResource>();
        foreach (var shiftId in selectedShiftIds)
        {
            var taskShift = await _shiftRepository.Get(shiftId);
            if (taskShift is null)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Selected task shift {shiftId} could not be re-read while building the container template.");
            }

            containerTemplateItems.Add(new ContainerTemplateItemResource
            {
                Id = Guid.Empty,
                ShiftId = taskShift.Id,
                AbsenceId = null,
                StartItem = taskShift.StartShift,
                EndItem = taskShift.EndShift,
                BriefingTime = taskShift.BriefingTime,
                DebriefingTime = taskShift.DebriefingTime,
                TravelTimeAfter = taskShift.TravelTimeAfter,
                TravelTimeBefore = taskShift.TravelTimeBefore,
                TimeRangeStartItem = null,
                TimeRangeEndItem = null,
                TransportMode = Klacks.Api.Domain.Enums.TransportMode.ByCar
            });
        }

        return containerTemplateItems;
    }

    /// <summary>
    /// Releases the container lock on every exit path and swallows a failing release, so it can never mask the
    /// operation's real outcome.
    /// </summary>
    /// <param name="lockId">Id of the lock acquired before the template write</param>
    private async Task ReleaseLockQuietlyAsync(Guid lockId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new ReleaseContainerLockCommand(lockId), cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    private static SkillResult BuildSuccessResult(BundleRequest request, BundleOutcome outcome)
    {
        var (isoWeekday, transportModeStr, location) = (request.IsoWeekday, request.TransportModeText, request.Location);

        var autofill = outcome.AutofillResult;
        var resultData = new
        {
            ContainerId = outcome.ContainerId,
            ContainerName = outcome.ContainerName,
            Weekday = isoWeekday,
            TransportMode = transportModeStr,
            CandidatesFound = autofill.TotalAvailableShifts,
            ServicesBundled = autofill.SelectedShiftCount,
            TotalDistanceKm = autofill.TotalDistanceKm,
            EstimatedTravelAndWorkTime = autofill.EstimatedTravelTime.ToString(@"hh\:mm"),
            RemainingBudget = autofill.RemainingTime.ToString(@"hh\:mm"),
            BundledShiftIds = autofill.SelectedShiftIds,
            Verified = true
        };

        var message =
            $"Container '{outcome.ContainerName}' created for weekday {isoWeekday} (transportMode={transportModeStr}) and its " +
            $"template was filled with {autofill.SelectedShiftCount} of {autofill.TotalAvailableShifts} nearby TimeRange " +
            $"service(s) around '{location}', confirmed in the database (verified). Estimated route: {autofill.TotalDistanceKm} km, " +
            $"{autofill.EstimatedTravelTime:hh\\:mm} travel+work time, {autofill.RemainingTime:hh\\:mm} of the time budget " +
            "remaining. Candidates were not limited by a hard distance radius — only by the time budget and route " +
            "optimization. To fill another weekday, call this skill again with a different weekday.";

        return SkillResult.SuccessResult(resultData, message);
    }

    /// <summary>
    /// The autofill service is a domain service without a caller, so its picks are narrowed to the task shifts the caller
    /// may see before anything is written; an unrestricted caller keeps every pick.
    /// </summary>
    /// <param name="autofillResult">Raw autofill result</param>
    private async Task<ContainerAutofillResult> KeepVisibleSelectionAsync(
        ContainerAutofillResult autofillResult, CancellationToken cancellationToken)
    {
        if (autofillResult.SelectedShiftIds.Count == 0)
        {
            return autofillResult;
        }

        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(autofillResult.SelectedShiftIds, cancellationToken);
        if (visibleIds.Count == autofillResult.SelectedShiftIds.Distinct().Count())
        {
            return autofillResult;
        }

        var visibleSelection = autofillResult.SelectedShiftIds.Where(visibleIds.Contains).ToList();
        return autofillResult with { SelectedShiftIds = visibleSelection, SelectedShiftCount = visibleSelection.Count };
    }

    private static bool IsTemplateFullyPersisted(
        List<ContainerTemplateResource> templates, int storageWeekday, IReadOnlyCollection<Guid> selectedShiftIds)
    {
        var template = templates.FirstOrDefault(t => t.Weekday == storageWeekday && !t.IsHoliday)
            ?? templates.FirstOrDefault(t => t.Weekday == storageWeekday);

        return template != null
            && selectedShiftIds.All(id => template.ContainerTemplateItems.Any(i => i.ShiftId == id));
    }

    private static (ContainerTransportMode? mode, string? error) ParseTransportMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "car" => (ContainerTransportMode.ByCar, null),
            "bicycle" => (ContainerTransportMode.ByBicycle, null),
            "foot" => (ContainerTransportMode.ByFoot, null),
            "mix" => (ContainerTransportMode.Mix, null),
            _ => (null, $"Invalid transportMode '{value}'. Expected one of: car, bicycle, foot, mix.")
        };
    }

    private static (bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun) BuildIsoWeekdayFlags(int isoWeekday)
    {
        return isoWeekday switch
        {
            1 => (true, false, false, false, false, false, false),
            2 => (false, true, false, false, false, false, false),
            3 => (false, false, true, false, false, false, false),
            4 => (false, false, false, true, false, false, false),
            5 => (false, false, false, false, true, false, false),
            6 => (false, false, false, false, false, true, false),
            7 => (false, false, false, false, false, false, true),
            _ => (false, false, false, false, false, false, false)
        };
    }
}
