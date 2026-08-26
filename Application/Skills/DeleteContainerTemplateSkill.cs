// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes the weekday templates of a container shift (ShiftType.IsContainer) — the inverse of
/// create_container_template. The REST endpoint behind it (DELETE Containers/{id}/templates) takes only
/// the container, so this skill removes EVERY weekday template of that container at once, not a single
/// weekday: there is no per-weekday delete route, and inventing one by rewriting the remaining set
/// through the PUT endpoint would silently rebuild rows the caller never asked to touch. That is why the
/// skill is classified Sensitive and asks a human at every autonomy level. As the registered inverse of
/// create_container_template it is exact only when the container held no template before the create —
/// the remediation case an empty container is in; with templates already present the delete removes
/// those too, and their items are left orphaned under a soft-deleted parent because the handler does not
/// cascade them. The container edit lock is acquired first and released again in a finally block; a
/// container that has no templates at all is reported as a no-op and never takes the lock.
/// </summary>
/// <param name="containerId">Required. UUID of the container's plannable shift (Status OriginalShift, ShiftType IsContainer).</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_container_template")]
public class DeleteContainerTemplateSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_container_template";
    private const string LockResourceType = "ContainerTemplate";
    private const string SelfCallInstanceId = "";
    private const string TemplatesRouteSegment = "templates";
    private const string LockAcquireRouteSegment = "Acquire";
    private const int StorageSunday = 0;
    private const int IsoSunday = 7;

    private readonly IShiftRepository _shiftRepository;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public DeleteContainerTemplateSkill(
        IShiftRepository shiftRepository,
        IContainerTemplateRepository containerTemplateRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _shiftRepository = shiftRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _selfApi = selfApi;
        _routes = routes;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");

        var container = await _shiftRepository.Get(containerId);
        if (container is null)
        {
            return SkillResult.Error($"Shift/container not found: {containerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {containerId} is not a container shift (ShiftType must be IsContainer).");
        }

        if (container.Status != ShiftStatus.OriginalShift)
        {
            return SkillResult.Error(
                $"Container '{container.Name}' has status {container.Status}; templates belong to the plannable " +
                "container shift (status OriginalShift). Pass the plannable shift's id, not the order's — " +
                "get_shift_details or search_shifts return it.");
        }

        var existingTemplates = await _containerTemplateRepository.GetTemplatesForContainer(containerId);
        if (existingTemplates.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ContainerId = containerId, ContainerName = container.Name, DeletedTemplateCount = 0 },
                $"Container '{container.Name}' has no weekday templates; nothing was deleted.");
        }

        var lockResult = await _selfApi.PostAsync<ContainerLockResource>(
            $"{SelfApiRoutes.ContainerLocks}/{LockAcquireRouteSegment}",
            new AcquireContainerLockRequest
            {
                ResourceType = LockResourceType,
                ResourceId = containerId,
                InstanceId = SelfCallInstanceId
            },
            context,
            SkillName,
            cancellationToken);

        if (!lockResult.Success)
        {
            return SkillResult.Error(lockResult.ErrorMessage!);
        }

        var acquiredLock = lockResult.Value;
        if (acquiredLock is null)
        {
            return SkillResult.Error("The container edit lock could not be acquired — nothing was deleted.");
        }

        if (!acquiredLock.Acquired)
        {
            return SkillResult.Error(acquiredLock.IsSelfConflict
                ? $"You have container '{container.Name}' open for editing yourself, and an assistant-driven " +
                  "write cannot share that editing session. Close the container page and try again, or delete " +
                  "the weekday templates directly on the page you already have open."
                : $"Container '{container.Name}' is currently being edited by {acquiredLock.UserName}.");
        }

        try
        {
            var containersRoute = _routes.Resolve(typeof(ContainerTemplateResource));
            var templatesRoute = $"{containersRoute}/{containerId}/{TemplatesRouteSegment}";

            var deleteResult = await _selfApi.DeleteAsync<List<ContainerTemplateResource>>(
                templatesRoute,
                context,
                SkillName,
                cancellationToken);

            if (!deleteResult.Success)
            {
                return SkillResult.Error(deleteResult.ErrorMessage!);
            }

            // Confirmed from the endpoint's own response (the deleted rows) rather than by re-reading the
            // repository: the delete ran in the API's own request scope over the self-call, so this
            // instance's DbContext would answer a re-read from state it never saw change.
            var removed = deleteResult.Value;
            if (removed is null || removed.Count == 0)
            {
                return SkillResult.Error(
                    $"The delete of the weekday templates of container '{container.Name}' returned no removed " +
                    "template, so the write could not be confirmed.");
            }

            var deleted = existingTemplates
                .Select(t => new
                {
                    ContainerTemplateId = t.Id,
                    Weekday = ToIsoWeekday(t.Weekday),
                    FromTime = t.FromTime.ToString("HH\\:mm"),
                    UntilTime = t.UntilTime.ToString("HH\\:mm"),
                    t.IsHoliday,
                    t.IsWeekdayAndHoliday,
                    t.StartBase,
                    t.EndBase,
                    ItemCount = t.ContainerTemplateItems.Count
                })
                .OrderBy(t => t.Weekday)
                .ToList();

            var itemCount = existingTemplates.Sum(t => t.ContainerTemplateItems.Count);
            var deletedCount = removed.Count;

            return SkillResult.SuccessResult(
                new
                {
                    ContainerId = containerId,
                    ContainerName = container.Name,
                    DeletedTemplateCount = deletedCount,
                    DeletedItemCount = itemCount,
                    DeletedTemplates = deleted,
                    Verified = true
                },
                $"Deleted all {deletedCount} weekday template(s) of container '{container.Name}' " +
                $"({itemCount} configured task(s) removed with them). Re-create them with " +
                "create_container_template and add_container_template_task.");
        }
        finally
        {
            await ReleaseLockAsync(acquiredLock.Id, context);
        }
    }

    private static int ToIsoWeekday(int storageWeekday) =>
        storageWeekday == StorageSunday ? IsoSunday : storageWeekday;

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
