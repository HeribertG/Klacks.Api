// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates the weekday template of a container shift (ShiftType.IsContainer) — the entry that
/// add_container_template_task requires and refuses to create itself. The template is written over the
/// own REST API: the container edit lock is acquired first, the template is posted, and the lock is
/// released again in a finally block. The lock is acquired with an EMPTY instance id because
/// ContainerLocksController reads it from the request body while PostContainerTemplatesCommandHandler
/// checks it against the X-Instance-Id header, which KlacksSelfApiClient does not send — an empty
/// value is the only one under which both ends agree on a self-call.
/// </summary>
/// <param name="shiftVisibilityGuard">Answers a hidden container (or task shift) exactly like a missing one</param>
/// <param name="containerId">Required. UUID of the container's plannable shift (Status OriginalShift, ShiftType IsContainer).</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday the template applies to.</param>
/// <param name="fromTime">Required. Start of the day's time budget (HH:mm).</param>
/// <param name="untilTime">Required. End of the day's time budget (HH:mm); must be after fromTime.</param>
/// <param name="isHoliday">Optional. Holiday template instead of the weekday template: alone it applies on every public holiday, with isWeekdayAndHoliday only on a public holiday falling on this weekday; defaults to false.</param>
/// <param name="isWeekdayAndHoliday">Optional, only together with isHoliday. Restricts the holiday template to public holidays on this weekday (ticked days also run on holidays, unticked ones do not); defaults to false.</param>
/// <param name="startBase">Optional. Start location the container's tasks are routed from.</param>
/// <param name="endBase">Optional. End location of that route; falls back to startBase when omitted.</param>
/// <param name="transportMode">Optional: car|bicycle|foot|mix; defaults to car.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_container_template")]
public class CreateContainerTemplateSkill : BaseSkillImplementation
{
    private const string SkillName = "create_container_template";
    private const string SelfCallInstanceId = "";
    private const string TemplatesRouteSegment = "templates";
    private const string LockAcquireRouteSegment = "Acquire";
    private const string TransportModeCar = "car";
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;

    private readonly IShiftRepository _shiftRepository;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public CreateContainerTemplateSkill(
        IShiftRepository shiftRepository,
        IContainerTemplateRepository containerTemplateRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _selfApi = selfApi;
        _routes = routes;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    private sealed record TemplateRequest(
        Guid ContainerId,
        int IsoWeekday,
        int StorageWeekday,
        TimeOnly FromTime,
        TimeOnly UntilTime,
        bool IsHoliday,
        bool IsWeekdayAndHoliday,
        string? StartBase,
        string? EndBase,
        string TransportModeText,
        ContainerTransportMode TransportMode);

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

        var (container, containerError) = await LoadPlannableContainerAsync(request.ContainerId, cancellationToken);
        if (container is null)
        {
            return SkillResult.Error(containerError!);
        }

        var duplicateError = await FindDuplicateTemplateAsync(request, container);
        if (duplicateError != null)
        {
            return SkillResult.Error(duplicateError);
        }

        var containersRoute = _routes.Resolve(typeof(ContainerTemplateResource));
        var templatesRoute = $"{containersRoute}/{request.ContainerId}/{TemplatesRouteSegment}";

        var (acquiredLock, lockError) = await AcquireLockAsync(request.ContainerId, container, context, cancellationToken);
        if (acquiredLock is null)
        {
            return SkillResult.Error(lockError!);
        }

        try
        {
            return await PostTemplateAsync(request, container, templatesRoute, context, cancellationToken);
        }
        finally
        {
            await ReleaseLockAsync(acquiredLock.Id, context);
        }
    }

    private static (TemplateRequest? Request, string? Error) ParseRequest(Dictionary<string, object> parameters, string? language)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var fromTimeStr = GetRequiredString(parameters, "fromTime");
        var untilTimeStr = GetRequiredString(parameters, "untilTime");
        var isHoliday = GetParameter<bool?>(parameters, "isHoliday") ?? false;
        var isWeekdayAndHoliday = GetParameter<bool?>(parameters, "isWeekdayAndHoliday") ?? false;
        var startBase = GetParameter<string>(parameters, "startBase");
        var endBase = GetParameter<string>(parameters, "endBase");
        var transportModeStr = GetParameter<string>(parameters, "transportMode") ?? TransportModeCar;

