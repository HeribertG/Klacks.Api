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
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
        ;

        CreateMap<Communication, CommunicationResource>()
          ;
        CreateMap<CommunicationResource, Communication>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
        ;

        CreateMap<Annotation, AnnotationResource>()
          ;
        CreateMap<AnnotationResource, Annotation>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;

        CreateMap<Countries, CountryResource>()
          .ForMember(dest => dest.Select, opt => opt.Ignore())
                 ;

        CreateMap<CountryResource, Countries>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<Membership, MembershipResource>()
          ;

        CreateMap<MembershipResource, Membership>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;

        CreateMap<Client, ClientResource>()
          ;

        CreateMap<ClientResource, Client>()
          .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.Breaks, opt => opt.Ignore())
          ;

        CreateMap<Client, ClientBreakResource>()
          .ForMember(dest => dest.Membership, opt => opt.Ignore())
          ;

        CreateMap<Client, ClientWorkResource>()
         .ForMember(dest => dest.Membership, opt => opt.Ignore())
         .ForMember(dest => dest.NeededRows, opt => opt.Ignore())
         ;

        CreateMap<Break, BreakResource>();
        CreateMap<BreakResource, Break>()
           .ForMember(dest => dest.Absence, opt => opt.Ignore())
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.BreakReasonId, opt => opt.Ignore())
          .ForMember(dest => dest.BreakReason, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<RegistrationResource, AppUser>()
          .ForMember(dest => dest.Id, opt => opt.Ignore())
          .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
          .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
          .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
          .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
          .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
          .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
          .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
          .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
          .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
          .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
          .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
          .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
          ;

        CreateMap<CalendarRule, CalendarRuleResource>().ReverseMap();

        CreateMap<TruncatedAbsence_dto, TruncatedAbsence>();

        CreateMap<Macro, MacroResource>()
          ;

        CreateMap<MacroResource, Macro>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<Absence, AbsenceResource>()
          ;

        CreateMap<AbsenceResource, Absence>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;

        CreateMap<State, StateResource>()
          ;

        CreateMap<StateResource, State>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;

        CreateMap<CalendarSelection, CalendarSelectionResource>()
          ;

        CreateMap<CalendarSelectionResource, CalendarSelection>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<SelectedCalendar, SelectedCalendarResource>()
          ;

        CreateMap<SelectedCalendarResource, SelectedCalendar>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CalendarSelection, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;
         
        CreateMap<SimpleGroupResource, Group>()
             .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.Rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore())
            ;

        CreateMap<Group, SimpleGroupResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidUntil))
            ;

        CreateMap<Group, GroupResource>()
          .ForMember(dest => dest.Children, opt => opt.Ignore())
          .ForMember(dest => dest.Depth, opt => opt.Ignore())
          .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems))
          ;

        CreateMap<GroupResource, Group>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems))
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Lft, opt => opt.Ignore())
            .ForMember(dest => dest.Rgt, opt => opt.Ignore())
            .ForMember(dest => dest.Root, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            ;

        CreateMap<GroupItem, GroupItemResource>()
          ;

        CreateMap<GroupItemResource, GroupItem>()
        .ForMember(dest => dest.Group, opt => opt.Ignore())
        .ForMember(dest => dest.Client, opt => opt.Ignore())
        .ForMember(dest => dest.Shift, opt => opt.Ignore())
        .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
        .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
        .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
        .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
        .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
        .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
        .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
        ;

        CreateMap<TruncatedGroup, TruncatedGroupResource>()
         ;

        CreateMap<GroupVisibility, GroupVisibilityResource>()
            ;
        CreateMap<GroupVisibilityResource, GroupVisibility>()
          .ForMember(dest => dest.AppUser, opt => opt.Ignore()) 
          .ForMember(dest => dest.Group, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
           ;

        CreateMap<Work, WorkResource>();
        CreateMap<WorkResource, Work>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<Shift, ShiftResource>()
            ;
        CreateMap<ShiftResource, Shift>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<AssignedGroup, AssignedGroupResource>()
            .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Group.Name))
            ;

        CreateMap<AssignedGroupResource, AssignedGroup>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.Group, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;

        CreateMap<TruncatedShift, TruncatedShiftResource>()
         ;
    }
}