// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_group")]
public class CreateGroupSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupSkill(
        IGroupRepository groupRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _unitOfWork = unitOfWork;
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

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(parentIdStr))
        {
            if (!Guid.TryParse(parentIdStr, out var parsedParent))
            {
                return SkillResult.Error($"Invalid parentId UUID: {parentIdStr}");
            }

            var parent = await _groupRepository.Get(parsedParent);
            if (parent == null)
            {
                return SkillResult.Error($"Parent group '{parsedParent}' not found.");
            }
            parentId = parsedParent;
        }

        Guid? calendarSelectionId = null;
        if (!string.IsNullOrWhiteSpace(calendarIdStr))
        {
            if (!Guid.TryParse(calendarIdStr, out var parsedCalendar))
            {
                return SkillResult.Error($"Invalid calendarId UUID: {calendarIdStr}");
            }

            if (!await _calendarSelectionRepository.Exists(parsedCalendar))
            {
                return SkillResult.Error($"Calendar selection '{parsedCalendar}' not found. Use list_calendars to see available IDs.");
            }
            calendarSelectionId = parsedCalendar;
        }

        var paymentInterval = PaymentInterval.Monthly;
        if (!string.IsNullOrWhiteSpace(paymentIntervalStr))
        {
            if (!Enum.TryParse(paymentIntervalStr, ignoreCase: true, out paymentInterval))
            {
                return SkillResult.Error($"Invalid paymentInterval '{paymentIntervalStr}'. Allowed: Weekly, Biweekly, Monthly, Individual.");
            }
        }

        var validFrom = DateTime.UtcNow.Date;
        if (!string.IsNullOrEmpty(validFromStr) && DateTime.TryParse(validFromStr, out var parsedValidFrom))
        {
            validFrom = parsedValidFrom;
        }

        DateTime? validUntil = null;
        if (!string.IsNullOrEmpty(validUntilStr) && DateTime.TryParse(validUntilStr, out var parsedValidUntil))
        {
            validUntil = parsedValidUntil;
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
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

        await _groupRepository.Add(group);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                GroupId = group.Id,
                group.Name,
                ParentId = parentId,
                group.ValidFrom,
                group.ValidUntil,
                PaymentInterval = paymentInterval.ToString(),
                CalendarSelectionId = calendarSelectionId
            },
            $"Group '{name}' was created" + (parentId.HasValue ? $" under parent {parentId.Value}." : " at root level."));
    }
}
