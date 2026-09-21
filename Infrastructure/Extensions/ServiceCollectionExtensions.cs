// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dependency injection registrations for application, infrastructure and domain event services.
/// </summary>

using System.Runtime.InteropServices;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Skills;
using Klacks.Api.Infrastructure.Scripting;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Staffs;
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
using Klacks.Api.Application.Common;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ScheduleEntries;
using Klacks.Api.Infrastructure.Services.AnalyseScenarios;
using Klacks.Api.Infrastructure.Services.PeriodHours;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Services.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Services.Macros;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Repositories.Associations;
using Klacks.Api.Infrastructure.Repositories.Email;
using Klacks.Api.Infrastructure.Repositories.Authentification;
using Klacks.Api.Infrastructure.Repositories.CalendarSelections;
using Klacks.Api.Infrastructure.Repositories.Imports;
using Klacks.Api.Infrastructure.Repositories.Reports;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Klacks.Api.Infrastructure.Repositories.Scheduling;
using Klacks.Api.Infrastructure.Repositories.Settings;
using Klacks.Api.Infrastructure.Repositories.Exports;
using Klacks.Api.Infrastructure.Repositories.Staffs;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.Groups;
using Klacks.Api.Infrastructure.Services.Settings;
using Klacks.Api.Infrastructure.Services.Shifts;
using Klacks.Api.Application.Services.Authentication;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Translation;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Infrastructure.Services.Associations;
using Klacks.Api.Infrastructure.Services.Plugins;
using Klacks.Api.Infrastructure.Services.ClientAvailabilitySchedule;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Skills.Generated;
using Klacks.Api.Application.Skills.Generic;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Application.Services;
using Klacks.Api.KnowledgeIndex.Infrastructure.Api;
using Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;
using Klacks.Api.KnowledgeIndex.Infrastructure.Persistence;
using Klacks.Api.KnowledgeIndex.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRepositories();
        services.AddDomainServices(configuration);
        services.AddAuthenticationServices();
        services.AddAssistantServices(configuration);
        services.AddInfrastructureServices();
        services.AddFeaturePluginServices(configuration);
        services.AddDomainEventServices();
        return services;
    }

    private static void AddDomainEventServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventDispatcher, Klacks.Api.Infrastructure.Events.DomainEventDispatcher>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.PeriodClosedEvent>, Klacks.Api.Infrastructure.Events.Handlers.PayrollExportOnPeriodClosedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.PeriodClosedEvent>, Klacks.Api.Infrastructure.Events.Handlers.WizardRunCaptureMeasurementOnPeriodClosedHandler>();
        services.AddScoped<Klacks.Api.Application.Interfaces.ISurchargeRecalculationScope, Klacks.Api.Infrastructure.Services.Schedules.SurchargeRecalculationScopeService>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.ContractChangedEvent>, Klacks.Api.Infrastructure.Events.Handlers.ThoroughRecalculationOnContractChangedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.SchedulingRuleChangedEvent>, Klacks.Api.Infrastructure.Events.Handlers.ThoroughRecalculationOnSchedulingRuleChangedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.SchedulingRuleRateRevisionsImportedEvent>, Klacks.Api.Infrastructure.Events.Handlers.ThoroughRecalculationOnRateRevisionsImportedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.SurchargeSettingsChangedEvent>, Klacks.Api.Infrastructure.Events.Handlers.ThoroughRecalculationOnSurchargeSettingsChangedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.MonthlyTargetHoursChangedEvent>, Klacks.Api.Infrastructure.Events.Handlers.ThoroughRecalculationOnMonthlyTargetHoursChangedHandler>();
        services.AddScoped<Klacks.Api.Domain.Events.IDomainEventHandler<Klacks.Api.Domain.Events.ActiveIndustriesChangedEvent>, Klacks.Api.Infrastructure.Events.Handlers.RevalidationOnActiveIndustriesChangedHandler>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Scheduling.IIndustryMigrationReader, Klacks.Api.Infrastructure.Repositories.Scheduling.IndustryMigrationReader>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.IHolidayWorkExemptionRuleRepository, Klacks.Api.Infrastructure.Repositories.Scheduling.HolidayWorkExemptionRuleRepository>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IClientHolidayCalendarResolver, Klacks.Api.Infrastructure.Services.Schedules.ClientHolidayCalendarResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IHolidayWorkEvaluator, Klacks.Api.Application.Services.Schedules.HolidayWorkEvaluator>();
    }

    private static readonly List<Klacks.Plugin.Contracts.IPluginRegistrar> PluginRegistrars = [];
    private static readonly Lock PluginRegistrarsLock = new();

    private static void AddFeaturePluginServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Klacks.Plugin.Contracts.IPluginEventBus, Klacks.Api.Infrastructure.Plugins.PluginEventBus>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginUnitOfWork, Klacks.Api.Infrastructure.Plugins.PluginUnitOfWorkBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginSettingsReader, Klacks.Api.Infrastructure.Plugins.PluginSettingsReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginSettingsWriter, Klacks.Api.Infrastructure.Plugins.PluginSettingsWriterBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IClientGroupReader, Klacks.Api.Infrastructure.Plugins.ClientGroupReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IClientPhoneReader, Klacks.Api.Infrastructure.Plugins.ClientPhoneReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IClientIdNumberReader, Klacks.Api.Infrastructure.Plugins.ClientIdNumberReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IEmployeeClientReader, Klacks.Api.Infrastructure.Plugins.EmployeeClientReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IAppUserDirectoryReader, Klacks.Api.Infrastructure.Plugins.AppUserDirectoryReaderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginEmailSender, Klacks.Api.Infrastructure.Plugins.PluginEmailSenderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginStateChecker, Klacks.Api.Infrastructure.Plugins.PluginStateCheckerBridge>();
        services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Persistence.DataBaseContext>());

        var assemblies = PluginRegistrars.SelectMany(r => r.GetControllerAssemblies()).ToList();
        foreach (var registrar in GetPluginRegistrars())
        {
            registrar.RegisterServices(services, configuration);
        }

        var oldRegistrars = typeof(ServiceCollectionExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IFeaturePluginRegistrar).IsAssignableFrom(t))
            .Select(t => (IFeaturePluginRegistrar)Activator.CreateInstance(t)!)
            .ToList();

        foreach (var registrar in oldRegistrars)
        {
            registrar.RegisterServices(services, configuration);
        }
    }

    /// <summary>
    /// Registers every core [SkillImplementation] class in this assembly that is not already registered,
    /// as Scoped. Mirrors the registry scan (SkillRegistryInitializer) so the skill-name -> type map and
    /// the DI container cannot drift apart; skip-existing preserves explicit (e.g. Singleton) lifetimes.
    /// </summary>
    private static void AddScannedSkillImplementations(this IServiceCollection services)
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            var attr = System.Attribute.GetCustomAttribute(
                type, typeof(Klacks.Api.Domain.Attributes.SkillImplementationAttribute));
            if (attr == null)
            {
                continue;
            }

            var alreadyRegistered = false;
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == type)
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                services.AddScoped(type);
            }
        }
    }

    /// <summary>
    /// Registers a plugin exactly once for the lifetime of the process. The backing list is static, so a
    /// second host built in the same process (every integration-test fixture does this) would otherwise
    /// append a duplicate registrar and run its RegisterServices twice against one IServiceCollection --
    /// which makes single-shot registrations such as AddPolicy throw "already exists". Deduplication is by
    /// registrar type; the EF model configurer is registered here so it, too, is added only once.
    /// </summary>
    /// <param name="registrar">Plugin registrar to add; ignored when one of the same type is already present</param>
    public static void RegisterPlugin(Klacks.Plugin.Contracts.IPluginRegistrar registrar)
    {
        lock (PluginRegistrarsLock)
        {
            foreach (var existing in PluginRegistrars)
            {
                if (existing.GetType() == registrar.GetType())
                {
                    return;
                }
            }

            PluginRegistrars.Add(registrar);
            Klacks.Api.Infrastructure.Plugins.PluginModelRegistry.Register(registrar.ConfigureDbModel);
        }
    }

    public static IReadOnlyList<Klacks.Plugin.Contracts.IPluginRegistrar> GetPluginRegistrars()
    {
        lock (PluginRegistrarsLock)
        {
            return PluginRegistrars.ToArray();
        }
    }

    private static void AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<Application.Validation.Accounts.JwtValidator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Authentification.IUserDataEraser,
            Klacks.Api.Infrastructure.Services.UserDataEraser>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Authentification.IUserDataErasureParticipant,
            Klacks.Api.Infrastructure.Plugins.MessagingPluginUserContactEraser>();
        services.AddScoped<IRefreshTokenService, Services.Authentication.RefreshTokenService>();
        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<IAccountPasswordService, AccountPasswordService>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Authentification.IAdminSetupGateService,
            Klacks.Api.Application.Services.Authentication.AdminSetupGateService>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();
        services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>();
        services.AddScoped<ILdapService, Services.Identity.LdapService>();
        services.AddScoped<IOAuth2Service, Services.Identity.OAuth2Service>();
        services.AddScoped<IOAuth2StateStore, Services.Identity.OAuth2StateStore>();
        services.AddScoped<IClientSyncService, ClientSyncService>();
        services.AddScoped<IClientAddressService, ClientAddressService>();
    }

    private static void AddAssistantServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AutoMemoryOptions>(configuration.GetSection(AutoMemoryOptions.SectionName));
        services.AddLLMCoreServices();
        services.AddLLMProviders();
        services.AddSkillServices();
        services.AddKnowledgeIndexServices(configuration);
        services.AddAssistantBackgroundServices(configuration);
    }

    private static void AddSkillServices(this IServiceCollection services)
    {
        services.AddSingleton<ISkillCacheService, SkillCacheService>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Adapters.ISkillAdapterFactory, Domain.Services.Assistant.Skills.Adapters.SkillAdapterFactory>();
        services.AddSingleton<ISkillRegistry, Domain.Services.Assistant.Skills.SkillRegistry>();
        services.AddScoped<ISkillExecutor, Domain.Services.Assistant.Skills.SkillExecutorService>();
        services.AddScoped<ISkillUsageTracker, SkillUsageTrackerService>();
        services.AddScoped<Application.Interfaces.IGroupScopeGuard, Application.Services.Grouping.GroupScopeGuard>();
        services.AddScoped<Persistence.Seed.GlobalAgentRuleSeedService>();
        services.AddScoped<Domain.Services.Assistant.Skills.ILLMSkillBridge, Domain.Services.Assistant.Skills.LLMSkillBridge>();

        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetSystemInfoSkill>();
        services.AddScoped<Domain.Services.Assistant.Skills.Implementations.GetCurrentTimeSkill>();
        services.AddScoped<Domain.Services.Assistant.Skills.Implementations.GetUserContextSkill>();
        services.AddScoped<Domain.Services.Assistant.Skills.Implementations.NavigateToSkill>();
        services.AddScoped<Domain.Services.Assistant.Skills.Implementations.ValidateCalendarRuleSkill>();

        services.AddScoped<Application.Skills.CreateEmployeeSkill>();
        services.AddScoped<Application.Skills.AddClientPhoneSkill>();
        services.AddScoped<Application.Skills.AddClientEmailSkill>();
        services.AddScoped<Application.Skills.AddClientNoteSkill>();
        services.AddScoped<Application.Skills.AddClientToGroupByNameSkill>();
        services.AddScoped<Application.Skills.RemoveClientFromGroupSkill>();
        services.AddScoped<Application.Skills.UpdateClientBirthdateSkill>();
        services.AddScoped<Application.Skills.UpdateClientGenderSkill>();
        services.AddScoped<Application.Skills.AssignContractByNameSkill>();
        services.AddScoped<Application.Skills.SearchEmployeesSkill>();
        services.AddScoped<Application.Skills.SearchAndNavigateSkill>();
        services.AddScoped<Application.Skills.GetClientDetailsSkill>();
        services.AddScoped<Application.Skills.AddClientToGroupSkill>();
        services.AddScoped<Application.Skills.AssignContractToClientSkill>();
        services.AddScoped<Application.Skills.ListContractsSkill>();
        services.AddScoped<Application.Skills.ListGroupsSkill>();
        services.AddScoped<Application.Skills.ValidateAddressSkill>();
        services.AddScoped<Application.Skills.GetUserPermissionsSkill>();
        services.AddScoped<Application.Skills.GetGeneralSettingsSkill>();
        services.AddScoped<Application.Skills.GetAiSoulSkill>();
        services.AddScoped<Application.Skills.UpdateAiSoulSkill>();
        services.AddScoped<Application.Skills.AddAiMemorySkill>();
        services.AddScoped<Application.Skills.GetAiMemoriesSkill>();
        services.AddScoped<Application.Skills.UpdateAiMemorySkill>();
        services.AddScoped<Application.Skills.DeleteAiMemorySkill>();
        services.AddScoped<Application.Skills.SetUserGroupScopeSkill>();
        services.AddScoped<Application.Skills.GetAiGuidelinesSkill>();
        services.AddScoped<Application.Skills.UpdateAiGuidelinesSkill>();
        services.AddScoped<Application.Skills.GetPageControlsSkill>();
        services.AddGeneratedSettingsSkills();
        services.AddScoped<Application.Skills.WebSearchSkill>();
        services.AddScoped<Application.Skills.TestSmtpConnectionSkill>();
        services.AddScoped<Application.Skills.TestImapConnectionSkill>();
        services.AddScoped<Application.Skills.UpdateWebSearchSettingsSkill>();
        services.AddScoped<Application.Skills.UpdateSpamFilterSettingsSkill>();
        services.AddScoped<Application.Skills.UpdateOwnerLocaleSettingsSkill>();
        services.AddScoped<Application.Skills.ListAbsenceTypesSkill>();
        services.AddScoped<Application.Skills.SearchClientAbsencesSkill>();
        services.AddScoped<Application.Skills.CreateShiftSkill>();
        services.AddScoped<Application.Skills.CreateTestEnvironmentSkill>();
        services.AddScoped<Application.Skills.Meta.ListAgentSkillsSkill>();
        services.AddScoped<Application.Skills.Meta.CreateAgentSkillSkill>();
        services.AddScoped<Application.Skills.Meta.UpdateAgentSkillSkill>();
        services.AddScoped<Application.Skills.Meta.DeleteAgentSkillSkill>();

        // Auto-register every [SkillImplementation] class that is not already registered above. The
        // skill registry (SkillRegistryInitializer) reflects over the SAME attribute to map skill-name
        // -> ImplementationType, and the executor resolves that type from DI; if the two drift apart a
        // skill throws "No service for type X" at first invocation (which is how the macro/planner
        // skills were silently broken — never live-run). Skip-existing preserves the explicit
        // (e.g. Singleton) lifetimes above; new skills no longer need a manual line here.
        services.AddScannedSkillImplementations();

        services.AddScoped<Application.Interfaces.IWebSearchProviderFactory, Infrastructure.WebSearch.WebSearchProviderFactory>();

        services.AddScoped<Persistence.Seed.AgentSoulSectionSeedService>();
        services.AddScoped<Persistence.Seed.UiControlSeedService>();
        services.AddScoped<Persistence.Seed.EmailFolderSeedService>();
        services.AddScoped<Persistence.Seed.SkillSeedLoader>();
        services.AddScoped<Persistence.Seed.SkillRelationSeedLoader>();
        services.AddScoped<Persistence.Seed.TurnGoldsetGoldenCaseSeedLoader>();
        services.AddScoped<Persistence.Seed.RecipeSeedLoader>();
        services.AddScoped<Persistence.Seed.SentimentKeywordSeedService>();
        services.AddScoped<Persistence.Seed.NavigationTargetSynonymSeedService>();
        services.AddScoped<Persistence.Seed.KlacksyKnowledgeMemorySeed>();
        services.AddScoped<Persistence.Seed.ClientPhoneticBackfillSeed>();
        services.AddScoped<Application.Services.Assistant.SkillRegistryInitializer>();
        services.AddScoped<Application.Services.Assistant.ISkillCatalogRefresher, Application.Services.Assistant.SkillCatalogRefresher>();
        services.AddScoped<ISubstratePriorDeriver, Application.Services.Assistant.SkillGraph.SubstratePriorDeriver>();
        services.AddScoped<ISkillRelationLearner, Application.Services.Assistant.SkillGraph.SkillRelationLearner>();
        services.AddScoped<ISkillRetrievalExpander, Application.Services.Assistant.SkillGraph.SkillRetrievalExpander>();
        services.AddScoped<ISkillSequenceSuggester, Application.Services.Assistant.SkillGraph.SkillSequenceSuggester>();
        services.AddScoped<ISkillSequenceProactiveNotifier, Application.Services.Assistant.SkillGraph.SkillSequenceProactiveNotifier>();
        services.AddSingleton<Infrastructure.Services.Assistant.SkillRelationLearningBackgroundService>();

        services.AddScoped<GenericListExecutor>();
        services.AddScoped<GenericDeleteExecutor>();
        services.AddScoped<KnowledgeHappenExecutor>();
        services.AddScoped<IGenericSkillDispatcher, GenericSkillDispatcher>();
    }

    private static void AddAssistantBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.Embedding && KnowledgeIndexServiceCollectionExtensions.IsOnnxRuntimeSupported(configuration))
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.EmbeddingBackgroundService>();

        if (bgOptions.MemoryCleanup)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.MemoryCleanupBackgroundService>();

        if (bgOptions.KlacksyLearning)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.SkillLearningBackgroundService>();

        services.AddScoped<ISkillLearningCaseCollector, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningCaseCollector>();
        services.AddScoped<ISkillLearningOptionsProvider, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningOptionsProvider>();
        services.AddScoped<ISkillLearningMaintenanceService, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningMaintenanceService>();
        services.AddScoped<ISkillRoutingOracle, Klacks.Api.Application.Services.Assistant.Learning.SkillRoutingOracle>();
        services.AddScoped<ILearnedArtifactGenerator, Klacks.Api.Application.Services.Assistant.Learning.LearnedArtifactGenerator>();
        services.AddScoped<IPhraseLearner, Klacks.Api.Application.Services.Assistant.Learning.PhraseLearner>();
        services.AddScoped<ISkillExecutionOracle, Klacks.Api.Application.Services.Assistant.Learning.SkillExecutionOracle>();
        services.AddScoped<IRecipeDraftValidator, Klacks.Api.Application.Services.Assistant.Learning.RecipeDraftValidator>();
        services.AddScoped<ICapabilityLearner, Klacks.Api.Application.Services.Assistant.Learning.CapabilityLearner>();
        services.AddScoped<ILearnedArtefactResolver, Klacks.Api.Application.Services.Assistant.Learning.LearnedArtefactResolver>();
        services.AddScoped<ISkillLearningFitnessService, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningFitnessService>();
        services.AddScoped<ISkillLearningPruner, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningPruner>();
        services.AddScoped<IGoldsetHoldoutReplayGate, Klacks.Api.Application.Services.Assistant.Learning.GoldsetHoldoutReplayGate>();
        services.AddScoped<ISkillDescriptionSharpener, Klacks.Api.Application.Services.Assistant.Learning.SkillDescriptionSharpener>();
        services.AddScoped<ISkillLearningLoop, Klacks.Api.Application.Services.Assistant.Learning.SkillLearningLoop>();

        // Singleton because the gate that keeps the six-hourly tick and the manual trigger from
        // overlapping only means anything if there is exactly one of it in the process.
        services.AddSingleton<ISkillLearningRunLauncher, Klacks.Api.Infrastructure.Services.Assistant.SkillLearningRunLauncher>();

        services.AddSingleton(new Klacks.Api.Domain.Models.Assistant.Grounding.AnswerGroundingOptions(
            configuration.GetValue<string>("Assistant:AnswerGroundingMode")
                ?? Klacks.Api.Domain.Constants.AnswerGroundingModes.Off));
        services.AddScoped<IAnswerGroundingEvaluator, Klacks.Api.Domain.Services.Assistant.Grounding.AnswerGroundingEvaluator>();
        services.AddScoped<IAnswerGroundingNameResolver, Application.Services.Assistant.AnswerGroundingNameResolver>();
        services.AddScoped<IAnswerGroundingSentinelProbe, Klacks.Api.Domain.Services.Assistant.Grounding.AnswerGroundingSentinelProbe>();

        if (bgOptions.AnswerGroundingSentinel)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.AnswerGroundingSentinelBackgroundService>();
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
        services.AddScoped<IAddressCoordinateWriter, AddressCoordinateWriter>();

        services.AddHttpClient<ITranslationService, Services.Translation.DeepLTranslationService>();
        services.AddScoped<IMultiLanguageTranslationService, MultiLanguageTranslationService>();

        services.AddHttpClient<Domain.Interfaces.Routing.IRoutingService, Services.Routing.OpenRouteServiceRoutingService>();

        services.AddScoped<Application.Interfaces.Exports.IShiftDescendantResolver, Services.Exports.ShiftDescendantResolver>();
        services.AddScoped<Application.Interfaces.Exports.IOrderExportDataLoader, Services.Exports.OrderExportDataLoader>();
        services.AddScoped<Application.Interfaces.Exports.IReportXlsxBuilder, Services.Exports.ReportXlsxBuilder>();
        services.AddScoped<Application.Interfaces.Exports.IClientPeriodExportDataLoader, Services.Exports.ClientPeriodExportDataLoader>();
        services.AddScoped<Domain.Interfaces.Exports.IClientPeriodExportFormatter, Services.Exports.ClientPeriodXmlExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IClientPeriodExportFormatter, Services.Exports.ClientPeriodCsvExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IClientPeriodExportFormatter, Services.Exports.ClientPeriodJsonExportFormatter>();
        services.AddScoped<Application.Interfaces.Exports.IPayrollExportDataLoader, Services.Exports.PayrollExportDataLoader>();
        services.AddScoped<Application.Interfaces.Exports.IPayrollExportConfigRepository, Repositories.Exports.PayrollExportConfigRepository>();
        services.AddScoped<Application.Interfaces.Exports.ISealedOrderListLoader, Services.Exports.SealedOrderListLoader>();
        services.AddScoped<Application.Interfaces.Exports.ISealedOrderIdLoader, Services.Exports.SealedOrderIdLoader>();
        services.AddScoped<Application.Interfaces.Exports.ISealedOrderDetailsLoader, Services.Exports.SealedOrderDetailsLoader>();
        services.AddScoped<Application.Interfaces.Exports.IPeriodClosedEntryFilter, Services.Exports.PeriodClosedEntryFilter>();
        services.AddScoped<Application.Interfaces.Exports.ICompanyInfoLoader, Services.Exports.CompanyInfoLoader>();
        services.AddScoped<Application.Interfaces.Exports.IExportFormatPolicy, Services.Exports.ExportFormatPolicy>();
        services.AddScoped<Application.Interfaces.Exports.IExportFormatOverrideRepository, Repositories.Exports.ExportFormatOverrideRepository>();
        services.AddScoped<Application.Interfaces.Exports.IExportFormatOverrideApplier, Application.Services.Exports.ExportFormatOverrideApplier>();
        services.AddScoped<Application.Interfaces.Exports.IExportFormatFamilyResolver, Application.Services.Exports.ExportFormatFamilyResolver>();
        services.AddScoped<Application.Interfaces.PeriodClosing.IPeriodValidationLoader, Services.PeriodClosing.PeriodValidationLoader>();
        services.AddScoped<Application.Interfaces.Schedules.IPreCommitConflictChecker, Services.Schedules.PreCommitConflictChecker>();
        services.AddScoped<Application.Interfaces.Schedules.IWorkWriteGuard, Application.Services.Schedules.WorkWriteGuardService>();
        services.AddScoped<Application.Interfaces.Schedules.IWorkRestoreAuthorizer, Application.Services.Schedules.WorkRestoreAuthorizer>();
        services.AddScoped<Application.Interfaces.Schedules.IComplianceEscalationService, Application.Services.Schedules.ComplianceEscalationService>();
        services.AddScoped<Application.Interfaces.Schedules.ICompliancePartitionService, Application.Services.Schedules.CompliancePartitionService>();
        services.AddScoped<Application.Interfaces.Schedules.IScenarioComplianceService, Application.Services.Schedules.ScenarioComplianceService>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.CsvExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.JsonExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.XmlExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.DatevExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.BmdExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.MoveinIlExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.OmegaSkExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.Sie4bSeExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.TemeljnicaHrSiExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IExportFormatter, Services.Exports.ZohoBooksAeExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.DatevLugBewegungsdatenFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.GenericDelimitedPayrollExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.GenericXlsxPayrollExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.MeritPalkEeExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.PaxmlSeExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.AbaConnectChExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.PohodaCzExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.WinmentorRoExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.BrightpayIeUkExportFormatter>();
        services.AddScoped<Domain.Interfaces.Exports.IPayrollExportFormatter, Services.Exports.LogoBordroTrExportFormatter>();
        services.AddScoped<Domain.Interfaces.Imports.IOrderImportParser, Services.Imports.XmlOrderImportParser>();
    }
}
