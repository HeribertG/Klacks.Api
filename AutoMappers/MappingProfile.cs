using AutoMapper;
using Klacks.Api.Models.Associations;
using Klacks.Api.Models.Authentification;
using Klacks.Api.Models.CalendarSelections;
using Klacks.Api.Models.Schedules;
using Klacks.Api.Models.Settings;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Registrations;
using Klacks.Api.Resources.Schedules;
using Klacks.Api.Resources.Settings;
using Klacks.Api.Resources.Staffs;

namespace Klacks.Api.AutoMappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        CreateMap<TruncatedClient, TruncatedClientResource>()
            .ForMember(dest => dest.Clients, opt => opt.MapFrom(src => src.Clients))
            ;

        CreateMap<Address, AddressResource>()
          ;

        CreateMap<AddressResource, Address>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
        ;

        CreateMap<Communication, CommunicationResource>()
          ;
        CreateMap<CommunicationResource, Communication>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
        ;

        CreateMap<Annotation, AnnotationResource>()
          ;
        CreateMap<AnnotationResource, Annotation>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
         ;

        CreateMap<Countries, CountryResource>()
          .ForMember(x => x.Select, o => o.Ignore())
                 ;

        CreateMap<CountryResource, Countries>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<Membership, MembershipResource>()
          ;

        CreateMap<MembershipResource, Membership>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
         ;

        CreateMap<Client, ClientResource>()
          ;

        CreateMap<ClientResource, Client>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          .ForMember(x => x.Breaks, o => o.Ignore())
          ;

        CreateMap<Client, ClientBreakResource>()
          .ForMember(x => x.Membership, o => o.Ignore())
          ;

        CreateMap<Client, ClientWorkResource>()
         .ForMember(x => x.Membership, o => o.Ignore())
         .ForMember(x => x.NeededRows, o => o.Ignore())
         ;

        CreateMap<Break, BreakResource>();
        CreateMap<BreakResource, Break>()
           .ForMember(dest => dest.Absence, opt => opt.Ignore())
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.BreakReasonId, opt => opt.Ignore())
          .ForMember(dest => dest.BreakReason, opt => opt.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<RegistrationResource, AppUser>()
          .ForMember(x => x.Id, o => o.Ignore())
          .ForMember(x => x.NormalizedUserName, o => o.Ignore())
          .ForMember(x => x.NormalizedEmail, o => o.Ignore())
          .ForMember(x => x.EmailConfirmed, o => o.Ignore())
          .ForMember(x => x.PasswordHash, o => o.Ignore())
          .ForMember(x => x.SecurityStamp, o => o.Ignore())
          .ForMember(x => x.ConcurrencyStamp, o => o.Ignore())
          .ForMember(x => x.PhoneNumber, o => o.Ignore())
          .ForMember(x => x.PhoneNumberConfirmed, o => o.Ignore())
          .ForMember(x => x.TwoFactorEnabled, o => o.Ignore())
          .ForMember(x => x.LockoutEnabled, o => o.Ignore())
          .ForMember(x => x.AccessFailedCount, o => o.Ignore())
          .ForMember(x => x.LockoutEnd, o => o.Ignore())
          ;

        CreateMap<CalendarRule, CalendarRuleResource>().ReverseMap();

        CreateMap<TruncatedAbsence_dto, TruncatedAbsence>();

        CreateMap<Macro, MacroResource>()
          ;

        CreateMap<MacroResource, Macro>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<Absence, AbsenceResource>()
          ;

        CreateMap<AbsenceResource, Absence>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
         ;

        CreateMap<State, StateResource>()
          ;

        CreateMap<StateResource, State>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
         ;

        CreateMap<CalendarSelection, CalendarSelectionResource>()
          ;

        CreateMap<CalendarSelectionResource, CalendarSelection>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<SelectedCalendar, SelectedCalendarResource>()
          ;

        CreateMap<SelectedCalendarResource, SelectedCalendar>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CalendarSelection, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
         ;

        CreateMap<Group, GroupResource>()
          .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems))
          ;

        CreateMap<GroupResource, Group>()
          .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems))
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
        ;

        CreateMap<GroupItem, GroupItemResource>()
          ;

        CreateMap<GroupItemResource, GroupItem>()
          .ForMember(dest => dest.Group, opt => opt.Ignore())
          .ForMember(x => x.Client, o => o.Ignore())
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
        ;

        CreateMap<TruncatedGroup, TruncatedGroupResource>()
         ;

        CreateMap<Work, WorkResource>();
        CreateMap<WorkResource, Work>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<Shift, ShiftResource>();
        CreateMap<ShiftResource, Shift>()
          .ForMember(x => x.CreateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserCreated, o => o.Ignore())
          .ForMember(x => x.UpdateTime, o => o.Ignore())
          .ForMember(x => x.CurrentUserUpdated, o => o.Ignore())
          .ForMember(x => x.DeletedTime, o => o.Ignore())
          .ForMember(x => x.IsDeleted, o => o.Ignore())
          .ForMember(x => x.CurrentUserDeleted, o => o.Ignore())
          ;

        CreateMap<GroupCreateResource, Group>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore());

        CreateMap<GroupUpdateResource, Group>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.ParentId))
            .ForMember(dest => dest.Root, opt => opt.Ignore());

        CreateMap<Group, GroupTreeNodeResource>()
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.Parent))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.rgt))
            .ForMember(dest => dest.Depth, opt => opt.Ignore())
            .ForMember(dest => dest.ClientsCount, opt => opt.Ignore())
            .ForMember(dest => dest.Clients, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore());
    }
}