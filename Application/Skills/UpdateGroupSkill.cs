// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing group's name, description, validity dates, payment interval and
/// holiday-calendar assignment. The write runs in a transaction and is verified by
/// re-reading the group from the database; a mismatch rolls the update back. Reparenting
/// is NOT done here — use move_group, which keeps the nested-set invariants intact.
/// </summary>
/// <param name="groupId">Required. UUID of the group to update.</param>
/// <param name="name">Optional. New display name.</param>
/// <param name="description">Optional. New description.</param>
/// <param name="validFrom">Optional. ISO date.</param>
/// <param name="validUntil">Optional. ISO date.</param>
/// <param name="paymentInterval">Optional. Weekly, Biweekly, Monthly or Individual — the period grid of the schedule.</param>
/// <param name="calendarId">Optional. UUID of the calendar selection (holiday calendar); empty string clears it.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "update_group";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGroupSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ICalendarSelectionRepository calendarSelectionRepository,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _calendarSelectionRepository = calendarSelectionRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID '{groupId}' not found.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsInScope(group))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(group.Name));
        }

        var changed = new List<string>();
        var verifications = new List<Func<Group, bool>>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name != group.Name)
        {
            group.Name = name;
            changed.Add("name");
            verifications.Add(persisted => persisted.Name == name);
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description != group.Description)
        {
            group.Description = description;
            changed.Add("description");
            verifications.Add(persisted => persisted.Description == description);
        }

        var validFromStr = GetParameter<string>(parameters, "validFrom");
        if (!string.IsNullOrEmpty(validFromStr) && DateTime.TryParse(validFromStr, out var parsedValidFrom))
        {
            if (group.ValidFrom != parsedValidFrom)
            {
                group.ValidFrom = parsedValidFrom;
                changed.Add("validFrom");
                verifications.Add(persisted => persisted.ValidFrom == parsedValidFrom);
            }
        }

        var validUntilStr = GetParameter<string>(parameters, "validUntil");
        if (validUntilStr != null)
        {
            DateTime? newValidUntil = null;
            if (validUntilStr.Length > 0 && DateTime.TryParse(validUntilStr, out var parsedValidUntil))
            {
                newValidUntil = parsedValidUntil;
            }
            if (group.ValidUntil != newValidUntil)
            {
                group.ValidUntil = newValidUntil;
                changed.Add("validUntil");
                verifications.Add(persisted => persisted.ValidUntil == newValidUntil);
            }
        }

        var paymentIntervalStr = GetParameter<string>(parameters, "paymentInterval");
        if (!string.IsNullOrWhiteSpace(paymentIntervalStr))
        {
            if (!Enum.TryParse<PaymentInterval>(paymentIntervalStr, ignoreCase: true, out var paymentInterval))
            {
                return SkillResult.Error(
                    $"Invalid paymentInterval '{paymentIntervalStr}'. Allowed: Weekly, Biweekly, Monthly, Individual.");
            }

            if (group.PaymentInterval != paymentInterval)
            {
                group.PaymentInterval = paymentInterval;
                changed.Add("paymentInterval");
                verifications.Add(persisted => persisted.PaymentInterval == paymentInterval);
            }
        }

        var calendarIdStr = GetParameter<string>(parameters, "calendarId");
        if (calendarIdStr != null)
        {
            Guid? newCalendarId = null;
            if (calendarIdStr.Length > 0)
            {
                if (!Guid.TryParse(calendarIdStr, out var parsedCalendar))
                {
                    return SkillResult.Error($"Invalid calendarId UUID: {calendarIdStr}");
                }

                if (!await _calendarSelectionRepository.Exists(parsedCalendar))
                {
                    return SkillResult.Error(
                        $"Calendar selection '{parsedCalendar}' not found. Use list_calendars to see available IDs.");
                }

                newCalendarId = parsedCalendar;
            }

            if (group.CalendarSelectionId != newCalendarId)
            {
                group.CalendarSelectionId = newCalendarId;
                changed.Add("calendarId");
                verifications.Add(persisted => persisted.CalendarSelectionId == newCalendarId);
            }
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { GroupId = groupId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — group left unchanged.");
        }

        group.UpdateTime = DateTime.UtcNow;
        group.CurrentUserUpdated = context.UserName;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _groupRepository.Put(group);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _groupRepository.GetNoTracking(groupId),
                    persisted => verifications.All(check => check(persisted)),
                    $"the update ({string.Join(", ", changed)}) of group '{group.Name}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new
            {
                GroupId = groupId,
                ChangedFields = changed,
                group.Name,
                group.ValidFrom,
                group.ValidUntil,
                PaymentInterval = group.PaymentInterval.ToString(),
                group.CalendarSelectionId
            },
            $"Group '{group.Name}' updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }
}
