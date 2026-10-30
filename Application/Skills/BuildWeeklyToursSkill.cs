// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Self-verifying skill that turns the unused TimeRange services of a group into a fixed number of weekly tours (containers) in
/// one call: each customer is assigned to exactly one tour by a deterministic geographic clustering (GeographicClusterer, via
/// WeeklyTourPlanner), so every weekday service of a customer lands in the same tour. It creates one sealed container per tour
/// (ContainerShiftFactory, named "{namePrefix} {number}", active only on the weekdays that have services, linked to the group)
/// and, for every weekday, writes the template that the container autofill builds restricted to the services of that tour on
/// that day (WeeklyTourBuilder, ContainerAutofillRequest.RestrictToShiftIds). The whole run is ONE database transaction:
/// WeeklyTourPlacementVerifier re-reads the templates, and a tour that places nothing, a template that cannot be confirmed or
/// a service in the wrong tour rolls everything back. A service that does not fit a day's time budget is reported per tour and
/// weekday, never dropped; customers without a locatable address are reported as unplanned. Services that still run on several
/// weekdays refuse the run (they must be cut by weekday first). The result carries per-tour and per-weekday visit counts,
/// distance and stops, Verified, and a Route to the first container page. Container order, input parsing, the transport mode
/// check, route planning, template writing and the estimation note are the ones bundle_nearby_timerange_shifts_into_container
/// uses.
/// </summary>
/// <param name="location">City/place/address used as start and end base of every tour.</param>
/// <param name="tourCount">Number of tours, at least 2; not more than the customers that can be located.</param>
/// <param name="groupName">Name of the group whose services are planned, resolved by BundleGroupFilterResolver.</param>
/// <param name="fromTime">Optional start of the day's time budget (HH:mm); the earliest start of the services when omitted.</param>
/// <param name="untilTime">Optional end of the day's time budget (HH:mm); the latest end of the services when omitted.</param>
/// <param name="fromDate">Optional first day of validity (YYYY-MM-DD); the earliest first day of the services when omitted.</param>
/// <param name="untilDate">Optional last day of validity (YYYY-MM-DD); open-ended when omitted; needs fromDate.</param>
/// <param name="transportMode">Optional: car|bicycle|foot|mix; defaults to car.</param>
/// <param name="timeRangeTolerance">Optional strictness of the TimeRange windows (1.0 strict, 0 disables); defaults to 0.5.</param>
/// <param name="namePrefix">Optional prefix of the tour names; defaults to "Tour".</param>
/// <param name="groupVisibilityGuard">Answers a group a restricted caller may not see exactly like a missing group</param>
/// <param name="groupItemRepository">Lists the services linked directly to the group</param>
/// <param name="defaultShiftMacroResolver">Supplies the default Shift-category macro of the new containers</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class BuildWeeklyToursSkill : BaseSkillImplementation
{
    public const string SkillName = "build_weekly_tours";
    public const string DefaultNamePrefix = "Tour";

    private const double DefaultTimeRangeTolerance = 0.5;
    private const string ListSeparator = "; ";

    private readonly BundleGroupFilterResolver _groupFilterResolver;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly ContainerTransportModeResolver _transportModes;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserService _userService;
    private readonly WeeklyTourPlanner _planner;
    private readonly WeeklyTourBuilder _builder;
    private readonly WeeklyTourPlacementVerifier _verifier;

    public BuildWeeklyToursSkill(
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupItemRepository groupItemRepository,
        IGeoServicesPolicy geoServicesPolicy,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        IDefaultShiftMacroResolver defaultShiftMacroResolver,
        IUnitOfWork unitOfWork,
        IUserService userService,
        WeeklyTourPlanner planner,
        WeeklyTourBuilder builder,
        WeeklyTourPlacementVerifier verifier)
    {
        _groupFilterResolver = new BundleGroupFilterResolver(groupRepository, groupVisibilityGuard, groupItemRepository);
        _groupItemRepository = groupItemRepository;
        _transportModes = new ContainerTransportModeResolver(geoServicesPolicy, settingsReader, encryptionService);
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
        _unitOfWork = unitOfWork;
        _userService = userService;
        _planner = planner;
        _builder = builder;
        _verifier = verifier;
    }

    internal sealed record ToursOutcome(IReadOnlyList<WeeklyTourOutcome> Tours);

    private sealed record TourRequest(
        string Location,
        int TourCount,
        string GroupName,
        TimeOnly? FromTime,
        TimeOnly? UntilTime,
        DateOnly? FromDate,
        DateOnly? UntilDate,
        string TransportModeText,
        ContainerTransportMode TransportMode,
        double TimeRangeTolerance,
        string NamePrefix);

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

        var (groups, groupError) = await _groupFilterResolver.ResolveAsync(null, request.GroupName, cancellationToken);
        if (groups is not { Count: 1 })
        {
            return SkillResult.Error(groupError ?? $"The group '{request.GroupName}' could not be resolved.");
        }

        var groupIds = groups.Select(group => group.Id).ToList();
        var groupShiftIds = await _groupItemRepository.GetShiftIdsByGroupIds(groupIds, cancellationToken);

        var (plan, planError) = await _planner.PlanAsync(
            new WeeklyTourRequest(
                groupIds, groupShiftIds, request.TourCount, request.FromTime, request.UntilTime, request.FromDate, request.UntilDate),
            cancellationToken);
        if (plan is null)
        {
            return SkillResult.Error(planError!);
        }

        var settings = new WeeklyTourBuildSettings(
            SkillName,
            request.Location,
            plan,
            request.TransportMode,
            request.TimeRangeTolerance,
            request.NamePrefix,
            groups,
            await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken),
            context.UserName,
            _userService.GetInstanceId() ?? string.Empty);

        ToursOutcome outcome;
        try
        {
            outcome = await _unitOfWork.ExecuteInTransactionAsync(() => BuildAllAsync(settings, cancellationToken));
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return WeeklyToursResult.Build(request.TransportModeText, request.Location, plan, outcome.Tours);
    }

    private async Task<ToursOutcome> BuildAllAsync(WeeklyTourBuildSettings settings, CancellationToken cancellationToken)
    {
        var tours = new List<WeeklyTourOutcome>();
        foreach (var tour in settings.Plan.Tours)
        {
            tours.Add(await _builder.BuildAsync(settings, tour, cancellationToken));
        }

        var mismatches = await _verifier.FindMismatchesAsync(settings.Plan, tours, cancellationToken);
        if (mismatches.Count > 0)
        {
            throw new SkillVerificationException(
                SkillName,
                "Database verification failed — the whole run was rolled back and no tour was created: " +
                string.Join(ListSeparator, mismatches));
        }

        return new ToursOutcome(tours);
    }

    private static (TourRequest? Request, string? Error) ParseRequest(Dictionary<string, object> parameters, string? language)
    {
        var location = GetRequiredString(parameters, "location");
        var tourCount = GetRequiredInt(parameters, "tourCount");
        var groupName = GetRequiredString(parameters, "groupName");
        var transportModeText = GetParameter<string>(parameters, "transportMode") ?? ContainerTransportModeResolver.Car;
        var namePrefix = GetParameter<string>(parameters, "namePrefix");

        if (tourCount < WeeklyTourPlanner.MinTourCount)
        {
            return (null, $"tourCount must be at least {WeeklyTourPlanner.MinTourCount}.");
        }

        var (fromTime, untilTime, timeError) = ParseOptionalTimes(
            GetParameter<string>(parameters, "fromTime"), GetParameter<string>(parameters, "untilTime"), language);
        if (timeError != null)
        {
            return (null, timeError);
        }

        var (fromDate, untilDate, dateError) = ParseOptionalDates(
            GetParameter<string>(parameters, "fromDate"), GetParameter<string>(parameters, "untilDate"), language);
        if (dateError != null)
        {
            return (null, dateError);
        }

        var (transportMode, transportModeError) = ContainerTransportModeResolver.Parse(transportModeText);
        if (transportMode is null)
        {
            return (null, transportModeError!);
        }

        return (new TourRequest(
            location, tourCount, groupName, fromTime, untilTime, fromDate, untilDate, transportModeText, transportMode.Value,
            GetParameter<double>(parameters, "timeRangeTolerance", DefaultTimeRangeTolerance),
            string.IsNullOrWhiteSpace(namePrefix) ? DefaultNamePrefix : namePrefix.Trim()), null);
    }

    private static (TimeOnly? From, TimeOnly? Until, string? Error) ParseOptionalTimes(
        string? fromRaw, string? untilRaw, string? language)
    {
        if (string.IsNullOrWhiteSpace(fromRaw) && string.IsNullOrWhiteSpace(untilRaw))
        {
            return (null, null, null);
        }

        if (string.IsNullOrWhiteSpace(fromRaw) || string.IsNullOrWhiteSpace(untilRaw))
        {
            return (null, null, "fromTime and untilTime go together: pass both or neither.");
        }

        var (from, until, error) = ContainerSkillInputParser.ParseTimeBudget(fromRaw, untilRaw, language);
        return error is null ? (from, until, null) : (null, null, error);
    }

    private static (DateOnly? From, DateOnly? Until, string? Error) ParseOptionalDates(
        string? fromRaw, string? untilRaw, string? language)
    {
        if (string.IsNullOrWhiteSpace(fromRaw))
        {
            return string.IsNullOrWhiteSpace(untilRaw)
                ? (null, null, null)
                : (null, null, "untilDate needs fromDate: pass both or only fromDate.");
        }

        var (from, until, error) = ContainerSkillInputParser.ParseValidity(fromRaw, untilRaw, language);
        return error is null ? (from, until, null) : (null, null, error);
    }
}
