// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a new group (location, branch, organisational unit) with optional parent for
/// the nested-set hierarchy. ValidFrom defaults to today if not given.
/// </summary>
/// <param name="name">Required. Display name of the group.</param>
/// <param name="description">Optional. Free-text description.</param>
/// <param name="parentId">Optional. UUID of the parent group; root level if omitted.</param>
/// <param name="validFrom">Optional. ISO date the group becomes active; defaults to today.</param>
/// <param name="validUntil">Optional. ISO date the group becomes inactive.</param>
/// <param name="calendarId">Optional. UUID of the calendar selection (holiday calendar) for the group.</param>
/// <param name="paymentInterval">Optional. Payment interval: Weekly, Biweekly, Monthly or Individual; defaults to Monthly.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Constants;

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_group")]
public class CreateGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "create_group";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupVisibilityPreservationService _visibilityPreservation;

    public CreateGroupSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ICalendarSelectionRepository calendarSelectionRepository,
        GroupMapper groupMapper,
        IKlacksSelfApiClient selfApi,
        ICompanyClock companyClock,
        IGroupVisibilityPreservationService visibilityPreservation)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _calendarSelectionRepository = calendarSelectionRepository;
        _groupMapper = groupMapper;
        _selfApi = selfApi;
        _companyClock = companyClock;
        _visibilityPreservation = visibilityPreservation;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        var description = GetParameter<string>(parameters, "description") ?? string.Empty;
        var parentIdStr = GetParameter<string>(parameters, "parentId");
        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var validUntilStr = GetParameter<string>(parameters, "validUntil");
        var calendarIdStr = GetParameter<string>(parameters, "calendarId");
        var paymentIntervalStr = GetParameter<string>(parameters, "paymentInterval");

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);

        var (parentId, parentError) = await ResolveParentAsync(parentIdStr, scope);
        if (parentError != null)
        {
            return parentError;
        }

        var (calendarSelectionId, calendarError) = await ResolveCalendarAsync(calendarIdStr);
        if (calendarError != null)
        {
            return calendarError;
        }

        var (paymentInterval, paymentIntervalError) = ParsePaymentInterval(paymentIntervalStr);
        if (paymentIntervalError != null)
        {
            return paymentIntervalError;
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (parsedValidFrom, validUntil, dateError) = ParseValidity(
            validFromStr, validUntilStr, today, context.UserLanguage);
        if (dateError != null)
        {
            return dateError;
        }

        var validFrom = parsedValidFrom ?? today;

        var group = new Group
        {
            Name = name,
            Description = description,
            Parent = parentId,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            PaymentInterval = paymentInterval,
            CalendarSelectionId = calendarSelectionId,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        var usersKeepingFullVisibility =
            await _visibilityPreservation.CountUsersKeepingFullVisibilityAsync(cancellationToken);

        var result = await _selfApi.PostAsync<GroupResource>(
            SelfApiRoutes.Groups, _groupMapper.ToGroupResource(group), context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        if (result.Value == null)
        {
            return SkillResult.Error(
                $"Creating the group '{name}' returned no result — the operation may have failed.");
        }

        return BuildCreatedResult(
            result.Value.Id, group, name, parentId, paymentInterval, calendarSelectionId, usersKeepingFullVisibility);
    }

    private async Task<(Guid? ParentId, SkillResult? Error)> ResolveParentAsync(string? parentIdStr, GroupScopeAccess scope)
    {
        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(parentIdStr))
        {
            if (!Guid.TryParse(parentIdStr, out var parsedParent))
            {
                return (null, SkillResult.Error($"Invalid parentId UUID: {parentIdStr}"));
            }

            var parent = await _groupRepository.Get(parsedParent);
            if (parent == null)
            {
                return (null, SkillResult.Error($"Parent group '{parsedParent}' not found."));
            }

            if (!scope.IsInScope(parent))
            {
                return (null, SkillResult.Error(scope.BuildOutOfScopeError(parent.Name)));
            }
            parentId = parsedParent;
        }
        else if (!scope.IsUnrestricted)
        {
            return (null, SkillResult.Error(scope.BuildRootCreationError()));
        }

        return (parentId, null);
    }

    private async Task<(Guid? CalendarSelectionId, SkillResult? Error)> ResolveCalendarAsync(string? calendarIdStr)
    {
        Guid? calendarSelectionId = null;
        if (!string.IsNullOrWhiteSpace(calendarIdStr))
        {
            if (!Guid.TryParse(calendarIdStr, out var parsedCalendar))
            {
                return (null, SkillResult.Error($"Invalid calendarId UUID: {calendarIdStr}"));
            }

            if (!await _calendarSelectionRepository.Exists(parsedCalendar))
            {
                return (null, SkillResult.Error($"Calendar selection '{parsedCalendar}' not found. Use list_calendars to see available IDs."));
            }
            calendarSelectionId = parsedCalendar;
        }

        return (calendarSelectionId, null);
    }

    private static (PaymentInterval Interval, SkillResult? Error) ParsePaymentInterval(string? paymentIntervalStr)
    {
        var paymentInterval = PaymentInterval.Monthly;
        if (!string.IsNullOrWhiteSpace(paymentIntervalStr))
        {
            if (!Enum.TryParse(paymentIntervalStr, ignoreCase: true, out paymentInterval))
            {
                return (paymentInterval, SkillResult.Error($"Invalid paymentInterval '{paymentIntervalStr}'. Allowed: Weekly, Biweekly, Monthly, Individual."));
            }
        }

        return (paymentInterval, null);
    }

    private static (DateTime? ValidFrom, DateTime? ValidUntil, SkillResult? Error) ParseValidity(
        string? validFromStr, string? validUntilStr, DateTime today, string? userLanguage)
    {
        var (parsedValidFrom, invalidValidFrom) = SkillDateParser.ParseOptionalUtcDate(
            validFromStr, today, userLanguage);
        var (validUntil, invalidValidUntil) = SkillDateParser.ParseOptionalUtcDate(
            validUntilStr, today, userLanguage);
        if (invalidValidFrom)
        {
            return (null, null, SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validFrom", validFromStr!)));
        }

        if (invalidValidUntil)
        {
            return (null, null, SkillResult.Error(SkillDateParser.InvalidDateMessageFor("validUntil", validUntilStr!)));
        }

        return (parsedValidFrom, validUntil, null);
    }

    private static SkillResult BuildCreatedResult(
        Guid createdGroupId,
        Group group,
        string name,
        Guid? parentId,
        PaymentInterval paymentInterval,
        Guid? calendarSelectionId,
        int usersKeepingFullVisibility)
    {
        var visibilityAdvisory = FirstGroupVisibilityAdvisory.For(usersKeepingFullVisibility);

        return SkillResult.SuccessResult(
            new
            {
                GroupId = createdGroupId,
                group.Name,
                ParentId = parentId,
                group.ValidFrom,
                group.ValidUntil,
                PaymentInterval = paymentInterval.ToString(),
                CalendarSelectionId = calendarSelectionId,
                UsersKeepingFullVisibilityCount = usersKeepingFullVisibility
            },
            $"Group '{name}' was created" + (parentId.HasValue ? $" under parent {parentId.Value}." : " at root level.")
            + (visibilityAdvisory.Length > 0 ? " " + visibilityAdvisory : string.Empty));
    }
}
