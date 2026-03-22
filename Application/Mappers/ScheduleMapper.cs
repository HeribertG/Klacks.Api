// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Main file of the ScheduleMapper (Mapperly) with Contract, Membership, BreakPlaceholder and Calendar mappings.
/// Additional mappings in partial files: Shifts, Containers, Schedules, Notifications.
/// </summary>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.DTOs.Settings;
using Staffs = Klacks.Api.Application.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class ScheduleMapper
{
    public partial ContractResource ToContractResource(Contract contract);

    [MapperIgnoreTarget(nameof(Contract.CreateTime))]
    [MapperIgnoreTarget(nameof(Contract.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Contract.UpdateTime))]
    [MapperIgnoreTarget(nameof(Contract.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Contract.DeletedTime))]
    [MapperIgnoreTarget(nameof(Contract.IsDeleted))]
    [MapperIgnoreTarget(nameof(Contract.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Contract.CalendarSelection))]
    [MapperIgnoreTarget(nameof(Contract.SchedulingRule))]
    public partial Contract ToContractEntity(ContractResource resource);

    public void UpdateContractEntity(Contract target, ContractResource source)
    {
        target.Name = source.Name;
        target.GuaranteedHours = source.GuaranteedHours;
        target.MaximumHours = source.MaximumHours;
        target.MinimumHours = source.MinimumHours;
        target.FullTime = source.FullTime;
        target.NightRate = source.NightRate;
        target.HolidayRate = source.HolidayRate;
        target.SaRate = source.SaRate;
        target.SoRate = source.SoRate;
        target.PaymentInterval = source.PaymentInterval;
        target.ValidFrom = source.ValidFrom;
        target.ValidUntil = source.ValidUntil;
        target.CalendarSelectionId = source.CalendarSelectionId;
        target.SchedulingRuleId = source.SchedulingRuleId;
    }

    public partial MembershipResource ToMembershipResource(Membership membership);

    [MapperIgnoreTarget(nameof(Membership.CreateTime))]
    [MapperIgnoreTarget(nameof(Membership.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Membership.UpdateTime))]
    [MapperIgnoreTarget(nameof(Membership.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Membership.DeletedTime))]
    [MapperIgnoreTarget(nameof(Membership.IsDeleted))]
    [MapperIgnoreTarget(nameof(Membership.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Membership.Client))]
    public partial Membership ToMembershipEntity(MembershipResource resource);

    public partial BreakPlaceholderResource ToBreakPlaceholderResource(BreakPlaceholder @break);

    [MapperIgnoreTarget(nameof(BreakPlaceholder.CreateTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.UpdateTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.DeletedTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.IsDeleted))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.Absence))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.Client))]
    public partial BreakPlaceholder ToBreakPlaceholderEntity(BreakPlaceholderResource resource);

    [MapperIgnoreTarget(nameof(BreakPlaceholder.Id))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CreateTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.UpdateTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.DeletedTime))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.IsDeleted))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.Absence))]
    [MapperIgnoreTarget(nameof(BreakPlaceholder.Client))]
    public partial void UpdateBreakEntity(BreakPlaceholderResource resource, BreakPlaceholder target);

    public partial CalendarRuleResource ToCalendarRuleResource(CalendarRule rule);
    public partial CalendarRule ToCalendarRuleEntity(CalendarRuleResource resource);

    public void UpdateCalendarRuleEntity(CalendarRuleResource source, CalendarRule target)
    {
        target.Country = source.Country;
        target.Description = source.Description ?? new();
        target.IsMandatory = source.IsMandatory;
        target.IsPaid = source.IsPaid;
        target.Name = source.Name ?? new();
        target.Rule = source.Rule;
        target.State = source.State;
        target.SubRule = source.SubRule;
    }

    public partial CalendarSelectionResource ToCalendarSelectionResource(CalendarSelection selection);

    [MapperIgnoreTarget(nameof(CalendarSelection.CreateTime))]
    [MapperIgnoreTarget(nameof(CalendarSelection.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(CalendarSelection.UpdateTime))]
    [MapperIgnoreTarget(nameof(CalendarSelection.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(CalendarSelection.DeletedTime))]
    [MapperIgnoreTarget(nameof(CalendarSelection.IsDeleted))]
    [MapperIgnoreTarget(nameof(CalendarSelection.CurrentUserDeleted))]
    public partial CalendarSelection ToCalendarSelectionEntity(CalendarSelectionResource resource);

    public partial SelectedCalendarResource ToSelectedCalendarResource(SelectedCalendar calendar);

    [MapperIgnoreTarget(nameof(SelectedCalendar.CreateTime))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.UpdateTime))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.DeletedTime))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.IsDeleted))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(SelectedCalendar.CalendarSelection))]
    public partial SelectedCalendar ToSelectedCalendarEntity(SelectedCalendarResource resource);
}
