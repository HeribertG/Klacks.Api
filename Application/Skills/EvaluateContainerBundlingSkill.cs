// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises, before a container exists, whether the mobile TimeRange services around a location fit one tour on a weekday.
/// Runs the dry run of the real calculation (IContainerAutofillService.PreviewAsync) on the tasks a new container with these
/// groups and this validity would be offered, then places the visits with the planner of the bundling; only the tasks the
/// caller may see enter the calculation, so no count or name reveals a hidden one. Stores nothing, also no geocoded
/// coordinates. Read-only advice computed in C# (ContainerBundlingAdvisor); creating the container is a separate step.
/// </summary>
/// <param name="location">Required. City, place or address used as start and end base.</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday.</param>
/// <param name="fromTime">Required. Start of the day's time budget (HH:mm).</param>
/// <param name="untilTime">Required. End of the day's time budget (HH:mm); must be after fromTime.</param>
/// <param name="fromDate">Optional. First day the tour would run (YYYY-MM-DD); today in the company time zone when omitted.</param>
/// <param name="untilDate">Optional. Last day the tour would run (YYYY-MM-DD).</param>
/// <param name="transportMode">Optional. car|bicycle|foot|mix; defaults to car.</param>
/// <param name="timeRangeTolerance">Optional. Strictness of the service time windows, as in the bundling; defaults to 0.5.</param>
/// <param name="groupName">Optional. Group whose services are looked at.</param>
/// <param name="groupIds">Optional. Comma-separated group UUIDs, combined with groupName.</param>
/// <param name="breakMinutes">Optional. Length of the break to look for; defaults to the container break default.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;
using ContainerNames = Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill.ContainerAutofillDefaults;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateContainerBundlingSkill : ContainerBundleSkillBase
{
    public const string SkillName = "evaluate_container_bundling";

    private const string BreakMinutesParameter = "breakMinutes";

    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly IContainerAutofillService _containerAutofillService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly BundleGroupFilterResolver _groupFilterResolver;
    private readonly ICompanyClock _companyClock;

    public EvaluateContainerBundlingSkill(
        IContainerAvailableTasksService availableTasksService,
        IContainerAutofillService containerAutofillService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupItemRepository groupItemRepository,
        ICompanyClock companyClock,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        IGeoServicesPolicy geoServicesPolicy)
        : base(settingsReader, encryptionService, geoServicesPolicy)
    {
        _availableTasksService = availableTasksService;
        _containerAutofillService = containerAutofillService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _groupFilterResolver = new BundleGroupFilterResolver(groupRepository, groupVisibilityGuard, groupItemRepository);
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken));
        var (request, requestError) = ParseBundleRequest(parameters, context.UserLanguage, today);
        if (request is null)
        {
            return SkillResult.Error(requestError!);
        }

        var breakMinutes = GetParameter<int?>(parameters, BreakMinutesParameter) ?? ContainerVisitDefaults.DefaultMovableBlockMinutes;
        if (breakMinutes < ContainerVisitDefaults.MinBlockDurationMinutes || breakMinutes >= DayTimeConstants.MinutesPerDay)
        {
            return SkillResult.Error(
                $"{BreakMinutesParameter} must be between {ContainerVisitDefaults.MinBlockDurationMinutes} and {DayTimeConstants.MinutesPerDay - 1}.");
        }

        var unavailableTransportMode = await TransportModes.CheckAvailableAsync(request.TransportMode, request.TransportModeText);
        if (unavailableTransportMode != null)
        {
            return SkillResult.Error(unavailableTransportMode);
        }

        var (groups, groupError) = await _groupFilterResolver.ResolveAsync(request.GroupIdsRaw, request.GroupName, cancellationToken);
        if (groups is null)
        {
            return SkillResult.Error(groupError!);
        }

        var tasks = await LoadVisibleTasksAsync(request, groups.Select(group => group.Id).ToList(), cancellationToken);
        var preview = await _containerAutofillService.PreviewAsync(new ContainerAutofillPreviewRequest(
            tasks, request.Location, request.Location, request.FromTime, request.UntilTime, request.TransportMode,
            request.TimeRangeTolerance, ContainerBundlingAdviceLimits.MaxGeocodedAddresses, cancellationToken));

        var facts = BuildFacts(request, groups.Select(group => group.Name).ToList(), breakMinutes, tasks, preview);
        var evaluation = ContainerBundlingAdvisor.Evaluate(facts);

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }

    private async Task<List<Shift>> LoadVisibleTasksAsync(
        ContainerBundleRequest request, List<Guid> groupIds, CancellationToken cancellationToken)
    {
        var tasks = await _availableTasksService.GetAvailableTasksForNewContainerAsync(
            new NewContainerTaskScope(groupIds, request.FromDate, request.UntilDate),
            request.StorageWeekday,
            request.FromTime,
            request.UntilTime,
            cancellationToken);

        var visibleIds = await _shiftVisibilityGuard.FilterVisibleAsync(tasks.Select(task => task.Id).ToList(), cancellationToken);
        return tasks.Where(task => visibleIds.Contains(task.Id)).ToList();
    }

    private static ContainerBundlingFacts BuildFacts(
        ContainerBundleRequest request, List<string> groupNames, int breakMinutes, List<Shift> tasks, ContainerAutofillPreview preview)
    {
        var tasksById = tasks.ToDictionary(task => task.Id);
        var isRouted = preview.Outcome == ContainerAutofillPlanOutcome.Routed;
        (PlannedContainerVisits? visits, string? refusal) = isRouted
            ? ContainerVisitPlanner.TryPlan(
                request.Location, request.FromTime, preview.Result, preview.Result.SelectedShiftIds.Select(id => tasksById[id]).ToList())
            : (null, null);

        return new ContainerBundlingFacts(
            request.Location,
            request.IsoWeekday,
            request.FromTime,
            request.UntilTime,
            request.FromDate,
            request.UntilDate,
            request.TransportModeText,
            groupNames,
            breakMinutes,
            preview.Outcome,
            preview.TravelTimesEstimated,
            preview.TimeRangeTaskCount,
            isRouted ? BuildRoute(preview.Result, visits?.Stops ?? []) : null,
            refusal,
            BuildLeftOut(tasksById, preview));
    }

    private static ContainerBundlingRouteFacts BuildRoute(ContainerAutofillResult result, List<ContainerRouteStop> stops) => new(
        stops,
        result.SelectedShiftCount,
        WholeMinutes(result.EstimatedTravelTime - result.TotalWorkTime),
        WholeMinutes(result.TotalWorkTime),
        WholeMinutes(result.RemainingTime),
        result.TotalDistanceKm);

    private static int WholeMinutes(TimeSpan time) => (int)Math.Round(time.TotalMinutes, MidpointRounding.AwayFromZero);

    private static List<ContainerBundlingLeftOut> BuildLeftOut(Dictionary<Guid, Shift> tasksById, ContainerAutofillPreview preview)
    {
        var unlocated = preview.Unlocated.Select(task => (task.ShiftId, Reason: ToReason(task.Reason)));
        var omitted = preview.Omissions.Select(task => (task.ShiftId, Reason: ToReason(task.Reason)));

        return unlocated.Concat(omitted)
            .Select(entry => new ContainerBundlingLeftOut(
                entry.ShiftId,
                tasksById[entry.ShiftId].Client?.Name ?? ContainerNames.UnknownLocationName,
                entry.Reason))
            .ToList();
    }

    private static ContainerBundlingLeftOutReason ToReason(ContainerAutofillUnlocatedReason reason) => reason switch
    {
        ContainerAutofillUnlocatedReason.NoAddress => ContainerBundlingLeftOutReason.NoAddress,
        ContainerAutofillUnlocatedReason.SameAddress => ContainerBundlingLeftOutReason.SameAddress,
        ContainerAutofillUnlocatedReason.NotLookedUp => ContainerBundlingLeftOutReason.NotLookedUp,
        _ => ContainerBundlingLeftOutReason.NotLocated,
    };

    private static ContainerBundlingLeftOutReason ToReason(ContainerAutofillOmissionReason reason) => reason switch
    {
        ContainerAutofillOmissionReason.TooLongForTheDay => ContainerBundlingLeftOutReason.TooLongForTheDay,
        ContainerAutofillOmissionReason.TimeWindowConflict => ContainerBundlingLeftOutReason.TimeWindowConflict,
        _ => ContainerBundlingLeftOutReason.NoRoomOnTour,
    };
}
