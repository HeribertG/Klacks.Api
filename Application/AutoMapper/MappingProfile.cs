using AutoMapper;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Histories;
using Klacks.Api.Presentation.DTOs.Registrations;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Application.Queries.LLM;
namespace Klacks.Api.Application.AutoMapper;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<TruncatedClient, TruncatedClientResource>()
            .ForMember(dest => dest.Clients, opt => opt.MapFrom(src => src.Clients))
            .ForMember(dest => dest.Editor, opt => opt.MapFrom(src => src.Editor))
            .ForMember(dest => dest.LastChange, opt => opt.MapFrom(src => src.LastChange))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.MaxItems))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.MaxPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.CurrentPage))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => src.FirstItemOnPage));
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
        CreateMap<CommunicationType, CommunicationTypeResource>();
        CreateMap<CommunicationTypeResource, CommunicationType>();
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
          .ForMember(dest => dest.Contract, opt => opt.Ignore())
          .ForMember(dest => dest.ContractId, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
         ;
        CreateMap<Contract, ContractResource>()
          .ForMember(dest => dest.CalendarSelection, opt => opt.MapFrom(src => src.CalendarSelection))
          ;
        CreateMap<ContractResource, Contract>()
          .ForMember(dest => dest.CalendarSelection, opt => opt.Ignore())
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
        CreateMap<Client, ClientBreakResource>();
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
          .ForMember(dest => dest.PasswordResetToken, opt => opt.Ignore())
          .ForMember(dest => dest.PasswordResetTokenExpires, opt => opt.Ignore())
          ;
        CreateMap<CalendarRule, CalendarRuleResource>().ReverseMap();
        CreateMap<TruncatedAbsence, TruncatedAbsence>();
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
          .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client != null ? src.Client : null)) 
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
        CreateMap<BreakReason, BreakReasonResource>()
          ;
        CreateMap<BreakReasonResource, BreakReason>()
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;
        CreateMap<History, HistoryResource>()
          ;
        CreateMap<HistoryResource, History>()
          .ForMember(dest => dest.Client, opt => opt.Ignore())
          .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
          .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
          .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
          .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
          .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
          ;
        CreateMap<MacroType, MacroTypeResource>().ReverseMap();
        CreateMap<Klacks.Api.Domain.Models.Settings.Settings, SettingsResource>().ReverseMap();
        CreateMap<Vat, VatResource>().ReverseMap();
        CreateMap<CreateProviderCommand, LLMProvider>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Models, opt => opt.Ignore())
            .ForMember(dest => dest.ApiVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore());
        CreateMap<UpdateProviderCommand, LLMProvider>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProviderId, opt => opt.Ignore())
            .ForMember(dest => dest.ProviderName, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.Models, opt => opt.Ignore())
            .ForMember(dest => dest.ApiVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Settings, opt => opt.Ignore());
        CreateMap<LLMUsageRawData, LLMUsageResponse>()
            .ForMember(dest => dest.TotalTokens, opt => opt.MapFrom(src => src.Usages.Sum(u => u.TotalTokens)))
            .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
            .ForMember(dest => dest.ModelUsage, opt => opt.MapFrom(src => src.ModelSummary))
            .ForMember(dest => dest.DailyUsage, opt => opt.MapFrom(src => src.Usages
                .GroupBy(u => u.CreateTime.HasValue ? u.CreateTime.Value.Date : DateTime.UtcNow.Date)
                .Select(g => new DailyUsage
                {
                    Date = g.Key,
                    Tokens = g.Sum(u => u.TotalTokens),
                    Cost = g.Sum(u => u.Cost),
                    Requests = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList()));
        ConfigureDomainToCriteriaMappings();
        ConfigureDomainToResultMappings();
    }
    private void ConfigureDomainToCriteriaMappings()
    {
        CreateMap<BaseFilter, BaseCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder));
        CreateMap<FilterResource, ClientSearchCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder))
            .ForMember(dest => dest.SearchString, opt => opt.MapFrom(src => src.SearchString))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => 
                src.Male == true ? 1 : 
                src.Female == true ? 0 : 
                src.LegalEntity == true ? 3 : (int?)null))
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => 
                src.HomeAddress == true ? 1 : 
                src.CompanyAddress == true ? 2 : 
                src.InvoiceAddress == true ? 3 : (int?)null))
            .ForMember(dest => dest.HasAnnotation, opt => opt.MapFrom(src => src.HasAnnotation))
            .ForMember(dest => dest.IsActiveMember, opt => opt.MapFrom(src => src.ActiveMembership))
            .ForMember(dest => dest.IsFormerMember, opt => opt.MapFrom(src => src.FormerMembership))
            .ForMember(dest => dest.IsFutureMember, opt => opt.MapFrom(src => src.FutureMembership))
            .ForMember(dest => dest.MembershipStartDate, opt => opt.MapFrom(src => src.ScopeFrom.HasValue ? DateOnly.FromDateTime(src.ScopeFrom.Value) : (DateOnly?)null))
            .ForMember(dest => dest.MembershipEndDate, opt => opt.MapFrom(src => src.ScopeUntil.HasValue ? DateOnly.FromDateTime(src.ScopeUntil.Value) : (DateOnly?)null))
            .ForMember(dest => dest.StateOrCountryCode, opt => opt.Ignore()) 
            .ForMember(dest => dest.AnnotationText, opt => opt.Ignore()) 
            .ForMember(dest => dest.MembershipYear, opt => opt.Ignore()) 
            .ForMember(dest => dest.BreaksYear, opt => opt.Ignore()); 
        CreateMap<Presentation.DTOs.Filter.GroupFilter, GroupSearchCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder))
            .ForMember(dest => dest.SearchString, opt => opt.MapFrom(src => src.SearchString))
            .ForMember(dest => dest.ActiveDateRange, opt => opt.MapFrom(src => src.ActiveDateRange))
            .ForMember(dest => dest.FormerDateRange, opt => opt.MapFrom(src => src.FormerDateRange))
            .ForMember(dest => dest.FutureDateRange, opt => opt.MapFrom(src => src.FutureDateRange))
            .ForMember(dest => dest.ValidFromDate, opt => opt.Ignore()) 
            .ForMember(dest => dest.ValidToDate, opt => opt.Ignore()) 
            .ForMember(dest => dest.ParentGroupId, opt => opt.Ignore()) 
            .ForMember(dest => dest.IncludeSubGroups, opt => opt.Ignore()) 
            .ForMember(dest => dest.MaxDepth, opt => opt.Ignore()) 
            .ForMember(dest => dest.HasMembers, opt => opt.Ignore()) 
            .ForMember(dest => dest.MinMemberCount, opt => opt.Ignore()) 
            .ForMember(dest => dest.MaxMemberCount, opt => opt.Ignore()); 
    }
    private void ConfigureDomainToResultMappings()
    {
        CreateMap<ClientSummary, ClientResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) 
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LastName)) 
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender ?? GenderEnum.Female))
            .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => ParseIdNumber(src.IdNumber)))
            .ForMember(dest => dest.Birthdate, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? src.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.Addresses, opt => opt.Ignore()) 
            .ForMember(dest => dest.Annotations, opt => opt.Ignore()) 
            .ForMember(dest => dest.Communications, opt => opt.Ignore()) 
            .ForMember(dest => dest.LegalEntity, opt => opt.MapFrom(src => src.Gender == GenderEnum.LegalEntity))
            .ForMember(dest => dest.MaidenName, opt => opt.Ignore()) 
            .ForMember(dest => dest.Membership, opt => opt.Ignore()) 
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(src => Guid.Empty)) 
            .ForMember(dest => dest.PasswortResetToken, opt => opt.Ignore()) 
            .ForMember(dest => dest.SecondName, opt => opt.Ignore()) 
            .ForMember(dest => dest.Title, opt => opt.Ignore()) 
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.Type, opt => opt.Ignore()) 
            .ForMember(dest => dest.Works, opt => opt.Ignore()); 
        CreateMap<GroupSummary, GroupResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) 
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore()) 
            .ForMember(dest => dest.Children, opt => opt.Ignore()) 
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.ParentId.HasValue ? Guid.NewGuid() : (Guid?)null))
            .ForMember(dest => dest.Root, opt => opt.Ignore()) 
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue));
        CreateMap<GroupSummary, Group>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) 
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore()) 
            .ForMember(dest => dest.Parent, opt => opt.Ignore()) 
            .ForMember(dest => dest.Root, opt => opt.Ignore()) 
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue))
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore()) 
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore()) 
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore()) 
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore()) 
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime));
        CreateMap<PagedResult<ClientSummary>, BaseTruncatedResult>()
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));
        CreateMap<PagedResult<GroupSummary>, BaseTruncatedResult>()
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));
        CreateMap<PagedResult<GroupSummary>, TruncatedGroup>()
            .ForMember(dest => dest.Groups, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => (src.PageNumber - 1) * src.PageSize + 1));

        CreateMap<FilterResource, ClientFilter>()
            .ForMember(dest => dest.FilteredCantons, opt => opt.MapFrom(src => 
                src.List.Select(t => t.State).ToList()))
            .ForMember(dest => dest.Countries, opt => opt.MapFrom(src => src.Countries.Select(c => c.Abbreviation).ToList()))
            .ForMember(dest => dest.FilteredStateToken, opt => opt.MapFrom(src => 
                src.List.Select(t => new StateCountryFilter 
                { 
                    Id = t.Id, 
                    Country = t.Country, 
                    State = t.State, 
                    Select = t.Select 
                }).ToList()));

        CreateMap<StateCountryToken, StateCountryFilter>();

        CreateMap<Presentation.DTOs.Filter.BreakFilter, Domain.Models.Filters.BreakFilter>()
            .ForMember(dest => dest.AbsenceIds, opt => opt.MapFrom(src => src.Absences.Where(a => a.Checked).Select(a => a.Id).ToList()));

        CreateMap<Presentation.DTOs.Filter.WorkFilter, Domain.Models.Filters.WorkFilter>();
        CreateMap<Presentation.DTOs.Filter.GroupFilter, Domain.Models.Filters.GroupFilter>();
        CreateMap<Presentation.DTOs.Filter.AbsenceFilter, Domain.Models.Filters.AbsenceFilter>();

        CreateMap<BaseFilter, PaginationParams>()
            .ForMember(dest => dest.PageIndex, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.NumberOfItemsPerPage > 0 ? src.NumberOfItemsPerPage : 20))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.IsDescending, opt => opt.MapFrom(src => src.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)));

        // Response Mappings - Domain Models to Presentation DTOs
        CreateMap<LastChangeMetaData, LastChangeMetaDataResource>()
            .ForMember(dest => dest.Autor, opt => opt.MapFrom(src => src.Author));

        CreateMap<Domain.Models.Results.PagedResult<Client>, TruncatedClient>()
            .ForMember(dest => dest.Clients, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.MaxItems, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.MaxPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.CurrentPage, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.FirstItemOnPage, opt => opt.MapFrom(src => src.Items.Count() == 0 ? -1 : src.PageNumber * src.PageSize))
            .ForMember(dest => dest.Editor, opt => opt.Ignore())
            .ForMember(dest => dest.LastChange, opt => opt.Ignore());
    }
    private static int ParseIdNumber(string idNumber)
    {
        return int.TryParse(idNumber, out var id) ? id : 0;
    }
}