using AutoMapper;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;

namespace Klacks.Api.Application.AutoMapper;

public class ScheduleMappingProfile : Profile
{
    public ScheduleMappingProfile()
    {
        CreateMap<Contract, ContractResource>()
            .ForMember(dest => dest.CalendarSelection, opt => opt.MapFrom(src => src.CalendarSelection));

        CreateMap<ContractResource, Contract>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.CalendarSelection, opt => opt.Ignore());

        CreateMap<Membership, MembershipResource>();

        CreateMap<MembershipResource, Membership>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<Break, BreakResource>();

        CreateMap<BreakResource, Break>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Absence, opt => opt.Ignore())
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            .ForMember(dest => dest.BreakReasonId, opt => opt.Ignore())
            .ForMember(dest => dest.BreakReason, opt => opt.Ignore());

        CreateMap<CalendarRule, CalendarRuleResource>().ReverseMap();

        CreateMap<CalendarSelection, CalendarSelectionResource>();

        CreateMap<CalendarSelectionResource, CalendarSelection>()
            .IgnoreAuditFields();

        CreateMap<SelectedCalendar, SelectedCalendarResource>();

        CreateMap<SelectedCalendarResource, SelectedCalendar>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.CalendarSelection, opt => opt.Ignore());

        CreateMap<Work, WorkResource>()
            .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client))
            .ForMember(dest => dest.Shift, opt => opt.MapFrom(src => src.Shift));

        CreateMap<WorkResource, Work>()
            .IgnoreAuditFields();

        CreateMap<Shift, ShiftResource>()
            .ForMember(dest => dest.Groups, opt => opt.MapFrom(src =>
                src.GroupItems.Select(gi => new SimpleGroupResource
                {
                    Id = gi.GroupId,
                    Name = gi.Group != null ? gi.Group.Name : string.Empty,
                    Description = gi.Group != null ? gi.Group.Description : string.Empty,
                    ValidFrom = gi.ValidFrom ?? DateTime.MinValue,
                    ValidUntil = gi.ValidUntil
                })));

        CreateMap<ShiftResource, Shift>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src =>
                src.Groups.Select(g => new GroupItem
                {
                    GroupId = g.Id,
                    ValidFrom = g.ValidFrom,
                    ValidUntil = g.ValidUntil
                })))
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<TruncatedShift, TruncatedShiftResource>();

        CreateMap<Shift, Shift>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.Empty))
            .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src =>
                src.GroupItems.Select(gi => new GroupItem
                {
                    Id = Guid.Empty,
                    GroupId = gi.GroupId,
                    ValidFrom = gi.ValidFrom,
                    ValidUntil = gi.ValidUntil,
                    ShiftId = Guid.Empty
                })))
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<ContainerTemplate, ContainerTemplateResource>()
            .ForMember(dest => dest.Shift, opt => opt.MapFrom(src => src.Shift))
            .ForMember(dest => dest.ContainerTemplateItems, opt => opt.MapFrom(src => src.ContainerTemplateItems));

        CreateMap<ContainerTemplateResource, ContainerTemplate>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Shift, opt => opt.Ignore())
            .ForMember(dest => dest.ContainerTemplateItems, opt => opt.MapFrom(src => src.ContainerTemplateItems));

        CreateMap<ContainerTemplateItem, ContainerTemplateItemResource>()
            .ForMember(dest => dest.Shift, opt => opt.MapFrom(src => src.Shift))
            .ForMember(dest => dest.Weekday, opt => opt.Ignore());

        CreateMap<ContainerTemplateItemResource, ContainerTemplateItem>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Shift, opt => opt.Ignore())
            .ForMember(dest => dest.ContainerTemplate, opt => opt.Ignore());
    }
}