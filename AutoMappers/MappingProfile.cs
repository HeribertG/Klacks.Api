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
            ;

        CreateMap<Address, AddressResource>()
          ;

        CreateMap<AddressResource, Address>()
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
          .ForMember(x => x.IsDeleted, o => o.Ignore())
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
          ;

        CreateMap<GroupResource, Group>()
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
    }
}