        if (isoWeekday < MinIsoWeekday || isoWeekday > MaxIsoWeekday)
        {
            return (null, "weekday must be between 1 (Monday) and 7 (Sunday).");
        }

        if (!SkillCalendarStringParser.TryParseTimeOnly(fromTimeStr, language, out var fromTime))
        {
            return (null, $"Invalid fromTime '{fromTimeStr}'. Expected format 'HH:mm'.");
        }

        if (!SkillCalendarStringParser.TryParseTimeOnly(untilTimeStr, language, out var untilTime))
        {
            return (null, $"Invalid untilTime '{untilTimeStr}'. Expected format 'HH:mm'.");
        }

        if (untilTime <= fromTime)
        {
            return (null, $"untilTime ({untilTime:HH\\:mm}) must be after fromTime ({fromTime:HH\\:mm}).");
        }

        var (transportMode, transportModeError) = ParseTransportMode(transportModeStr);
        if (transportMode is null)
        {
            return (null, transportModeError!);
        }

        var storageWeekday = isoWeekday == IsoSunday ? StorageSunday : isoWeekday;

        return (new TemplateRequest(
            containerId, isoWeekday, storageWeekday, fromTime, untilTime, isHoliday, isWeekdayAndHoliday,
            startBase, endBase, transportModeStr, transportMode.Value), null);
    }

    private async Task<(Shift? Container, string? Error)> LoadPlannableContainerAsync(
        Guid containerId, CancellationToken cancellationToken)
    {
        var container = await _shiftRepository.Get(containerId);
        if (container is null || !await _shiftVisibilityGuard.IsVisibleAsync(containerId, cancellationToken))
        {
            return (null, $"Shift/container not found: {containerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return (null, $"Shift {containerId} is not a container shift (ShiftType must be IsContainer).");
        }

        if (container.Status != ShiftStatus.OriginalShift)
        {
            return (null,
                $"Container '{container.Name}' has status {container.Status}; a template belongs to the plannable " +
                "container shift (status OriginalShift). Pass the plannable shift's id, not the order's — " +
                "get_shift_details or search_shifts with status=container return it.");
        }

        return (container, null);
    }

    private async Task<string?> FindDuplicateTemplateAsync(TemplateRequest request, Shift container)
    {
        var (isoWeekday, storageWeekday) = (request.IsoWeekday, request.StorageWeekday);
        var (isHoliday, isWeekdayAndHoliday) = (request.IsHoliday, request.IsWeekdayAndHoliday);

        var existingTemplates = await _containerTemplateRepository.GetTemplatesForContainer(request.ContainerId);
        if (existingTemplates.Any(t =>
                t.Weekday == storageWeekday
                && t.IsHoliday == isHoliday
                && t.IsWeekdayAndHoliday == isWeekdayAndHoliday))
        {
            return
                $"Container '{container.Name}' already has a template for weekday {isoWeekday} " +
                $"(isHoliday={isHoliday}, isWeekdayAndHoliday={isWeekdayAndHoliday}). " +
                "Use list_container_template to inspect it and add_container_template_task to fill it.";
        }

        return null;
    }

    private async Task<(ContainerLockResource? Lock, string? Error)> AcquireLockAsync(
        Guid containerId, Shift container, SkillExecutionContext context, CancellationToken cancellationToken)
    {
        var lockResult = await _selfApi.PostAsync<ContainerLockResource>(
            $"{SelfApiRoutes.ContainerLocks}/{LockAcquireRouteSegment}",
            new AcquireContainerLockRequest
            {
                ResourceType = ContainerLockResourceTypes.ContainerTemplate,
                ResourceId = containerId,
                InstanceId = SelfCallInstanceId
            },
            context,
            SkillName,
            cancellationToken);

        if (!lockResult.Success)
        {
            return (null, lockResult.ErrorMessage!);
        }

        var acquiredLock = lockResult.Value;
        if (acquiredLock is null)
        {
            return (null, "The container edit lock could not be acquired — nothing was written.");
        }

        if (!acquiredLock.Acquired)
        {
            // A self-conflict is the normal case here rather than an edge case: the browser holds the lock
            // under its X-Instance-Id, and the self-call cannot present that id, so the caller's own open
            // container page looks like a foreign session. Saying "another user" would be untrue.
            return (null, acquiredLock.IsSelfConflict
                ? $"You have container '{container.Name}' open for editing yourself, and an assistant-driven " +
                  "write cannot share that editing session. Close the container page and try again, or create " +
                  "the weekday template directly on the page you already have open."
                : $"Container '{container.Name}' is currently being edited by {acquiredLock.UserName}.");
        }

        return (acquiredLock, null);
    }

    private async Task<SkillResult> PostTemplateAsync(
        TemplateRequest request,
        Shift container,
        string templatesRoute,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var (isoWeekday, storageWeekday) = (request.IsoWeekday, request.StorageWeekday);
        var (isHoliday, isWeekdayAndHoliday) = (request.IsHoliday, request.IsWeekdayAndHoliday);

        var templateResource = BuildEmptyTemplate(request);

        var postResult = await _selfApi.PostAsync<List<ContainerTemplateResource>>(
            templatesRoute,
            new List<ContainerTemplateResource> { templateResource },
            context,
            SkillName,
            cancellationToken);

        if (!postResult.Success)
        {
            return SkillResult.Error(postResult.ErrorMessage!);
        }

        var created = postResult.Value?.FirstOrDefault(t =>
            t.Weekday == storageWeekday
            && t.IsHoliday == isHoliday
            && t.IsWeekdayAndHoliday == isWeekdayAndHoliday);

        if (created is null || created.Id == Guid.Empty)
        {
            return SkillResult.Error(
                $"The template for weekday {isoWeekday} was posted but the API returned no persisted template, " +
                "so the write could not be confirmed.");
        }

        return CreatedTemplateResult(request, container, created.Id, templateResource);
    }

    private static ContainerTemplateResource BuildEmptyTemplate(TemplateRequest request)
    {
        return new ContainerTemplateResource
        {
            Id = Guid.Empty,
            ContainerId = request.ContainerId,
            FromTime = request.FromTime,
            UntilTime = request.UntilTime,
            Weekday = request.StorageWeekday,
            IsHoliday = request.IsHoliday,
            IsWeekdayAndHoliday = request.IsWeekdayAndHoliday,
            StartBase = request.StartBase,
            EndBase = request.EndBase ?? request.StartBase,
            TransportMode = request.TransportMode,
            ContainerTemplateItems = new List<ContainerTemplateItemResource>()
        };
    }

    private static SkillResult CreatedTemplateResult(
        TemplateRequest request, Shift container, Guid createdTemplateId, ContainerTemplateResource templateResource)
    {
        var (containerId, isoWeekday, fromTime, untilTime) =
            (request.ContainerId, request.IsoWeekday, request.FromTime, request.UntilTime);

        var resultData = new
        {
            ContainerTemplateId = createdTemplateId,
            ContainerId = containerId,
            ContainerName = container.Name,
            Weekday = isoWeekday,
            FromTime = fromTime.ToString("HH\\:mm"),
            UntilTime = untilTime.ToString("HH\\:mm"),
            IsHoliday = request.IsHoliday,
            IsWeekdayAndHoliday = request.IsWeekdayAndHoliday,
            StartBase = templateResource.StartBase,
            EndBase = templateResource.EndBase,
            TransportMode = request.TransportModeText,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Weekday template for weekday {isoWeekday} created on container '{container.Name}' " +
            $"({fromTime:HH\\:mm}-{untilTime:HH\\:mm}). It is still empty — add task shifts to it with " +
            $"add_container_template_task using containerId=\"{containerId}\" and weekday={isoWeekday}.");
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

    /// <summary>
    /// Releases the lock on every exit path. It deliberately ignores the caller's cancellation token and
    /// swallows failures: a release that throws out of the finally block would mask the skill's real
    /// result and still leave the container locked until the 90-second staleness sweep.
    /// </summary>
    /// <param name="lockId">Id of the lock acquired at the start of the write</param>
    /// <param name="context">Caller context carrying the bearer token the release is sent with</param>
    private async Task ReleaseLockAsync(Guid lockId, SkillExecutionContext context)
    {
        try
        {
            await _selfApi.DeleteAsync<bool>(
                $"{SelfApiRoutes.ContainerLocks}/{lockId}",
                context,
                SkillName,
                CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }
}
