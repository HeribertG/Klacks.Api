// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Skills;
using Klacks.Api.Infrastructure.Scripting;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Repositories.Assistant;
using Klacks.Api.Domain.Services.Absences;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Infrastructure.Services.CalendarSelections;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Services.Clients;
using Klacks.Api.Domain.Services.ContainerTemplates;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Domain.Services.Settings;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ScheduleEntries;
using Klacks.Api.Infrastructure.Services.PeriodHours;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Services.Macros;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Services.Schedules;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Repositories.Associations;
using Klacks.Api.Infrastructure.Repositories.Authentification;
using Klacks.Api.Infrastructure.Repositories.CalendarSelections;
using Klacks.Api.Infrastructure.Repositories.Reports;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Klacks.Api.Infrastructure.Repositories.Scheduling;
using Klacks.Api.Infrastructure.Repositories.Settings;
using Klacks.Api.Infrastructure.Repositories.Staffs;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.Groups;
using Klacks.Api.Infrastructure.Services.Settings;
using Klacks.Api.Infrastructure.Services.Shifts;
using Klacks.Api.Application.Services.Authentication;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Application.Services.Translation;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddRepositories();
        services.AddDomainServices();
        services.AddAuthenticationServices();
        services.AddAssistantServices();
        services.AddInfrastructureServices();
        return services;
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientFilterRepository, ClientFilterRepository>();
        services.AddScoped<IClientBreakPlaceholderRepository, ClientBreakPlaceholderRepository>();
        services.AddScoped<IClientSearchRepository, ClientSearchRepository>();
        services.AddScoped<IClientSearchFilterService, ClientSearchFilterService>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAnnotationRepository, AnnotationRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IAbsenceRepository, AbsenceRepository>();
        services.AddScoped<IBreakPlaceholderRepository, BreakPlaceholderRepository>();
        services.AddScoped<IAbsenceDetailRepository, AbsenceDetailRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ICalendarSelectionRepository, CalendarSelectionRepository>();
        services.AddScoped<ISelectedCalendarRepository, SelectedCalendarRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBreakRepository, BreakRepository>();
        services.AddScoped<IWorkChangeRepository, WorkChangeRepository>();
        services.AddScoped<IExpensesRepository, ExpensesRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IAssignedGroupRepository, AssignedGroupRepository>();
        services.AddScoped<IGroupItemRepository, GroupItemRepository>();
        services.AddScoped<IGroupVisibilityRepository, GroupVisibilityRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<ISchedulingRuleRepository, SchedulingRuleRepository>();
        services.AddScoped<IClientImageRepository, ClientImageRepository>();
        services.AddScoped<IContainerTemplateRepository, ContainerTemplateRepository>();
        services.AddScoped<IPostcodeChRepository, PostcodeChRepository>();
        services.AddScoped<IIdentityProviderRepository, IdentityProviderRepository>();
        services.AddScoped<IIdentityProviderSyncLogRepository, IdentityProviderSyncLogRepository>();
        services.AddScoped<IShiftScheduleRepository, ShiftScheduleRepository>();
        services.AddScoped<IReportTemplateRepository, ReportTemplateRepository>();
        services.AddScoped<IAiMemoryRepository, AiMemoryRepository>();
        services.AddScoped<IAiSoulRepository, AiSoulRepository>();
        services.AddScoped<IAiGuidelinesRepository, AiGuidelinesRepository>();
        services.AddScoped<ILlmFunctionDefinitionRepository, LlmFunctionDefinitionRepository>();
        services.AddScoped<IHeartbeatConfigRepository, HeartbeatConfigRepository>();
        services.AddScoped<ILLMRepository, LLMRepository>();
        services.AddScoped<ISkillUsageRepository, SkillUsageRepository>();
        services.AddScoped<IAgentRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentRepository>();
        services.AddScoped<IAgentSoulRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSoulRepository>();
        services.AddScoped<IAgentMemoryRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentMemoryRepository>();
        services.AddScoped<IAgentSessionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSessionRepository>();
        services.AddScoped<IAgentSkillRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSkillRepository>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IGetAllClientIdsFromGroupAndSubgroups, GroupClientService>();
        services.AddScoped<IGroupVisibilityService, GroupVisibilityService>();
        services.AddScoped<IHolidaysListCalculator, HolidaysListCalculator>();
        services.AddSingleton<IMacroEngine, MacroEngine>();
        services.AddSingleton<IMacroCache, MacroCache>();
        services.AddSingleton<IHolidayCalculatorCache, HolidayCalculatorCache>();
        services.AddScoped<IMacroDataProvider, MacroDataProvider>();

        services.AddShiftServices();
        services.AddClientServices();
        services.AddGroupServices();
        services.AddScheduleServices();
        services.AddSettingsServices();
    }

    private static void AddShiftServices(this IServiceCollection services)
    {
        services.AddScoped<IDateRangeFilterService, DateRangeFilterService>();
        services.AddScoped<IShiftSearchService, ShiftSearchService>();
        services.AddScoped<IShiftSortingService, ShiftSortingService>();
        services.AddScoped<IScheduleDateRangeService, ScheduleDateRangeService>();
        services.AddScoped<IShiftStatusFilterService, ShiftStatusFilterService>();
        services.AddScoped<IShiftFilterService, ShiftFilterService>();
        services.AddScoped<IShiftPaginationService, ShiftPaginationService>();
        services.AddScoped<IShiftValidator, ShiftValidator>();
        services.AddScoped<IShiftGroupManagementService, ShiftGroupManagementService>();
        services.AddScoped<IShiftTreeService, ShiftTreeService>();
        services.AddScoped<IShiftResetService, ShiftResetService>();
        services.AddScoped<IShiftCutFacade, ShiftCutFacade>();
        services.AddScoped<IShiftScheduleService, ShiftScheduleService>();
        services.AddScoped<IShiftGroupFilterService, ShiftGroupFilterService>();
        services.AddScoped<IShiftScheduleFilterService, ShiftScheduleFilterService>();
        services.AddScoped<IShiftScheduleSearchService, ShiftScheduleSearchService>();
        services.AddScoped<IShiftScheduleSortingService, ShiftScheduleSortingService>();
        services.AddScoped<IShiftScheduleTypeFilterService, ShiftScheduleTypeFilterService>();
    }

    private static void AddClientServices(this IServiceCollection services)
    {
        services.AddScoped<IClientFilterService, ClientFilterService>();
        services.AddScoped<IClientGroupFilterService, ClientGroupFilterService>();
        services.AddScoped<IClientMembershipFilterService, ClientMembershipFilterService>();
        services.AddScoped<IClientSearchService, ClientSearchService>();
        services.AddScoped<IClientSortingService, ClientSortingService>();
        services.AddScoped<IClientChangeTrackingService, ClientChangeTrackingService>();
        services.AddScoped<IClientEntityManagementService, ClientEntityManagementService>();
        services.AddScoped<IClientValidator, ClientValidator>();
        services.AddScoped<IClientWorkFilterService, ClientWorkFilterService>();
    }

    private static void AddGroupServices(this IServiceCollection services)
    {
        services.AddScoped<IGroupTreeService, GroupTreeService>();
        services.AddScoped<IGroupHierarchyService, GroupHierarchyService>();
        services.AddScoped<IGroupSearchService, GroupSearchService>();
        services.AddScoped<IGroupValidityService, GroupValidityService>();
        services.AddScoped<IGroupMembershipService, GroupMembershipService>();
        services.AddScoped<IGroupIntegrityService, GroupIntegrityService>();
        services.AddSingleton<IGroupCacheService, GroupCacheService>();
        services.AddScoped<Infrastructure.Services.Groups.Integrity.NestedSetRepairService>();
        services.AddScoped<Infrastructure.Services.Groups.Integrity.NestedSetValidationService>();
        services.AddScoped<Infrastructure.Services.Groups.Integrity.GroupIssueFindingService>();
        services.AddScoped<Infrastructure.Services.Groups.Integrity.RootIntegrityService>();
        services.AddScoped<IGroupServiceFacade, GroupServiceFacade>();
    }

    private static void AddScheduleServices(this IServiceCollection services)
    {
        services.AddScoped<IScheduleEntriesService, ScheduleEntriesService>();
        services.AddScoped<IWorkLockLevelService, WorkLockLevelService>();
        services.AddScoped<IPeriodHoursService, PeriodHoursService>();
        services.AddScoped<IScheduleChangeTracker, ScheduleChangeTracker>();
        services.AddScoped<IContainerAvailableTasksService, ContainerAvailableTasksService>();
        services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
        services.AddScoped<IAbsenceSortingService, AbsenceSortingService>();
        services.AddScoped<IAbsencePaginationService, AbsencePaginationService>();
        services.AddScoped<IAbsenceExportService, AbsenceExportService>();
        services.AddScoped<IWorkMacroService, WorkMacroService>();
        services.AddScoped<IBreakMacroService, BreakMacroService>();
        services.AddScoped<ICalendarSelectionUpdateService, CalendarSelectionUpdateService>();
        services.AddScoped<IScheduleCompletionService, ScheduleCompletionService>();
        services.AddScoped<ContainerTemplateService>();
        services.AddScoped<IWorkChangeResultService, WorkChangeResultService>();
    }

    private static void AddSettingsServices(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsEncryptionService, SettingsEncryptionService>();
        services.AddScoped<ICalendarRuleFilterService, CalendarRuleFilterService>();
        services.AddScoped<ICalendarRuleSortingService, CalendarRuleSortingService>();
        services.AddScoped<ICalendarRulePaginationService, CalendarRulePaginationService>();
        services.AddScoped<IMacroManagementService, MacroManagementService>();
        services.AddScoped<ISettingsTokenService, SettingsTokenService>();
        services.AddScoped<IEmailTestService, EmailTestService>();
        services.AddScoped<IScheduleEmailService, ScheduleEmailService>();
        services.AddScoped<IEmailService, EmailService>();
    }

    private static void AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<Application.Validation.Accounts.JwtValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRefreshTokenService, Services.Authentication.RefreshTokenService>();
        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<IAccountPasswordService, AccountPasswordService>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();
        services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>();
        services.AddScoped<ILdapService, Services.Identity.LdapService>();
        services.AddScoped<IOAuth2Service, Services.Identity.OAuth2Service>();
        services.AddScoped<IClientSyncService, ClientSyncService>();
    }

    private static void AddAssistantServices(this IServiceCollection services)
    {
        services.AddScoped<ILLMService, LLMService>();
        services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
        services.AddScoped<LLMProviderOrchestrator>();
        services.AddScoped<LLMConversationManager>();
        services.AddScoped<LLMFunctionExecutor>();
        services.AddScoped<LLMResponseBuilder>();
        services.AddScoped<LLMSystemPromptBuilder>();
        services.AddSingleton<IPromptTranslationProvider, PromptTranslationProvider>();
        services.AddScoped<IEmbeddingService, Klacks.Api.Infrastructure.Services.Assistant.EmbeddingService>();
        services.AddScoped<ContextAssemblyPipeline>();
        services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.EmbeddingBackgroundService>();
        services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.MemoryCleanupBackgroundService>();

        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI.OpenAIProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic.AnthropicProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini.GeminiProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure.AzureOpenAIProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral.MistralProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere.CohereProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek.DeepSeekProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic.GenericOpenAICompatibleProvider>();

        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI.OpenAIProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic.AnthropicProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini.GeminiProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure.AzureOpenAIProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral.MistralProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere.CohereProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek.DeepSeekProvider>();
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic.GenericOpenAICompatibleProvider>();

        services.AddSingleton<Domain.Services.Assistant.Skills.Adapters.ISkillAdapterFactory, Domain.Services.Assistant.Skills.Adapters.SkillAdapterFactory>();
        services.AddSingleton<ISkillRegistry, Domain.Services.Assistant.Skills.SkillRegistry>();
        services.AddScoped<ISkillExecutor, Domain.Services.Assistant.Skills.SkillExecutorService>();
        services.AddScoped<ISkillUsageTracker, SkillUsageTrackerService>();
        services.AddSingleton<SkillRegistrationService>();
        services.AddScoped<Domain.Services.Assistant.Skills.ILLMSkillBridge, Domain.Services.Assistant.Skills.LLMSkillBridge>();

        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetSystemInfoSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetCurrentTimeSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetUserContextSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.NavigateToSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.ValidateCalendarRuleSkill>();

        services.AddScoped<Application.Skills.CreateEmployeeSkill>();
        services.AddScoped<Application.Skills.SearchEmployeesSkill>();
        services.AddScoped<Application.Skills.CreateContractSkill>();
        services.AddScoped<Application.Skills.SearchAndNavigateSkill>();
        services.AddScoped<Application.Skills.GetClientDetailsSkill>();
        services.AddScoped<Application.Skills.AddClientToGroupSkill>();
        services.AddScoped<Application.Skills.AssignContractToClientSkill>();
        services.AddScoped<Application.Skills.ListContractsSkill>();
        services.AddScoped<Application.Skills.ListGroupsSkill>();
        services.AddScoped<Application.Skills.ValidateAddressSkill>();
        services.AddScoped<Application.Skills.GetUserPermissionsSkill>();
        services.AddScoped<Application.Skills.GetGeneralSettingsSkill>();
        services.AddScoped<Application.Skills.UpdateGeneralSettingsSkill>();
        services.AddScoped<Application.Skills.GetOwnerAddressSkill>();
        services.AddScoped<Application.Skills.UpdateOwnerAddressSkill>();
        services.AddScoped<Application.Skills.GetAiSoulSkill>();
        services.AddScoped<Application.Skills.UpdateAiSoulSkill>();
        services.AddScoped<Application.Skills.AddAiMemorySkill>();
        services.AddScoped<Application.Skills.GetAiMemoriesSkill>();
        services.AddScoped<Application.Skills.UpdateAiMemorySkill>();
        services.AddScoped<Application.Skills.DeleteAiMemorySkill>();
        services.AddScoped<Application.Skills.SetUserGroupScopeSkill>();
        services.AddScoped<Application.Skills.ConfigureHeartbeatSkill>();
    }

    private static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IFileUploadService, UploadFile>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<EntityCollectionUpdateService>();

        services.AddScoped<IGroupTreeDatabaseAdapter>(sp =>
        {
            var context = sp.GetRequiredService<DataBaseContext>();
            var isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            return isInMemory
                ? new Persistence.Adapters.GroupTreeInMemoryAdapter(context)
                : new Persistence.Adapters.GroupTreeProductionAdapter(context);
        });

        services.AddHttpClient("Nominatim");
        services.AddMemoryCache();
        services.AddSingleton<IGeocodingService, GeocodingService>();

        services.AddHttpClient<ITranslationService, Services.Translation.DeepLTranslationService>();
        services.AddScoped<IMultiLanguageTranslationService, MultiLanguageTranslationService>();

    }
}
