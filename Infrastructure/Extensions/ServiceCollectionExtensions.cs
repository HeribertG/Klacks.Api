using Klacks.Api.Application.AutoMapper;
using Klacks.Api.BasicScriptInterpreter;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Repositories;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.PdfExports;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Domain.Services.Absences;
using Klacks.Api.Infrastructure.Services.Authentication;
using Klacks.Api.Application.Services;

namespace Klacks.Api.Infrastructure.Extensions;

public static  class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

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
        services.AddScoped<IContractRepository, ContractRepository>();

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

        // Authentication Domain Services
        services.AddScoped<Klacks.Api.Application.Validation.Accounts.JwtValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<IAccountRepository, AccountRepository>();

        // Phase 2: Domain Repository Implementations (TODO: Implement with correct Domain Model mappings)

        // Phase 3: Application Services (Clean Architecture)
        services.AddScoped<ClientApplicationService>();
        services.AddScoped<GroupApplicationService>();
        services.AddScoped<AccountApplicationService>();
        services.AddScoped<AbsenceApplicationService>();
        services.AddScoped<SettingsApplicationService>();

        // Phase 3: AutoMapper Configuration (erweitert bestehende MappingProfile)

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
