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

        CreateMap<Shift, ShiftResource>();

        CreateMap<ShiftResource, Shift>()
            .IgnoreAuditFields();

        CreateMap<TruncatedShift, TruncatedShiftResource>();
    }
}