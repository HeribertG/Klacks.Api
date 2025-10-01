using Klacks.Api.Application.Interfaces;
using Klacks.Api.BasicScriptInterpreter;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.Absences;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Domain.Services.CalendarSelections;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Domain.Services.Settings;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.LLM;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.Authentication;
using ClientGroupFilterService = Klacks.Api.Infrastructure.Services.ClientGroupFilterService;
using ClientSearchFilterService = Klacks.Api.Infrastructure.Services.ClientSearchFilterService;

namespace Klacks.Api.Infrastructure.Extensions;

public static  class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientFilterRepository, ClientFilterRepository>();
        services.AddScoped<IClientBreakRepository, ClientBreakRepository>();
        services.AddScoped<IClientWorkRepository, ClientWorkRepository>();
        services.AddScoped<IClientSearchRepository, ClientSearchRepository>();
        services.AddScoped<IClientGroupFilterService, ClientGroupFilterService>();
        services.AddScoped<IClientSearchFilterService, ClientSearchFilterService>();
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
        services.AddScoped<IShiftPaginationService, ShiftPaginationService>();
        services.AddScoped<IShiftGroupManagementService, ShiftGroupManagementService>();

        // Employee Domain Services
        services.AddScoped<IClientFilterService, ClientFilterService>();
        services.AddScoped<IClientMembershipFilterService, ClientMembershipFilterService>();
        services.AddScoped<IClientSearchService, ClientSearchService>();
        services.AddScoped<IClientSortingService, ClientSortingService>();
        services.AddScoped<IClientChangeTrackingService, ClientChangeTrackingService>();
        services.AddScoped<IClientEntityManagementService, ClientEntityManagementService>();
        services.AddScoped<IClientWorkFilterService, ClientWorkFilterService>();

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
        services.AddScoped<IAbsenceExportService, AbsenceExportService>();

        // Settings Domain Services
        services.AddScoped<ICalendarRuleFilterService, CalendarRuleFilterService>();
        services.AddScoped<ICalendarRuleSortingService, CalendarRuleSortingService>();
        services.AddScoped<ICalendarRulePaginationService, CalendarRulePaginationService>();
        services.AddScoped<IMacroManagementService, MacroManagementService>();
        services.AddScoped<IMacroTypeManagementService, MacroTypeManagementService>();
        services.AddScoped<IVatManagementService, VatManagementService>();
        services.AddScoped<ISettingsTokenService, SettingsTokenService>();
        
        // Email Services  
        services.AddScoped<IEmailTestService, EmailTestService>();

        // CalendarSelection Domain Services
        services.AddScoped<ICalendarSelectionUpdateService, CalendarSelectionUpdateService>();

        // Authentication Domain Services
        services.AddScoped<Application.Validation.Accounts.JwtValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Account Domain Services
        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<IAccountPasswordService, AccountPasswordService>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();

        // Generic Services
        services.AddScoped(typeof(IGenericPaginationService<>), typeof(Domain.Services.Common.GenericPaginationService<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Infrastructure Services
        services.AddScoped<EntityCollectionUpdateService>();

        // Database Adapters - Register based on environment
        services.AddScoped<IGroupTreeDatabaseAdapter>(sp =>
        {
            var context = sp.GetRequiredService<DataBaseContext>();
            var isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            return isInMemory
                ? new Persistence.Adapters.GroupTreeInMemoryAdapter(context)
                : new Persistence.Adapters.GroupTreeProductionAdapter(context);
        });

        // LLM Services
        services.AddScoped<ILLMRepository, LLMRepository>();
        services.AddScoped<ILLMService, LLMService>();
        services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();

        // LLM Domain Services
        services.AddScoped<LLMProviderOrchestrator>();
        services.AddScoped<LLMConversationManager>();
        services.AddScoped<LLMFunctionExecutor>();
        services.AddScoped<LLMResponseBuilder>();
        services.AddScoped<LLMSystemPromptBuilder>();

        // LLM Providers
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.OpenAI.OpenAIProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.Anthropic.AnthropicProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.Gemini.GeminiProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.Azure.AzureOpenAIProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.Mistral.MistralProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.Cohere.CohereProvider>();
        services.AddScoped<Klacks.Api.Domain.Services.LLM.Providers.DeepSeek.DeepSeekProvider>();

        // HttpClients for Providers
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.OpenAI.OpenAIProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.Anthropic.AnthropicProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.Gemini.GeminiProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.Azure.AzureOpenAIProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.Mistral.MistralProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.Cohere.CohereProvider>();
        services.AddHttpClient<Klacks.Api.Domain.Services.LLM.Providers.DeepSeek.DeepSeekProvider>();

        return services;
    }
}
