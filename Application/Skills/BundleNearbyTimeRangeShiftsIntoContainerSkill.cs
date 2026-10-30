// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Self-verifying skill that bundles all nearby mobile TimeRange services around a location into ONE
/// new container shift: it creates a sealed container order (ShiftType.IsContainer), runs
/// IContainerAutofillService to select and route-optimize the matching TimeRange shifts for exactly one
/// weekday, and persists the resulting weekday template — all inside a single database transaction that
/// rolls back completely (no orphaned empty container) when nothing matches or the write cannot be
/// confirmed. For other weekdays, call this skill again with a different weekday and the same location.
/// The template is written the way the Ui container page saves an autofill result, without a second optimization:
/// the autofill route becomes route steps, ContainerVisitPlanner places the visit windows and travel times before and after
/// in route order, and the template stores the RouteInfo. ContainerTemplateWriter writes it and the verification requires
/// the RouteInfo and every written visit and travel time. When the visibility guard drops picks, the route is narrowed to
/// the visible picks first (ContainerAutofillVisibilityFilter), so nothing of a hidden task reaches the template, the stops
/// or the map. Container order, input parsing, transport mode check, planning, writing and the estimation note are shared
/// with build_weekly_tours (ContainerShiftFactory, ContainerSkillInputParser, ContainerTransportModeResolver,
/// ContainerVisitPlanner, ContainerTemplateWriter, ContainerTravelTimeNotes).
/// Data carries the stops in route order and a top-level Route to the container page (ContainerTemplateRoutes).
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
/// <param name="groupName">Optional group name restricting which candidate services are considered, resolved by
/// BundleGroupFilterResolver (exact name wins, ambiguous or unknown names are refused with the candidates).</param>
/// <param name="groupIds">Optional comma-separated group UUIDs, combined with groupName; without any group the container
/// has no group filter (all matching tasks are eligible), and the result says so.</param>
/// <param name="name">Optional container name; defaults to "Mobile Dienste {location}".</param>
/// <param name="groupVisibilityGuard">Answers a group a restricted caller may not see exactly like a missing group</param>
/// <param name="groupItemRepository">Tells the group filter whether a requested group has any linked service</param>
/// <param name="shiftVisibilityGuard">Drops autofill picks a restricted caller may not see, so a hidden task shift is
/// never written into the template, reported back or named in an error</param>
/// <param name="defaultShiftMacroResolver">Supplies the default Shift-category macro of the new container, exactly as a
/// container created in the shift editor receives it</param>
/// <param name="scheduleMapper">Maps the built RouteInfo to the template resource</param>

