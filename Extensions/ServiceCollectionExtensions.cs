using Klacks.Api.BasicScriptInterpreter;
using Klacks.Api.Datas;
using Klacks.Api.Helper;
using Klacks.Api.Interfaces;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Repositories;
using Klacks.Api.Services;
using Klacks.Api.Services.Exports;
using Klacks.Api.Services.Groups;
using Klacks.Api.Services.Holidays;
using Klacks.Api.Services.Shifts;
using Klacks.Api.Services.Clients;
using Klacks.Api.Services.Absences;

namespace Klacks.Api.Extensions;

public static  class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAnnotationRepository, AnnotationRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAbsenceRepository, AbsenceRepository>();
        services.AddScoped<IBreakRepository, BreakRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<ICalendarSelectionRepository, CalendarSelectionRepository>();
        services.AddScoped<ISelectedCalendarRepository, SelectedCalendarRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGanttPdfExportService, GanttPdfExportService>();
        services.AddScoped<IAssignedGroupRepository, AssignedGroupRepository>();
        services.AddScoped<IGroupVisibilityRepository, GroupVisibilityRepository>();

        services.AddSingleton<IMacroEngine, MacroEngine>();
        services.AddScoped<UploadFile>();

        services.AddScoped<IGetAllClientIdsFromGroupAndSubgroups, GroupClientService>();
        services.AddScoped<IGroupVisibilityService, GroupVisibilityService>();
        services.AddScoped<IHolidaysListCalculator, HolidaysListCalculator>();

        // Shift Domain Services
        services.AddScoped<IDateRangeFilterService, DateRangeFilterService>();
        services.AddScoped<IShiftSearchService, ShiftSearchService>();
        services.AddScoped<IShiftSortingService, ShiftSortingService>();
        services.AddScoped<IScheduleDateRangeService, ScheduleDateRangeService>();
        services.AddScoped<IShiftStatusFilterService, ShiftStatusFilterService>();
        services.AddScoped<IShiftFilterService, ShiftFilterService>();

        // Client Domain Services
        services.AddScoped<IClientFilterService, ClientFilterService>();
        services.AddScoped<IClientMembershipFilterService, ClientMembershipFilterService>();
        services.AddScoped<IClientSearchService, ClientSearchService>();
        services.AddScoped<IClientSortingService, ClientSortingService>();

        // Group Domain Services
        services.AddScoped<IGroupTreeService, GroupTreeService>();
        services.AddScoped<IGroupHierarchyService, GroupHierarchyService>();
        services.AddScoped<IGroupSearchService, GroupSearchService>();
        services.AddScoped<IGroupValidityService, GroupValidityService>();
        services.AddScoped<IGroupMembershipService, GroupMembershipService>();
        services.AddScoped<IGroupIntegrityService, GroupIntegrityService>();

        // Absence Domain Services
        services.AddScoped<IAbsenceSortingService, AbsenceSortingService>();
        services.AddScoped<IAbsencePaginationService, AbsencePaginationService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
