using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Notifications;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using Staffs = Klacks.Api.Presentation.DTOs.Staffs;
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

    public partial WorkResource ToWorkResource(Work work);

    [MapperIgnoreTarget(nameof(Work.CreateTime))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Work.UpdateTime))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Work.DeletedTime))]
    [MapperIgnoreTarget(nameof(Work.IsDeleted))]
    [MapperIgnoreTarget(nameof(Work.CurrentUserDeleted))]
    public partial Work ToWorkEntity(WorkResource resource);

    public partial BreakResource ToBreakResource(Break @break);

    [MapperIgnoreTarget(nameof(Break.CreateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Break.UpdateTime))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Break.DeletedTime))]
    [MapperIgnoreTarget(nameof(Break.IsDeleted))]
    [MapperIgnoreTarget(nameof(Break.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Break.Client))]
    public partial Break ToBreakEntity(BreakResource resource);

    public ShiftResource ToShiftResource(Shift shift)
    {
        var resource = ToShiftResourceBase(shift);
        resource.Groups = shift.GroupItems?.Select(gi => new SimpleGroupResource
        {
            Id = gi.Group?.Id ?? gi.GroupId,
            Name = gi.Group?.Name ?? string.Empty,
            Description = gi.Group?.Description ?? string.Empty,
            ValidFrom = gi.ValidFrom ?? DateTime.MinValue,
            ValidUntil = gi.ValidUntil
        }).ToList() ?? [];

        if (shift.Client != null)
        {
            resource.Client = ToShiftClientResource(shift.Client);
        }

        return resource;
    }

    private static Staffs.ClientResource ToShiftClientResource(Client client)
    {
        return new Staffs.ClientResource
        {
            Id = client.Id,
            Name = client.Name,
            FirstName = client.FirstName,
            SecondName = client.SecondName,
            MaidenName = client.MaidenName,
            Title = client.Title,
            Company = client.Company,
            Gender = client.Gender,
            IdNumber = client.IdNumber,
            LegalEntity = client.LegalEntity,
            Birthdate = client.Birthdate,
            IsDeleted = client.IsDeleted,
            Type = (int)client.Type,
            Addresses = client.Addresses?.Select(ToShiftAddressResource).ToList()
                ?? []
        };
    }

    private static Staffs.AddressResource ToShiftAddressResource(Address address)
    {
        return new Staffs.AddressResource
        {
            Id = address.Id,
            ClientId = address.ClientId,
            ValidFrom = address.ValidFrom,
            Type = address.Type,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            Street = address.Street,
            Street2 = address.Street2,
            Street3 = address.Street3,
            Zip = address.Zip,
            City = address.City,
            State = address.State,
            Country = address.Country,
            Latitude = address.Latitude,
            Longitude = address.Longitude
        };
    }

    [MapperIgnoreTarget(nameof(ShiftResource.Groups))]
    [MapperIgnoreSource(nameof(Shift.Client))]
    private partial ShiftResource ToShiftResourceBase(Shift shift);

    [MapperIgnoreTarget(nameof(Shift.CreateTime))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Shift.UpdateTime))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Shift.DeletedTime))]
    [MapperIgnoreTarget(nameof(Shift.IsDeleted))]
    [MapperIgnoreTarget(nameof(Shift.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Shift.Client))]
    [MapperIgnoreTarget(nameof(Shift.GroupItems))]
    private partial Shift ToShiftEntityBase(ShiftResource resource);

    public Shift ToShiftEntity(ShiftResource resource)
    {
        var entity = ToShiftEntityBase(resource);
        entity.GroupItems = resource.Groups?.Select(g => new GroupItem
        {
            GroupId = g.Id,
            ValidFrom = g.ValidFrom,
            ValidUntil = g.ValidUntil
        }).ToList() ?? [];
        return entity;
    }

    public partial TruncatedShiftResource ToTruncatedShiftResource(TruncatedShift truncatedShift);

    public Shift CloneShift(Shift source)
    {
        return new Shift
        {
            Id = Guid.Empty,
            CuttingAfterMidnight = source.CuttingAfterMidnight,
            Abbreviation = source.Abbreviation,
            Description = source.Description,
            MacroId = source.MacroId,
            Name = source.Name,
            Status = source.Status,
            AfterShift = source.AfterShift,
            BeforeShift = source.BeforeShift,
            EndShift = source.EndShift,
            FromDate = source.FromDate,
            StartShift = source.StartShift,
            UntilDate = source.UntilDate,
            BriefingTime = source.BriefingTime,
            DebriefingTime = source.DebriefingTime,
            TravelTimeAfter = source.TravelTimeAfter,
            TravelTimeBefore = source.TravelTimeBefore,
            IsFriday = source.IsFriday,
            IsHoliday = source.IsHoliday,
            IsMonday = source.IsMonday,
            IsSaturday = source.IsSaturday,
            IsSunday = source.IsSunday,
            IsThursday = source.IsThursday,
            IsTuesday = source.IsTuesday,
            IsWednesday = source.IsWednesday,
            IsWeekdayAndHoliday = source.IsWeekdayAndHoliday,
            IsSporadic = source.IsSporadic,
            SporadicScope = source.SporadicScope,
            IsTimeRange = source.IsTimeRange,
            Quantity = source.Quantity,
            SumEmployees = source.SumEmployees,
            WorkTime = source.WorkTime,
            ShiftType = source.ShiftType,
            OriginalId = source.OriginalId,
            ParentId = source.ParentId,
            RootId = source.RootId,
            Lft = source.Lft,
            Rgt = source.Rgt,
            ClientId = source.ClientId,
            GroupItems = source.GroupItems.Select(gi => new GroupItem
            {
                Id = Guid.Empty,
                GroupId = gi.GroupId,
                ValidFrom = gi.ValidFrom,
                ValidUntil = gi.ValidUntil,
                ShiftId = Guid.Empty
            }).ToList()
        };
    }

    [MapperIgnoreSource(nameof(ContainerTemplate.Shift))]
    [MapperIgnoreSource(nameof(ContainerTemplate.ContainerTemplateItems))]
    private partial ContainerTemplateResource ToContainerTemplateResourceBase(ContainerTemplate template);

    public ContainerTemplateResource ToContainerTemplateResource(ContainerTemplate template)
    {
        var resource = ToContainerTemplateResourceBase(template);
        if (template.Shift != null)
        {
            resource.Shift = ToShiftResource(template.Shift);
        }
        resource.ContainerTemplateItems = template.ContainerTemplateItems
            .Select(ToContainerTemplateItemResource)
            .ToList();
        return resource;
    }

    [MapperIgnoreTarget(nameof(ContainerTemplate.CreateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.UpdateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.DeletedTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.IsDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplate.Shift))]
    public partial ContainerTemplate ToContainerTemplateEntity(ContainerTemplateResource resource);

    public partial RouteInfoResource ToRouteInfoResource(RouteInfo routeInfo);
    public partial RouteInfo ToRouteInfoEntity(RouteInfoResource resource);

    public partial RouteLocationResource ToRouteLocationResource(RouteLocation location);
    public partial RouteLocation ToRouteLocationEntity(RouteLocationResource resource);

    public partial RouteSegmentDirectionsResource ToRouteSegmentDirectionsResource(RouteSegmentDirections directions);
    public partial RouteSegmentDirections ToRouteSegmentDirectionsEntity(RouteSegmentDirectionsResource resource);

    public partial DirectionStepResource ToDirectionStepResource(DirectionStep step);
    public partial DirectionStep ToDirectionStepEntity(DirectionStepResource resource);

    [MapperIgnoreTarget(nameof(ContainerTemplateItemResource.Weekday))]
    [MapperIgnoreSource(nameof(ContainerTemplateItem.Shift))]
    private partial ContainerTemplateItemResource ToContainerTemplateItemResourceBase(ContainerTemplateItem item);

    public ContainerTemplateItemResource ToContainerTemplateItemResource(ContainerTemplateItem item)
    {
        var resource = ToContainerTemplateItemResourceBase(item);
        if (item.Shift != null)
        {
            resource.Shift = ToShiftResource(item.Shift);
        }
        return resource;
    }

    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CreateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.UpdateTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.DeletedTime))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.IsDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.Shift))]
    [MapperIgnoreTarget(nameof(ContainerTemplateItem.ContainerTemplate))]
    public partial ContainerTemplateItem ToContainerTemplateItemEntity(ContainerTemplateItemResource resource);

    public partial ShiftScheduleResource ToShiftScheduleResource(ShiftDayAssignment assignment);

    public partial List<ShiftScheduleResource> ToShiftScheduleResourceList(List<ShiftDayAssignment> assignments);

    public partial WorkScheduleResource ToWorkScheduleResource(ScheduleCell entry);

    public partial List<WorkScheduleResource> ToWorkScheduleResourceList(List<ScheduleCell> entries);

    public partial WorkScheduleClientResource ToWorkScheduleClientResource(Client client);

    public partial List<WorkScheduleClientResource> ToWorkScheduleClientResourceList(List<Client> clients);

    public partial WorkChangeResource ToWorkChangeResource(WorkChange workChange);

    public partial List<WorkChangeResource> ToWorkChangeResourceList(List<WorkChange> workChanges);

    [MapperIgnoreTarget(nameof(WorkChange.CreateTime))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(WorkChange.UpdateTime))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(WorkChange.DeletedTime))]
    [MapperIgnoreTarget(nameof(WorkChange.IsDeleted))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(WorkChange.Work))]
    [MapperIgnoreTarget(nameof(WorkChange.ReplaceClient))]
    public partial WorkChange ToWorkChangeEntity(WorkChangeResource resource);

    public partial ExpensesResource ToExpensesResource(Expenses expenses);

    public partial List<ExpensesResource> ToExpensesResourceList(List<Expenses> expenses);

    [MapperIgnoreTarget(nameof(Expenses.CreateTime))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Expenses.UpdateTime))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Expenses.DeletedTime))]
    [MapperIgnoreTarget(nameof(Expenses.IsDeleted))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Expenses.Work))]
    public partial Expenses ToExpensesEntity(ExpensesResource resource);

    public WorkNotificationDto ToWorkNotificationDto(Work work, string operationType, string sourceConnectionId)
    {
        return new WorkNotificationDto
        {
            WorkId = work.Id,
            ClientId = work.ClientId,
            ShiftId = work.ShiftId,
            CurrentDate = work.CurrentDate,
            OperationType = operationType,
            SourceConnectionId = sourceConnectionId
        };
    }

    public ShiftStatsNotificationDto ToShiftStatsNotificationDto(ShiftDayAssignment shiftData, string sourceConnectionId)
    {
        return new ShiftStatsNotificationDto
        {
            ShiftId = shiftData.ShiftId,
            Date = shiftData.Date.ToDateTime(TimeOnly.MinValue),
            Engaged = shiftData.Engaged,
            SourceConnectionId = sourceConnectionId
        };
    }
}