using System.Globalization;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.GeoServices;
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
    private const double DefaultTimeRangeTolerance = 0.5;
    private const string GroupFilterNote = " Only services of the group '{0}' were considered.";
    private const string NoGroupFilterNote = " No group filter was applied: services of all groups were considered.";
    private const string GroupNameSeparator = "', '";
    private const string NoMatchingServicesMessage =
        "No matching mobile TimeRange services were found around '{0}' within the given time budget — no container was created.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly IContainerAutofillService _containerAutofillService;
    private readonly BundleGroupFilterResolver _groupFilterResolver;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;
    private readonly ContainerTransportModeResolver _transportModes;
    private readonly ContainerAutofillVisibilityFilter _visibilityFilter;
    private readonly ContainerTemplateWriter _templateWriter;

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
        IGroupItemRepository groupItemRepository,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IDefaultShiftMacroResolver defaultShiftMacroResolver,
        ScheduleMapper scheduleMapper)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _userService = userService;
        _containerAutofillService = containerAutofillService;
        _groupFilterResolver = new BundleGroupFilterResolver(groupRepository, groupVisibilityGuard, groupItemRepository);
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
        _transportModes = new ContainerTransportModeResolver(geoServicesPolicy, settingsReader, encryptionService);
        _visibilityFilter = new ContainerAutofillVisibilityFilter(shiftVisibilityGuard);
        _templateWriter = new ContainerTemplateWriter(mediator, scheduleMapper, shiftRepository);
    }

    internal sealed record BundleOutcome(
        Guid ContainerId, string ContainerName, ContainerAutofillResult AutofillResult, List<ContainerRouteStop> Stops);

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
        string? GroupName,
        string Name);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (request, requestError) = ParseRequest(parameters, context.UserLanguage);
        if (request is null)
        {
            return SkillResult.Error(requestError!);
        }

        var unavailableTransportMode = await _transportModes.CheckAvailableAsync(request.TransportMode, request.TransportModeText);
        if (unavailableTransportMode != null)
        {
            return SkillResult.Error(unavailableTransportMode);
        }

        var (groups, groupError) = await _groupFilterResolver.ResolveAsync(
            request.GroupIdsRaw, request.GroupName, cancellationToken);
        if (groups is null)
        {
            return SkillResult.Error(groupError!);
        }

        var defaultMacroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);
        var spec = BuildContainerSpec(request, BuildGroupItems(groups, request.FromDate), defaultMacroId, context.UserName);

        var instanceId = _userService.GetInstanceId() ?? string.Empty;

        BundleOutcome outcome;
        try
        {
            outcome = await _unitOfWork.ExecuteInTransactionAsync(
                () => CreateAndFillContainerAsync(request, spec, instanceId, cancellationToken));
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return BuildSuccessResult(request, outcome, groups.Select(g => g.Name).ToList());
    }

    private static (BundleRequest? Request, string? Error) ParseRequest(Dictionary<string, object> parameters, string? language)
    {
        var location = GetRequiredString(parameters, "location");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var fromTimeStr = GetRequiredString(parameters, "fromTime");
        var untilTimeStr = GetRequiredString(parameters, "untilTime");
        var fromDateStr = GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var transportModeStr = GetParameter<string>(parameters, "transportMode") ?? ContainerTransportModeResolver.Car;
        var timeRangeTolerance = GetParameter<double>(parameters, "timeRangeTolerance", DefaultTimeRangeTolerance);
        var groupIdsRaw = GetParameter<string>(parameters, "groupIds");
        var groupName = GetParameter<string>(parameters, "groupName");
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Mobile Dienste {location}";
        }

        if (!IsoWeekday.IsValid(isoWeekday))
        {
            return (null, IsoWeekday.RangeError);
        }

        var (fromTime, untilTime, timeError) = ContainerSkillInputParser.ParseTimeBudget(fromTimeStr, untilTimeStr, language);
        if (timeError != null)
        {
            return (null, timeError);
        }

        var (fromDate, untilDate, dateError) = ContainerSkillInputParser.ParseValidity(fromDateStr, untilDateStr, language);
        if (dateError != null)
        {
            return (null, dateError);
        }

        var (transportMode, transportModeError) = ContainerTransportModeResolver.Parse(transportModeStr);
        if (transportMode is null)
        {
            return (null, transportModeError!);
        }

        var storageWeekday = IsoWeekday.ToStorage(isoWeekday);

        return (new BundleRequest(
            location, isoWeekday, storageWeekday, fromTime, untilTime, fromDate, untilDate,
            transportModeStr, transportMode.Value, timeRangeTolerance, groupIdsRaw, groupName, name), null);
    }

    private static List<GroupItem> BuildGroupItems(List<Group> groups, DateOnly fromDate) =>
        groups.Select(group => new GroupItem
        {
            GroupId = group.Id,
            ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        }).ToList();

    private static ContainerShiftSpec BuildContainerSpec(
        BundleRequest request, List<GroupItem> groupItems, Guid? defaultMacroId, string userName) => new(
            request.Name,
            GenerateAbbreviation(request.Name),
            request.FromTime,
            request.UntilTime,
            request.FromDate,
            request.UntilDate,
            CalculateWorkTime(request.FromTime, request.UntilTime),
            defaultMacroId,
            new HashSet<int> { request.IsoWeekday },
            groupItems,
            userName);

    /// <summary>
    /// Runs inside the transaction: persists the container, autofills it, and writes and confirms its weekday
    /// template while holding the container lock. Any SkillVerificationException rolls the whole operation back.
    /// </summary>
    /// <param name="request">Validated skill input</param>
    /// <param name="spec">The sealed container order to build and persist</param>
    /// <param name="instanceId">Instance id the container lock is acquired under</param>
    private async Task<BundleOutcome> CreateAndFillContainerAsync(
        BundleRequest request, ContainerShiftSpec spec, string instanceId, CancellationToken cancellationToken)
    {
        var resultShift = await _shiftRepository.AddWithSealedOrderHandling(ContainerShiftFactory.Build(spec));
        await _unitOfWork.CompleteAsync();

        var containerId = resultShift.Id;

        var autofillResult = await AutofillVisibleSelectionAsync(containerId, request, cancellationToken);
        if (autofillResult.SelectedShiftIds.Count == 0)
        {
            throw new SkillVerificationException(SkillName, string.Format(CultureInfo.InvariantCulture, NoMatchingServicesMessage, request.Location));
        }

        await using var lockScope = await ContainerTemplateLockScope.AcquireAsync(
            _mediator, containerId, instanceId, cancellationToken);

        if (!lockScope.Acquired)
        {
            throw new SkillVerificationException(
                SkillName,
                "The newly created container could not be locked for writing its template — the operation was rolled back.");
        }

        var stops = await WriteAndConfirmTemplateAsync(containerId, request, autofillResult, cancellationToken);

        return new BundleOutcome(containerId, resultShift.Name, autofillResult, stops);
    }

    private async Task<ContainerAutofillResult> AutofillVisibleSelectionAsync(
        Guid containerId, BundleRequest request, CancellationToken cancellationToken)
    {
        var rawAutofillResult = await _containerAutofillService.AutofillAsync(new ContainerAutofillRequest(
            ContainerId: containerId,
            Weekday: request.StorageWeekday,
            IsHoliday: false,
            StartBase: request.Location,
            EndBase: request.Location,
            FromTime: request.FromTime,
            UntilTime: request.UntilTime,
            TransportMode: request.TransportMode,
            TimeRangeTolerance: request.TimeRangeTolerance,
            CancellationToken: cancellationToken));

        return await _visibilityFilter.KeepVisibleSelectionAsync(rawAutofillResult, cancellationToken);
    }

    private async Task<List<ContainerRouteStop>> WriteAndConfirmTemplateAsync(
        Guid containerId, BundleRequest request, ContainerAutofillResult autofillResult, CancellationToken cancellationToken)
    {
        var taskShifts = await _templateWriter.LoadTaskShiftsAsync(SkillName, autofillResult.SelectedShiftIds);
        var planned = ContainerVisitPlanner.Plan(SkillName, request.Location, request.FromTime, autofillResult, taskShifts);

        var template = _templateWriter.BuildTemplate(
            containerId, request.StorageWeekday, request.FromTime, request.UntilTime, request.Location, request.TransportMode, planned);

        await _templateWriter.WriteAndConfirmAsync(SkillName, containerId, [template], cancellationToken);

        return planned.Stops;
    }

    private static SkillResult BuildSuccessResult(BundleRequest request, BundleOutcome outcome, List<string> groupNames)
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
            Stops = outcome.Stops,
            TravelTimesEstimated = autofill.IsEstimated,
            GroupFilter = groupNames,
            Route = ContainerTemplateRoutes.ForContainer(outcome.ContainerId),
            Verified = true
        };

        var message =
            $"Container '{outcome.ContainerName}' created for weekday {isoWeekday} (transportMode={transportModeStr}). Its " +
            $"template holds {autofill.SelectedShiftCount} of {autofill.TotalAvailableShifts} nearby TimeRange " +
            $"service(s) around '{location}' in route order, each with a planned visit time and the travel time before " +
            "it, the last one also with the travel time back to the base, plus the route info; all of it is confirmed " +
            "in the database (verified). " +
            $"Route: {autofill.TotalDistanceKm} km, {autofill.EstimatedTravelTime:hh\\:mm} travel+work time, " +
            $"{autofill.RemainingTime:hh\\:mm} of the time budget remaining." +
            BuildGroupNote(groupNames) +
            ContainerTravelTimeNotes.Estimation(autofill.IsEstimated, autofill.TotalAvailableShifts, groupNames.Count > 0) +
            " Candidates were not limited by a hard distance radius — only by the time budget and route " +
            "optimization. The chat opens the container page, where the user can check the tour on the map and change " +
            "it. To fill another weekday, call this skill again with a different weekday.";

        return SkillResult.SuccessResult(resultData, message);
    }

    private static string BuildGroupNote(List<string> groupNames) =>
        groupNames.Count == 0
            ? NoGroupFilterNote
            : string.Format(CultureInfo.InvariantCulture, GroupFilterNote, string.Join(GroupNameSeparator, groupNames));
}
