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
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Histories;
using Klacks.Api.Presentation.DTOs.Registrations;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.AutoMapper;

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
          .ForMember(dest => dest.CalendarSelectionId, opt => opt.MapFrom(src => src.CalendarSelection != null ? src.CalendarSelection.Id : (Guid?)null))
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
          .ForMember(dest => dest.GroupItems, opt => opt.MapFrom(src => src.GroupItems)) // Map GroupItems
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
          .ForMember(dest => dest.Client, opt => opt.MapFrom(src => src.Client != null ? src.Client : null)) // Explicitly map Employee
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

        // Missing Mappings - BreakReason
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

        // Missing Mappings - History
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

        // Missing Mappings - MacroType (No BaseEntity)
        CreateMap<MacroType, MacroTypeResource>().ReverseMap();

        // Missing Mappings - Settings (No BaseEntity)
        CreateMap<Klacks.Api.Domain.Models.Settings.Settings, SettingsResource>().ReverseMap();

        // Missing Mappings - Vat (No BaseEntity)
        CreateMap<Vat, VatResource>().ReverseMap();

        // Phase 3: Clean Architecture Mappings - Domain Models to Criteria and Results
        ConfigureDomainToCriteriaMappings();
        ConfigureDomainToResultMappings();
    }

    /// <summary>
    /// Phase 3: Configure Presentation Filter DTOs to Domain Criteria mappings
    /// </summary>
    private void ConfigureDomainToCriteriaMappings()
    {
        // Base Filter to Base Criteria mapping
        CreateMap<BaseFilter, BaseCriteria>()
            .ForMember(dest => dest.FirstItemOnLastPage, opt => opt.MapFrom(src => src.FirstItemOnLastPage))
            .ForMember(dest => dest.IsNextPage, opt => opt.MapFrom(src => src.IsNextPage))
            .ForMember(dest => dest.IsPreviousPage, opt => opt.MapFrom(src => src.IsPreviousPage))
            .ForMember(dest => dest.NumberOfItemsPerPage, opt => opt.MapFrom(src => src.NumberOfItemsPerPage))
            .ForMember(dest => dest.OrderBy, opt => opt.MapFrom(src => src.OrderBy))
            .ForMember(dest => dest.RequiredPage, opt => opt.MapFrom(src => src.RequiredPage))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder));

        // Filter Resource to Employee Search Criteria
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
            .ForMember(dest => dest.StateOrCountryCode, opt => opt.Ignore()) // Not in FilterResource, or map if available
            .ForMember(dest => dest.AnnotationText, opt => opt.Ignore()) // Not in FilterResource, or map if available
            .ForMember(dest => dest.MembershipYear, opt => opt.Ignore()) // Not in FilterResource, or map if available
            .ForMember(dest => dest.BreaksYear, opt => opt.Ignore()); // Not in FilterResource, or map if available

        // Group Filter to Group Search Criteria
        CreateMap<GroupFilter, GroupSearchCriteria>()
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
            .ForMember(dest => dest.ValidFromDate, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.ValidToDate, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.ParentGroupId, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.IncludeSubGroups, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.MaxDepth, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.HasMembers, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.MinMemberCount, opt => opt.Ignore()) // Not in GroupFilter, or map if available
            .ForMember(dest => dest.MaxMemberCount, opt => opt.Ignore()); // Not in GroupFilter, or map if available
    }

    /// <summary>
    /// Phase 3: Configure Domain Summary Results to Presentation DTO mappings
    /// </summary>
    private void ConfigureDomainToResultMappings()
    {
        // Domain Summary to Presentation Resource mappings
        CreateMap<ClientSummary, ClientResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) // Convert int to Guid
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LastName)) // Map LastName to Name field
            .ForMember(dest => dest.Company, opt => opt.MapFrom(src => src.Company))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender ?? GenderEnum.Female))
            .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => ParseIdNumber(src.IdNumber)))
            .ForMember(dest => dest.Birthdate, opt => opt.MapFrom(src => src.DateOfBirth.HasValue ? src.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.Addresses, opt => opt.Ignore()) // Collections not available in ClientSummary
            .ForMember(dest => dest.Annotations, opt => opt.Ignore()) // Collections not available in ClientSummary
            .ForMember(dest => dest.Communications, opt => opt.Ignore()) // Collections not available in ClientSummary
            .ForMember(dest => dest.LegalEntity, opt => opt.MapFrom(src => src.Gender == GenderEnum.LegalEntity))
            .ForMember(dest => dest.MaidenName, opt => opt.Ignore()) // Not available in ClientSummary
            .ForMember(dest => dest.Membership, opt => opt.Ignore()) // Complex object not available in ClientSummary
            .ForMember(dest => dest.MembershipId, opt => opt.MapFrom(src => Guid.Empty)) // Default value
            .ForMember(dest => dest.PasswortResetToken, opt => opt.Ignore()) // Not available in ClientSummary
            .ForMember(dest => dest.SecondName, opt => opt.Ignore()) // Not available in ClientSummary
            .ForMember(dest => dest.Title, opt => opt.Ignore()) // Not available in ClientSummary
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.Type, opt => opt.Ignore()) // Not available in ClientSummary
            .ForMember(dest => dest.Works, opt => opt.Ignore()); // Collections not available in ClientSummary

        CreateMap<GroupSummary, GroupResource>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) // Convert int to Guid
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore()) // Collections not available in GroupSummary
            .ForMember(dest => dest.Children, opt => opt.Ignore()) // Collections not available in GroupSummary
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.ParentId.HasValue ? Guid.NewGuid() : (Guid?)null))
            .ForMember(dest => dest.Root, opt => opt.Ignore()) // Not available in GroupSummary
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue));

        // Add GroupSummary to Group mapping for TruncatedGroup compatibility
        CreateMap<GroupSummary, Group>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid())) // Convert int to Guid
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom.HasValue ? src.ValidFrom.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue))
            .ForMember(dest => dest.ValidUntil, opt => opt.MapFrom(src => src.ValidTo.HasValue ? src.ValidTo.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null))
            .ForMember(dest => dest.GroupItems, opt => opt.Ignore()) // Collections not available in GroupSummary
            .ForMember(dest => dest.Parent, opt => opt.Ignore()) // Complex object not available in GroupSummary
            .ForMember(dest => dest.Root, opt => opt.Ignore()) // Not available in GroupSummary
            .ForMember(dest => dest.Lft, opt => opt.MapFrom(src => src.LeftValue))
            .ForMember(dest => dest.Rgt, opt => opt.MapFrom(src => src.RightValue))
            .ForMember(dest => dest.CurrentUserCreated, opt => opt.Ignore()) // BaseEntity properties
            .ForMember(dest => dest.CurrentUserDeleted, opt => opt.Ignore()) // BaseEntity properties
            .ForMember(dest => dest.CurrentUserUpdated, opt => opt.Ignore()) // BaseEntity properties
            .ForMember(dest => dest.DeletedTime, opt => opt.Ignore()) // BaseEntity properties
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => !src.IsActive))
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime));

        // Domain PagedResult to Presentation mappings
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
    }
    
    /// <summary>
    /// Helper method to parse string ID to int (needed to avoid expression tree issues)
    /// </summary>
    private static int ParseIdNumber(string idNumber)
    {
        return int.TryParse(idNumber, out var id) ? id : 0;
    }
}