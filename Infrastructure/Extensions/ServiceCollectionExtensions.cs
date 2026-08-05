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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    }

    private static readonly List<Klacks.Plugin.Contracts.IPluginRegistrar> PluginRegistrars = [];

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
        services.AddScoped<Klacks.Plugin.Contracts.IPluginEmailSender, Klacks.Api.Infrastructure.Plugins.PluginEmailSenderBridge>();
        services.AddScoped<Klacks.Plugin.Contracts.IPluginStateChecker, Klacks.Api.Infrastructure.Plugins.PluginStateCheckerBridge>();
        services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Persistence.DataBaseContext>());

        var assemblies = PluginRegistrars.SelectMany(r => r.GetControllerAssemblies()).ToList();
        foreach (var registrar in PluginRegistrars)
        {
            registrar.RegisterServices(services, configuration);
            Klacks.Api.Infrastructure.Plugins.PluginModelRegistry.Register(registrar.ConfigureDbModel);
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

    public static void RegisterPlugin(Klacks.Plugin.Contracts.IPluginRegistrar registrar)
    {
        PluginRegistrars.Add(registrar);
    }

    public static IReadOnlyList<Klacks.Plugin.Contracts.IPluginRegistrar> GetPluginRegistrars() => PluginRegistrars;

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientFilterRepository, ClientFilterRepository>();
        services.AddScoped<IClientBreakPlaceholderRepository, ClientBreakPlaceholderRepository>();
        services.AddScoped<IClientSearchRepository, ClientSearchRepository>();
        services.AddScoped<IClientFuzzySearchService, ClientFuzzySearchService>();
        services.AddScoped<IGroupSearchRepository, GroupSearchRepository>();
        services.AddScoped<IShiftSearchRepository, ShiftSearchRepository>();
        services.AddScoped<IClientSearchFilterService, ClientSearchFilterService>();
        services.AddScoped<IClientBaseQueryService, ClientBaseQueryService>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IAnnotationRepository, AnnotationRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISettingsReader>(sp => sp.GetRequiredService<ISettingsRepository>());
        services.AddScoped<IActiveIndustriesProvider, ActiveIndustriesProvider>();
        services.AddScoped<IAbsenceRepository, AbsenceRepository>();
        services.AddScoped<IBreakPlaceholderRepository, BreakPlaceholderRepository>();
        services.AddScoped<IAbsenceDetailRepository, AbsenceDetailRepository>();
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<ICountryResolver, Klacks.Api.Domain.Services.Common.CountryResolver>();
        services.AddScoped<Klacks.Api.Domain.Services.Common.StateAbbreviationResolver>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ICalendarSelectionRepository, CalendarSelectionRepository>();
        services.AddScoped<ISelectedCalendarRepository, SelectedCalendarRepository>();
        services.AddScoped<IWorkRepository, WorkRepository>();
        services.AddScoped<IBreakRepository, BreakRepository>();
        services.AddScoped<IWorkChangeRepository, WorkChangeRepository>();
        services.AddScoped<IExpensesRepository, ExpensesRepository>();
        services.AddScoped<IShiftExpensesRepository, ShiftExpensesRepository>();
        services.AddScoped<IScheduleNoteRepository, ScheduleNoteRepository>();
        services.AddScoped<IScheduleCommandRepository, ScheduleCommandRepository>();
        services.AddScoped<IAnalyseScenarioRepository, AnalyseScenarioRepository>();
        services.AddScoped<IWizardRunCaptureRepository, WizardRunCaptureRepository>();
        services.AddScoped<IWizardTrainingRepository, WizardTrainingRepository>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IScheduleSnapshotMarkerService,
                           Klacks.Api.Infrastructure.Services.Schedules.ScheduleSnapshotMarkerService>();
        services.AddScoped<IWizardRunCaptureMeasurementService, Klacks.Api.Infrastructure.Services.Schedules.WizardRunCaptureMeasurementService>();
        services.AddScoped<IAnalyseScenarioService, AnalyseScenarioService>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IContainerLockRepository, ContainerLockRepository>();
        services.AddScoped<IContainerWorkChildrenReadRepository, ContainerWorkChildrenReadRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IAssignedGroupRepository, AssignedGroupRepository>();
        services.AddScoped<IGroupItemRepository, GroupItemRepository>();
        services.AddScoped<IClientShiftPreferenceRepository, ClientShiftPreferenceRepository>();
        services.AddScoped<IClientQualificationRepository, ClientQualificationRepository>();
        services.AddScoped<IShiftRequiredQualificationRepository, ShiftRequiredQualificationRepository>();
        services.AddScoped<IQualificationRepository, QualificationRepository>();
        services.AddScoped<IGroupVisibilityRepository, GroupVisibilityRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<ISchedulingRuleRepository, SchedulingRuleRepository>();
        services.AddScoped<IClientAvailabilityRepository, ClientAvailabilityRepository>();
        services.AddScoped<IClientImageRepository, ClientImageRepository>();
        services.AddScoped<IContainerTemplateRepository, ContainerTemplateRepository>();
        services.AddScoped<IContainerShiftOverrideRepository, ContainerShiftOverrideRepository>();
        services.AddScoped<IIndividualPeriodRepository, IndividualPeriodRepository>();
        services.AddScoped<IMonthlyTargetHoursRepository, MonthlyTargetHoursRepository>();
        services.AddScoped<IPostcodeChRepository, PostcodeChRepository>();
        services.AddScoped<IIdentityProviderRepository, IdentityProviderRepository>();
        services.AddScoped<IIdentityProviderSyncLogRepository, IdentityProviderSyncLogRepository>();
        services.AddScoped<IErpDropPointRepository, ErpDropPointRepository>();
        services.AddScoped<Domain.Interfaces.Imports.IErpImportTokenRepository, Repositories.Imports.ErpImportTokenRepository>();
        services.AddErpObjectStorage();
        services.AddScoped<IPersonalAccessTokenRepository, PersonalAccessTokenRepository>();
        services.AddScoped<IOAuthClientRepository, OAuthClientRepository>();
        services.AddSingleton<IOAuthAuthorizationCodeStore, Klacks.Api.Infrastructure.Authentication.OAuthAuthorizationCodeStore>();
        services.AddScoped<IShiftScheduleRepository, ShiftScheduleRepository>();
        services.AddScoped<IReportTemplateRepository, ReportTemplateRepository>();
        services.AddScoped<IHeartbeatConfigRepository, HeartbeatConfigRepository>();
        services.AddScoped<ILLMRepository, LLMRepository>();
        services.AddScoped<ISkillUsageRepository, SkillUsageRepository>();
        services.AddScoped<IAgentRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentRepository>();
        services.AddScoped<IAgentSoulRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSoulRepository>();
        services.AddScoped<IAgentMemoryRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentMemoryRepository>();
        services.AddScoped<IPendingUserNoteRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingUserNoteRepository>();
        services.AddScoped<IAgentSessionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSessionRepository>();
        services.AddScoped<IAgentSkillRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSkillRepository>();
        services.AddScoped<IAgentRecipeRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentRecipeRepository>();
        services.AddScoped<IScheduledTaskRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ScheduledTaskRepository>();
        services.AddScoped<IScheduledTaskRunner, Klacks.Api.Application.Services.Assistant.Scheduling.ScheduledTaskRunner>();
        services.AddScoped<ISkillRelationRepository, Klacks.Api.Infrastructure.Repositories.Assistant.SkillRelationRepository>();
        services.AddScoped<ISkillPhraseRepository, Klacks.Api.Infrastructure.Repositories.Assistant.SkillPhraseRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IMemoryRelationRepository, Klacks.Api.Infrastructure.Repositories.Assistant.MemoryRelationRepository>();
        services.AddScoped<INavigationTargetSynonymRepository, Klacks.Api.Infrastructure.Repositories.Assistant.NavigationTargetSynonymRepository>();
        services.AddScoped<IGlobalAgentRuleRepository, Klacks.Api.Infrastructure.Repositories.Assistant.GlobalAgentRuleRepository>();
        services.AddScoped<IUiControlRepository, Klacks.Api.Infrastructure.Repositories.Assistant.UiControlRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Update.IUpdateHistoryRepository, Klacks.Api.Infrastructure.Repositories.Update.UpdateHistoryRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Update.IUpdateAvailabilityEvaluator, Klacks.Api.Application.Services.Update.UpdateAvailabilityEvaluator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Update.IUpdateManifestReader, Klacks.Api.Infrastructure.Services.Update.UpdateManifestReader>();
        services.AddHttpClient(Klacks.Api.Infrastructure.Services.Update.UpdateManifestReader.HttpClientName);
        services.AddScoped<ISkillGapRepository, Klacks.Api.Infrastructure.Repositories.Assistant.SkillGapRepository>();
        services.AddScoped<IAnswerGroundingRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AnswerGroundingRepository>();
        services.AddScoped<ISkillSelectionTrajectoryRepository, Klacks.Api.Infrastructure.Repositories.Assistant.SkillSelectionTrajectoryRepository>();
        services.AddScoped<IEvalRunRepository, Klacks.Api.Infrastructure.Repositories.Assistant.EvalRunRepository>();
        services.AddScoped<IProposedSkillChangeRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ProposedSkillChangeRepository>();
        services.AddScoped<ISkillDescriptionOptimizer, Klacks.Api.Application.Services.Assistant.Evaluation.SkillDescriptionOptimizer>();
        services.AddScoped<IReceivedEmailRepository, ReceivedEmailRepository>();
        services.AddScoped<IEmailQueryRepository, EmailQueryRepository>();
        services.AddScoped<IEmailFolderRepository, EmailFolderRepository>();
        services.AddScoped<ISpamRuleRepository, SpamRuleRepository>();
        services.AddScoped<ISentimentKeywordRepository, Klacks.Api.Infrastructure.Repositories.Assistant.SentimentKeywordRepository>();
        services.AddScoped<IPeriodAuditLogRepository, PeriodAuditLogRepository>();
        services.AddScoped<IExportLogRepository, ExportLogRepository>();
        services.AddScoped<IPeriodClosingReadRepository, PeriodClosingReadRepository>();
        services.AddScoped<IResourceMonitorReadRepository, Klacks.Api.Infrastructure.Repositories.Dashboard.ResourceMonitorReadRepository>();
        services.AddScoped<IShiftCoverageReadRepository, Klacks.Api.Infrastructure.Repositories.Dashboard.ShiftCoverageReadRepository>();
        services.AddScoped<ISealedDayRepository, SealedDayRepository>();
        services.AddScoped<IClientSortPreferenceRepository, ClientSortPreferenceRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Schedules.IScheduleCommandKeywordProvider,
                           Klacks.Api.Infrastructure.Services.Schedules.ScheduleCommandKeywordProvider>();
    }

    private static void AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGetAllClientIdsFromGroupAndSubgroups, GroupClientService>();
        services.AddScoped<IGroupVisibilityService, GroupVisibilityService>();
        services.AddScoped<IHolidaysListCalculator, HolidaysListCalculator>();
        services.AddScoped<IMacroEngine, MacroEngine>();
        services.AddSingleton<IMacroCache, MacroCache>();
        services.AddSingleton<IHolidayCalculatorCache, HolidayCalculatorCache>();
        services.AddScoped<IMacroDataProvider, MacroDataProvider>();
        services.AddScoped<IMacroCompilationService, MacroCompilationService>();
        services.AddScoped<IMacroScriptValidator, MacroScriptValidator>();
        services.AddScoped<IClientContractDataProvider, ClientContractDataProvider>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Schedules.IOvertimeSurchargeCalculator,
                           Klacks.Api.Infrastructure.Services.Schedules.OvertimeSurchargeCalculator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Schedules.IOvertimeConfigResolver,
                           Klacks.Api.Infrastructure.Services.Schedules.OvertimeConfigResolver>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Schedules.IClientWorkHoursProvider,
                           Klacks.Api.Infrastructure.Services.Schedules.ClientWorkHoursProvider>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.ISchedulingPolicyResolver,
                           Klacks.Api.Infrastructure.Services.Schedules.SchedulingPolicyResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IComplianceEnforcementResolver,
                           Klacks.Api.Infrastructure.Services.Schedules.ComplianceEnforcementResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.ISupervisorOverrideAuthorizer,
                           Klacks.Api.Infrastructure.Services.Schedules.SupervisorOverrideAuthorizer>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.IPeriodCapRuleRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.PeriodCapRuleRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.IRestDayRotationRuleRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.RestDayRotationRuleRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.ICounterRuleRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.CounterRuleRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.ICompanyRuleRepository,
                           Klacks.Api.Infrastructure.Repositories.Settings.CompanyRuleRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.ISchedulingRuleImportRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.SchedulingRuleImportRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.ISchedulingRuleRateRevisionImportRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.SchedulingRuleRateRevisionImportRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Staffs.IQualificationImportRepository,
                           Klacks.Api.Infrastructure.Repositories.Staffs.QualificationImportRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.IMacroImportRepository,
                           Klacks.Api.Infrastructure.Repositories.Settings.MacroImportRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.IClientMembershipStartResolver,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.ClientMembershipStartResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IPeriodCapEvaluator,
                           Klacks.Api.Application.Services.Schedules.PeriodCapEvaluator>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IRestDayRotationEvaluator,
                           Klacks.Api.Infrastructure.Services.Schedules.RestDayRotationEvaluator>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.ICounterRuleEvaluator,
                           Klacks.Api.Infrastructure.Services.Schedules.CounterRuleEvaluator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.IRestrictedTimeWindowRuleRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.RestrictedTimeWindowRuleRepository>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IRestrictedTimeWindowEvaluator,
                           Klacks.Api.Infrastructure.Services.Schedules.RestrictedTimeWindowEvaluator>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardRestrictedWindowBuilder,
                           Klacks.Api.Infrastructure.Services.Schedules.WizardRestrictedWindowBuilder>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Scheduling.ICompensatoryRestObligationRepository,
                           Klacks.Api.Infrastructure.Repositories.Scheduling.CompensatoryRestObligationRepository>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.ICompensatoryRestObligationReconciler,
                           Klacks.Api.Infrastructure.Services.Schedules.CompensatoryRestObligationReconciler>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.ICompensatoryRestEvaluator,
                           Klacks.Api.Infrastructure.Services.Schedules.CompensatoryRestEvaluator>();

        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardContextBuilder,
                           Klacks.Api.Application.Services.Schedules.WizardContextBuilder>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.WizardAgentSnapshotBuilder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardHardConstraintBuilder,
                           Klacks.Api.Infrastructure.Services.Schedules.WizardHardConstraintBuilder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardWarmStartBuilder,
                           Klacks.Api.Infrastructure.Services.Schedules.WizardWarmStartBuilder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardShiftBuilder,
                           Klacks.Api.Infrastructure.Services.Schedules.WizardShiftBuilder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IEligibilityMatrixBuilder,
                           Klacks.Api.Application.Services.Schedules.EligibilityMatrixBuilder>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.Recovery.IRecoverySnapshotBuilder,
                           Klacks.Api.Application.Services.Schedules.Recovery.RecoverySnapshotBuilder>();
        services.AddSingleton<Klacks.ScheduleRecovery.Engine.IRecoveryEngine,
                              Klacks.ScheduleRecovery.Engine.LocalRepairEngine>();

        services.AddSingleton<Klacks.Api.Application.Services.Schedules.WizardJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.WizardResultCache>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules
            .JobTerminalStateCache<Klacks.Api.Application.DTOs.Schedules.WizardJobResultDto>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.IWizardJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.WizardJobRunner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizardApplyService,
                           Klacks.Api.Infrastructure.Services.Schedules.WizardApplyService>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.IWizardBenchmarkService,
                              Klacks.Api.Infrastructure.Services.Schedules.WizardBenchmarkService>();

        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HarmonizerJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HarmonizerResultCache>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules
            .JobTerminalStateCache<Klacks.Api.Application.DTOs.Schedules.HarmonizerJobResultDto>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.IHarmonizerJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.HarmonizerJobRunner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IHarmonizerContextBuilder,
                           Klacks.Api.Infrastructure.Services.Schedules.HarmonizerContextBuilder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IHarmonizerApplyService,
                           Klacks.Api.Infrastructure.Services.Schedules.HarmonizerApplyService>();
        services.AddSingleton<Klacks.ScheduleOptimizer.Wizard4.IWizard4OptimizationCore,
                              Klacks.ScheduleOptimizer.Wizard4.Wizard4OptimizationCore>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.IHeavyWorkGate,
                              Klacks.Api.Infrastructure.Services.HeavyWorkGate>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4Runner,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4Runner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4AgentResolver,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4AgentResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IAvailabilityIneligibilityService,
                           Klacks.Api.Application.Services.Schedules.AvailabilityIneligibilityService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IHarmonizerTelemetrySink,
                           Klacks.Api.Infrastructure.Services.Schedules.LoggingHarmonizerTelemetrySink>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.IWorkSofteningRepository,
                           Klacks.Api.Infrastructure.Repositories.Schedules.WorkSofteningRepository>();

        services.AddScoped<Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm.IPlanProposalProvider,
                           Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.LlmPlanProposalProvider>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerEngine>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerRunService>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerModelCheckService>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.IHarmonizerEvalRunnerService,
                           Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HarmonizerEvalRunnerService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.SpeechModelCheckService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.KlacksyModelCheckService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.ICustomerGroupingPlanner,
            Klacks.Api.Application.Services.Grouping.CustomerGroupingPlanner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupGeocoder,
            Klacks.Api.Application.Services.Grouping.GroupGeocoder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupPlaceClassifier,
            Klacks.Api.Application.Services.Grouping.GroupPlaceClassifier>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupLocationResolver,
            Klacks.Api.Application.Services.Grouping.GroupLocationResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer.IHolisticHarmonizerApplyService,
                           Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerApplyService>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerModelCapabilityCache>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.JobTerminalStateCache<
            Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer.HolisticHarmonizerRunResponse>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer.IHolisticHarmonizerJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerJobRunner>();

        services.AddSingleton<Klacks.Api.Application.Services.Schedules.AutoWizard.AutoWizardJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules
            .JobTerminalStateCache<Klacks.Api.Application.DTOs.Schedules.AutoWizard.AutoWizardJobResultDto>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.AutoWizard.IAutoWizardHubNotifier,
                              Klacks.Api.Infrastructure.Services.Schedules.AutoWizard.AutoWizardHubNotifier>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.AutoWizard.IAutoWizardJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.AutoWizard.AutoWizardJobRunner>();

        services.AddShiftServices();
        services.AddClientServices();
        services.AddGroupServices();
        services.AddScheduleServices(configuration);
        services.AddSettingsServices(configuration);
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
        services.AddScoped<IShiftQueryPipelineService, ShiftQueryPipelineService>();
        services.AddScoped<IShiftValidator, ShiftValidator>();
        services.AddScoped<IShiftGroupManagementService, ShiftGroupManagementService>();
        services.AddScoped<IShiftTreeService, ShiftTreeService>();
        services.AddScoped<IShiftResetService, ShiftResetService>();
        services.AddScoped<IShiftCutFacade, ShiftCutFacade>();
        services.AddScoped<IShiftScheduleService, ShiftScheduleService>();
        services.AddScoped<IShiftGroupFilterService, ShiftGroupFilterService>();
        services.AddScoped<ISelectedGroupContextResolver, SelectedGroupContextResolver>();
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

    private static void AddScheduleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScheduleTimeOptions>(configuration.GetSection(ScheduleTimeOptions.SectionName));
        services.AddScoped<IScheduleEntriesService, ScheduleEntriesService>();
        services.AddScoped<IClientAvailabilityScheduleService, ClientAvailabilityScheduleService>();
        services.AddScoped<IWorkLockLevelService, WorkLockLevelService>();
        services.AddScoped<IDayLockService, DayLockService>();
        services.AddScoped<IPeriodHoursService, PeriodHoursService>();
        services.AddScoped<IScheduleChangeTracker, ScheduleChangeTracker>();
        services.AddScoped<IContainerAvailableTasksService, ContainerAvailableTasksService>();
        services.AddScoped<IDistanceMatrixBuilder, DistanceMatrixBuilder>();
        services.AddScoped<IRouteDirectionsBuilder, RouteDirectionsBuilder>();
        services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
        services.AddScoped<IContainerAutofillService, ContainerAutofillService>();
        services.AddScoped<IAbsenceSortingService, AbsenceSortingService>();
        services.AddScoped<IAbsencePaginationService, AbsencePaginationService>();
        services.AddScoped<IAbsenceExportService, AbsenceExportService>();
        services.AddScoped<IWorkChangeEffectiveTimeService, WorkChangeEffectiveTimeService>();
        services.AddScoped<IWorkMacroService, WorkMacroService>();
        services.AddScoped<IBreakMacroService, BreakMacroService>();
        services.AddScoped<IContainerWorkChildrenManager, ContainerWorkChildrenManager>();
        services.AddScoped<ICalendarSelectionUpdateService, CalendarSelectionUpdateService>();
        services.AddScoped<IScheduleCompletionService, ScheduleCompletionService>();
        services.AddScoped<IOvertimeCascadeService, OvertimeCascadeService>();
        services.AddScoped<ContainerTemplateService>();
        services.AddScoped<IWorkChangeResultService, WorkChangeResultService>();
        services.AddScoped<IWorkNotificationFacade, WorkNotificationFacade>();
        services.AddScoped<IBreakUserContextProvider, Klacks.Api.Application.Services.Breaks.BreakUserContextProvider>();
        services.AddSingleton<ITimelineCalculationService, TimelineCalculationService>();
        services.AddScoped<ITravelTimeCalculationService, TravelTimeCalculationService>();
        services.AddScoped<IContainerWorkExpansionService, ContainerWorkExpansionService>();
        services.AddScoped<IContainerWorkCascadeService, ContainerWorkCascadeService>();
    }

    private static void AddSettingsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISettingsEncryptionService, SettingsEncryptionService>();
        services.AddSingleton<ILanguagePluginService, LanguagePluginService>();
        services.AddSingleton<IFeaturePluginService, FeaturePluginService>();
        services.AddScoped<RegionSetupService>();
        services.AddScoped<IRegionSetupService>(sp => sp.GetRequiredService<RegionSetupService>());
        services.AddScoped<IRegionEntityImportService>(sp => sp.GetRequiredService<RegionSetupService>());
        services.AddScoped<IRegionPackageUpdateRunner, RegionPackageUpdateRunner>();
        services.AddSingleton<IRegionPackageSignatureVerifier, RegionPackageSignatureVerifier>();

        var marketplaceBaseUrl = configuration.GetValue<string>("Marketplace:BaseUrl")
            ?? configuration.GetValue<string>("LanguagePlugins:MarketplaceUrl");
        if (!string.IsNullOrWhiteSpace(marketplaceBaseUrl))
        {
            services.AddHttpClient<IMarketplaceClient, MarketplaceClient>(client =>
            {
                client.BaseAddress = new Uri(marketplaceBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        services.AddScoped<ICalendarRuleFilterService, CalendarRuleFilterService>();
        services.AddScoped<ICalendarRuleSortingService, CalendarRuleSortingService>();
        services.AddScoped<ICalendarRulePaginationService, CalendarRulePaginationService>();
        services.AddScoped<IMacroManagementService, MacroManagementService>();
        services.AddScoped<IDefaultShiftMacroResolver, DefaultShiftMacroResolver>();
        services.AddScoped<ISettingsTokenService, SettingsTokenService>();
        services.AddScoped<IEmailTestService, EmailTestService>();
        services.AddScoped<IScheduleEmailService, ScheduleEmailService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IImapEmailService, ImapEmailService>();
        services.AddScoped<IImapTestService, ImapTestService>();
        services.AddScoped<ISpamFilterService, SpamFilterService>();
        services.AddScoped<IEmailClientAssignmentService, EmailClientAssignmentService>();
        services.AddScoped<IEmailIntentAnalysisService, EmailIntentAnalysisService>();
        services.AddScoped<IEmailAnalysisNotifier, EmailAnalysisNotifier>();
        services.AddScoped<IEmailAnalysisRepository, EmailAnalysisRepository>();
        services.AddScoped<IEmailActionOrchestrator, EmailActionOrchestrator>();
        services.AddScoped<IEmailPeriodLoadService, EmailPeriodLoadService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Email.IEmailCapacityAdvisor, EmailCapacityAdvisor>();
        services.AddSingleton<IEmailReclassificationTrigger, EmailReclassificationTrigger>();

        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.EmailPolling)
            services.AddHostedService<EmailPollingBackgroundService>();

        services.AddHttpClient<IMarketplaceClientService, MarketplaceClientService>();
        services.AddHttpClient<IRegionPackageMarketplaceClient, RegionPackageMarketplaceClient>();

        if (bgOptions.RegionPackageUpdate)
            services.AddHostedService<RegionPackageUpdateService>();
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
        services.AddScoped<IRefreshTokenService, Services.Authentication.RefreshTokenService>();
        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<IAccountPasswordService, AccountPasswordService>();
        services.AddScoped<IAccountRegistrationService, AccountRegistrationService>();
        services.AddScoped<IAccountManagementService, AccountManagementService>();
        services.AddScoped<IAccountNotificationService, AccountNotificationService>();
        services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>();
        services.AddScoped<ILdapService, Services.Identity.LdapService>();
        services.AddScoped<IOAuth2Service, Services.Identity.OAuth2Service>();
        services.AddSingleton<IOAuth2StateStore, Services.Identity.OAuth2StateStore>();
        services.AddScoped<IClientSyncService, ClientSyncService>();
        services.AddScoped<IClientAddressService, ClientAddressService>();
    }

    private static void AddAssistantServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLLMCoreServices();
        services.AddLLMProviders();
        services.AddSkillServices();
        services.AddKnowledgeIndexServices(configuration);
        services.AddAssistantBackgroundServices(configuration);
    }

    internal static void AddLLMCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ILLMService, LLMService>();
        services.AddScoped<ILLMBackgroundTaskService, LLMBackgroundTaskService>();
        services.AddScoped<ITrajectoryCaptureService, Klacks.Api.Application.Services.Assistant.Evaluation.TrajectoryCaptureService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.IGoldsetLoader, Klacks.Api.Application.Services.Assistant.Evaluation.FileGoldsetLoader>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.IEvalRunnerService, Klacks.Api.Application.Services.Assistant.Evaluation.EvalRunnerService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.ITurnGoldsetLoader, Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.FileTurnGoldsetLoader>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.ITurnReplayService, Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.TurnReplayService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.ITurnEvalRunnerService, Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.TurnEvalRunnerService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.ISlotEntityResolver, Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.ClientSlotEntityResolver>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval.TurnGoldsetCandidateExtractor>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.ITurnGoldsetCandidateRepository, Klacks.Api.Infrastructure.Repositories.Assistant.TurnGoldsetCandidateRepository>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.ISpeechGoldsetLoader, Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.FileSpeechGoldsetLoader>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.ISpeechTranscriptionService, Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.SttSpeechTranscriptionService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.ISpeechWerEvalService, Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval.SpeechWerEvalService>();
        // Phase 2-4 autonomy (klacksy-autonomy-roadmap.md). S3 ships the executor + repository.
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanningAgent, Klacks.Api.Application.Services.Assistant.Planning.PlanningAgent>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentPlanRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentPlanRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalCandidateRepository, Klacks.Api.Infrastructure.Repositories.Assistant.GoalCandidateRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalReflectionService, Klacks.Api.Application.Services.Assistant.Reflection.GoalReflectionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalSignalSource, Klacks.Api.Application.Services.Assistant.Reflection.TriggerHistoryGoalSignalSource>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalPlanDraftService, Klacks.Api.Application.Services.Assistant.Reflection.GoalPlanDraftService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalPlanExecutionService, Klacks.Api.Application.Services.Assistant.Reflection.GoalPlanExecutionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISlackOwnerBridgeService, Klacks.Api.Application.Services.Assistant.SlackOwnerBridgeService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanStepExecutor, Klacks.Api.Application.Services.Assistant.Planning.PlanStepExecutor>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Planning.IPlanChatService, Klacks.Api.Application.Services.Assistant.Planning.PlanChatService>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPlanExecutionRegistry, Klacks.Api.Infrastructure.Services.Assistant.PlanExecutionRegistry>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IKlacksOntologyService, Klacks.Api.Application.Services.Assistant.Ontology.KlacksOntologyService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerService, Klacks.Api.Application.Services.Assistant.Triggers.AgentTriggerService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanningAudienceResolver, Klacks.Api.Infrastructure.Services.Assistant.PlanningAudienceResolver>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerRateLimiter, Klacks.Api.Application.Services.Assistant.Triggers.AgentTriggerRateLimiter>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerPreferenceRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentTriggerPreferenceRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveTriggerDispatchRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ProactiveTriggerDispatchRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IDismissStreakEvaluator, Klacks.Api.Application.Services.Assistant.Triggers.DismissStreakEvaluator>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IUserActivityTracker, Klacks.Api.Application.Services.Assistant.Triggers.UserActivityTracker>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentAutonomyPreferenceRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentAutonomyPreferenceRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.ISkillRiskClassifier, Klacks.Api.Application.Skills.Meta.SkillRiskClassifier>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingConfirmationRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingConfirmationRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingConfirmationStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingConfirmationStore>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingRecipeRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingRecipeRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingRecipeStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingRecipeStore>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingCompanyRuleDraftRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingCompanyRuleDraftRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingCompanyRuleDraftStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingCompanyRuleDraftStore>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Settings.ICompanyRuleParameterCatalog, Klacks.Api.Domain.Services.Settings.CompanyRuleParameterCatalog>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Settings.ICompanyRuleDraftValidator, Klacks.Api.Domain.Services.Settings.CompanyRuleDraftValidator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingPlanningProfileDraftRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingPlanningProfileDraftRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingPlanningProfileDraftStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingPlanningProfileDraftStore>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Settings.IPlanningProfileParameterCatalog, Klacks.Api.Domain.Services.Settings.PlanningProfileParameterCatalog>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Settings.IPlanningProfileDraftValidator, Klacks.Api.Domain.Services.Settings.PlanningProfileDraftValidator>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Settings.ISettingValueValidator, Klacks.Api.Domain.Services.Settings.SettingValueValidator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAutonomyGate, Klacks.Api.Application.Services.Assistant.Autonomy.AutonomyGateService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ITurnConfirmationScope, Klacks.Api.Application.Services.Assistant.Autonomy.TurnConfirmationScope>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IEntityChangeNotifier, Klacks.Api.Application.Services.Assistant.EntityChangeNotifier>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecentEntityRepository, Klacks.Api.Infrastructure.Repositories.Assistant.RecentEntityRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecentEntityRegistrar, Klacks.Api.Application.Services.Assistant.RecentEntityRegistrar>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.INavigationGuidanceProvider, Klacks.Api.Domain.Services.Assistant.Guidance.ShiftNavigationGuidanceProvider>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerPreferenceService, Klacks.Api.Application.Services.Assistant.Triggers.PersistentAgentTriggerPreferenceService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.UnstaffedShift7dDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.LockConflictDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.TargetHoursDriftDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.ScenarioPendingDetector>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.PeriodCloseDueDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.ContractExpiringSoonDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.AvailabilityGapDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.PeriodOverdueDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.ClientMissingCoreDataDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.CuriosityQuestionDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentSkillExecutionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSkillExecutionRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientContractReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientContractReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientAvailabilityReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientAvailabilityReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientCoreDataReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientCoreDataReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISuggestionEntityNameReader, Klacks.Api.Infrastructure.Repositories.Assistant.SuggestionEntityNameReader>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISkillCoverageService, Klacks.Api.Application.Services.Assistant.Coverage.SkillCoverageService>();
        services.AddScoped<IAutoMemoryExtractionService, AutoMemoryExtractionService>();
        services.AddScoped<ITurnReflectionService, TurnReflectionService>();
        services.AddScoped<IConversationCompactionService, ConversationCompactionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ICheapestModelResolver, Klacks.Api.Domain.Services.Assistant.CheapestModelResolver>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IContextBudgetPolicy, Klacks.Api.Domain.Services.Assistant.ContextBudgetPolicy>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IReadOnlyToolsetFilter, Klacks.Api.Application.Services.Assistant.ReadOnlyToolsetFilter>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IReadOnlyResearchService, Klacks.Api.Application.Services.Assistant.ReadOnlyResearchService>();
        services.AddScoped<IHeartbeatLLMService, Klacks.Api.Domain.Services.Assistant.HeartbeatLLMService>();
        services.AddScoped<IHeartbeatDataCollector, Klacks.Api.Infrastructure.Services.Assistant.HeartbeatDataCollector>();
        services.AddScoped<ILLMProviderFactory, LLMProviderFactory>();
        services.AddSingleton<Klacks.Api.Infrastructure.Security.IHostAddressResolver, Klacks.Api.Infrastructure.Security.DnsHostAddressResolver>();
        services.AddScoped<IProviderConnectivityTester, ProviderConnectivityTester>();
        services.AddScoped<IProviderWebDiscovery, ProviderWebDiscovery>();
        services.AddHttpClient("ProviderConnectivityTester")
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
            {
                ConnectCallback = new Klacks.Api.Infrastructure.Security.PrivateNetworkBlockingConnectCallback(
                    sp.GetRequiredService<Klacks.Api.Infrastructure.Security.IHostAddressResolver>()).ConnectAsync
            });
        services.AddScoped<ILLMModelSyncService, LLMModelSyncService>();
        services.AddScoped<LLMProviderOrchestrator>();
        services.AddScoped<LLMConversationManager>();
        services.AddScoped<Application.Interfaces.Assistant.IRetrievalQueryBuilder, Application.Services.Assistant.RetrievalQueryBuilder>();
        services.AddScoped<Application.Interfaces.Assistant.ISkillToolsetAssembler, Application.Services.Assistant.SkillToolsetAssembler>();
        services.AddScoped<LLMFunctionExecutor>();
        services.AddScoped<LLMResponseBuilder>();
        services.AddScoped<LLMChatPipeline>();
        services.AddScoped<ILLMStreamingOrchestrator, LLMStreamingOrchestrator>();
        services.AddScoped<LLMSystemPromptBuilder>();
        services.AddSingleton<IPromptTranslationProvider, PromptTranslationProvider>();
        services.AddScoped<IEmbeddingService, Klacks.Api.Infrastructure.Services.Assistant.EmbeddingService>();
        services.AddSingleton<ILanguageMetadataProvider, LanguageMetadataProvider>();
        services.AddScoped<IIdentityContextProvider, IdentityContextProvider>();
        services.AddScoped<IMemoryRetrievalService, MemoryRetrievalService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IMemoryRetrievalExpander, Application.Services.Assistant.MemoryGraph.MemoryRetrievalExpander>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IMemoryRelationBuilder, Application.Services.Assistant.MemoryGraph.MemoryRelationBuilder>();
        services.AddScoped<ContextAssemblyPipeline>();
        services.AddScoped<RecipeEngineService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ICompetingSkillIntentDetector, Application.Services.Assistant.CompetingSkillIntentDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecipeSkillMarginEvaluator, Application.Services.Assistant.RecipeSkillMarginEvaluator>();
        services.AddScoped<RecipeSlotExtractor>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IRuleContextProvider, Domain.Services.Assistant.RuleContextProvider>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IPlanningScopeEnricher, Application.Services.Assistant.PlanningScopeEnricher>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IEntityCandidateGrounder, Application.Services.Assistant.ClientNameCandidateGrounder>();
        services.AddSingleton<ISentimentAnalyzer, Domain.Services.Assistant.SentimentAnalyzer>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ITtsApiKeyResolver, Klacks.Api.Infrastructure.Services.Assistant.TtsApiKeyResolver>();
        services.AddSingleton<Klacks.Api.Infrastructure.Services.Assistant.EdgeTtsService>();
        services.AddSingleton<ITtsProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.EdgeTtsService>());
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.OpenAiTtsService>();
        services.AddScoped<ITtsProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.OpenAiTtsService>());
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.ElevenLabsTtsService>();
        services.AddScoped<ITtsProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.ElevenLabsTtsService>());
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.GoogleTtsService>();
        services.AddScoped<ITtsProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.GoogleTtsService>());
        services.AddScoped<ITranscriptionEnhancerService, TranscriptionEnhancerService>();

        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISuggestionsRanker,
            Klacks.Api.Application.Services.Assistant.SuggestionsRanker>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IOnboardingService,
            Klacks.Api.Application.Services.Assistant.OnboardingService>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IOpenMeteoClient,
            Klacks.Api.Infrastructure.Services.OpenMeteoClient>();
        services.AddHttpClient(
            Klacks.Api.Infrastructure.Services.OpenMeteoClient.HttpClientName,
            client =>
            {
                client.BaseAddress = new Uri("https://api.open-meteo.com/");
                client.Timeout = TimeSpan.FromSeconds(3);
            });
        services.AddHttpClient(
            Klacks.Api.Infrastructure.Services.OpenMeteoClient.AirQualityHttpClientName,
            client =>
            {
                client.BaseAddress = new Uri("https://air-quality-api.open-meteo.com/");
                client.Timeout = TimeSpan.FromSeconds(3);
            });
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPublicHolidayProvider,
            Klacks.Api.Infrastructure.Services.NagerDateHolidayProvider>();
        services.AddHttpClient(
            Klacks.Api.Infrastructure.Services.NagerDateHolidayProvider.HttpClientName,
            client =>
            {
                client.BaseAddress = new Uri("https://date.nager.at/");
                client.Timeout = TimeSpan.FromSeconds(3);
            });
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGreetingComposer,
            Klacks.Api.Application.Services.Assistant.GreetingComposer>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.ICompanyLocationProvider,
            Klacks.Api.Infrastructure.Services.CompanyLocationProvider>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.ICompanyClock,
            Klacks.Api.Infrastructure.Services.CompanyClock>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.IWeekConfiguration,
            Klacks.Api.Infrastructure.Services.WeekConfiguration>();

        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.DeepgramSttProvider>();
        services.AddScoped<ISttProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.DeepgramSttProvider>());
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.AssemblyAiSttProvider>();
        services.AddScoped<ISttProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.AssemblyAiSttProvider>());
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.GroqWhisperSttProvider>();
        services.AddScoped<ISttProvider>(sp => sp.GetRequiredService<Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.GroqWhisperSttProvider>());
        services.AddScoped<ICustomSttSessionFactory, Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt.CustomRestSttSessionFactory>();

        services.AddSingleton<IPhoneticEncoderFactory, Klacks.Api.Domain.Services.Assistant.Phonetics.PhoneticEncoderFactory>();
        services.AddSingleton<IPhoneticConfigProvider, Klacks.Api.Infrastructure.Services.Assistant.PhoneticConfigProvider>();
        services.AddScoped<IDictionaryService, Klacks.Api.Infrastructure.Services.Assistant.DictionaryService>();
        services.AddScoped<ITranscriptionDictionaryRepository, Klacks.Api.Infrastructure.Repositories.Assistant.TranscriptionDictionaryRepository>();
        services.AddScoped<ICustomSttProviderRepository, Klacks.Api.Infrastructure.Repositories.Assistant.CustomSttProviderRepository>();

    }

    private static void AddLLMProviders(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI.OpenAIProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic.AnthropicProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini.GeminiProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure.AzureOpenAIProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral.MistralProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek.DeepSeekProvider>();
        services.AddScoped<Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic.GenericOpenAICompatibleProvider>();

        // Reasoning-capable LLM models (DeepSeek-Reasoner, GPT o1, Claude with extended thinking,
        // Gemini Flash Thinking) routinely take 2-3 minutes for large prompts. The default 100s
        // HttpClient timeout aborts these mid-flight; 5 minutes is a safer ceiling for the
        // request-response style ProcessAsync path. Streaming uses CancellationTokens, so this
        // ceiling does not affect interactive chat.
        var llmHttpTimeout = TimeSpan.FromMinutes(5);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.OpenAI.OpenAIProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic.AnthropicProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Gemini.GeminiProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Azure.AzureOpenAIProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Mistral.MistralProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.DeepSeek.DeepSeekProvider>(c => c.Timeout = llmHttpTimeout);
        services.AddHttpClient<Klacks.Api.Infrastructure.Services.Assistant.Providers.Generic.GenericOpenAICompatibleProvider>(c => c.Timeout = llmHttpTimeout)
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
            {
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                    {
                        if (errors == System.Net.Security.SslPolicyErrors.None)
                            return true;

                        const string trustedDomain = "apertus.ai";
                        var host = (sender as System.Net.Security.SslStream)?.TargetHostName ?? string.Empty;
                        return host.Equals(trustedDomain, StringComparison.OrdinalIgnoreCase)
                            || host.EndsWith("." + trustedDomain, StringComparison.OrdinalIgnoreCase);
                    }
                },
                ConnectCallback = new Klacks.Api.Infrastructure.Security.CloudMetadataBlockingConnectCallback(
                    sp.GetRequiredService<Klacks.Api.Infrastructure.Security.IHostAddressResolver>()).ConnectAsync
            });
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
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetCurrentTimeSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.GetUserContextSkill>();
        services.AddScoped<Domain.Services.Assistant.Skills.Implementations.NavigateToSkill>();
        services.AddSingleton<Domain.Services.Assistant.Skills.Implementations.ValidateCalendarRuleSkill>();

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
        services.AddScoped<Application.Skills.ConfigureHeartbeatSkill>();
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
        services.AddScoped<Application.Skills.Meta.ReviewSkillSuggestionsSkill>();

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
        services.AddScoped<Persistence.Seed.RecipeSeedLoader>();
        services.AddScoped<Persistence.Seed.SentimentKeywordSeedService>();
        services.AddScoped<Persistence.Seed.NavigationTargetSynonymSeedService>();
        services.AddScoped<Persistence.Seed.KlacksyKnowledgeMemorySeed>();
        services.AddScoped<Persistence.Seed.ClientPhoneticBackfillSeed>();
        services.AddScoped<Application.Services.Assistant.SkillRegistryInitializer>();
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

    private static void AddKnowledgeIndexServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(KnowledgeIndexConstants.HttpClientName);

        var modelsRoot = ResolveModelsRoot(configuration);
        var onnxSupported = IsOnnxRuntimeSupported(configuration);

        services.AddSingleton<ModelLoader>(sp =>
            new ModelLoader(sp.GetRequiredService<IHttpClientFactory>().CreateClient(KnowledgeIndexConstants.HttpClientName)));

        if (onnxSupported)
        {
            services.AddSingleton<IEmbeddingProvider>(sp => new OnnxEmbeddingProvider(
                sp.GetRequiredService<ModelLoader>(),
                Path.Combine(modelsRoot, KnowledgeIndexConstants.EmbeddingModelName)));

            services.AddSingleton<IRerankerProvider>(sp => new OnnxRerankerProvider(
                sp.GetRequiredService<ModelLoader>(),
                Path.Combine(modelsRoot, KnowledgeIndexConstants.RerankerModelName)));
        }
        else
        {
            // Local-development fallback for hosts where ONNX cannot run (Windows ARM64): reuses the
            // "openai" LLM provider's API key already configured for chat instead of a separate secret.
            // A real (if weaker than the ONNX cross-encoder) similarity signal. Production (Hetzner,
            // x64) always takes the onnxSupported branch above and never reaches this; if no "openai"
            // provider key is configured either, this fails loud per-call (caught by the semantic
            // recipe match / retrieval call sites) instead of the old silent NullEmbeddingProvider no-op.
            // Was GeminiEmbeddingProvider until 2026-07-25: gemini-embedding-001 started returning
            // HTTP 429 "prepayment credits are depleted" on every call, which silently reduced each
            // chat turn to the always-on skills. text-embedding-3-small honours the "dimensions"
            // parameter, so it needs no truncation workaround. GeminiEmbeddingProvider is kept as a
            // drop-in alternative — swapping the type here is the whole switch.
            services.AddScoped<ILlmProviderCredentialReader, LlmProviderCredentialReader>();
            services.AddScoped<IEmbeddingProvider>(sp => new OpenAiEmbeddingProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(KnowledgeIndexConstants.HttpClientName),
                sp.GetRequiredService<ILlmProviderCredentialReader>()));
            services.AddScoped<IRerankerProvider>(sp =>
                new EmbeddingSimilarityRerankerProvider(sp.GetRequiredService<IEmbeddingProvider>()));
        }

        services.AddScoped<IKnowledgeIndexRepository>(sp =>
        {
            var context = sp.GetRequiredService<DataBaseContext>();
            var conn = (NpgsqlConnection)context.Database.GetDbConnection();
            if (conn.State == System.Data.ConnectionState.Closed)
                conn.Open();
            return new KnowledgeIndexRepository(conn);
        });

        services.AddScoped<IKnowledgeIndexSynchronizer, KnowledgeIndexSynchronizer>();
        services.AddScoped<IKnowledgeRetrievalService, KnowledgeRetrievalService>();

        // Scoped, so the pass ordinal in the [retrieval] log line counts within one turn.
        services.AddScoped<RetrievalCallCounter>();

        // Scoped on purpose: reusing cross-encoder scores is only unconditionally safe within one
        // request, where the query and candidate set provably have not changed underneath.
        services.AddScoped<RerankScoreCache>();

        // Singleton: the shadow measurement is about reuse ACROSS requests, so it has to outlive them.
        services.AddSingleton<ToolsetCacheShadow>();

        services.AddHostedService<KnowledgeIndexStartupService>();

        // Registered after the sync service so the index is current before the sessions are built.
        // It is a BackgroundService, so it does not hold up the host either way.
        services.AddHostedService<OnnxWarmupService>();
    }

    private static bool IsOnnxRuntimeSupported(IConfiguration configuration)
    {
        var configured = configuration[KnowledgeIndexConstants.OnnxEnabledConfigKey];
        if (bool.TryParse(configured, out var enabled))
        {
            return enabled;
        }

        // ONNX Runtime 1.20.1's bundled cpuinfo could not detect the Snapdragon X SoC on Windows ARM64
        // and faulted the process when an InferenceSession was created, so ONNX used to be disabled
        // there. That no longer reproduces on the 1.27.1 runtime this project ships: opening a session
        // AND running a forward pass both succeed on Windows ARM64 (see
        // OnnxRuntimePlatformProbeTests in Klacks.IntegrationTest, which is the way to re-check this on
        // any new platform). Keeping the block would silently downgrade every ARM host to a remote
        // embedding API, and ARM servers are becoming ordinary deployment targets.
        return true;
    }

    private static string ResolveModelsRoot(IConfiguration configuration)
    {
        var configured = configuration[KnowledgeIndexConstants.ModelsRootConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Path.Combine(AppContext.BaseDirectory, KnowledgeIndexConstants.ModelsCacheSubdirectory);
    }

    private static void AddAssistantBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.Embedding && IsOnnxRuntimeSupported(configuration))
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.EmbeddingBackgroundService>();

        if (bgOptions.MemoryCleanup)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.MemoryCleanupBackgroundService>();

        if (bgOptions.SkillGapSuggestion)
            services.AddHostedService<Klacks.Api.Infrastructure.Services.Assistant.SkillGapSuggestionBackgroundService>();

        services.AddScoped<ISkillGapDetector, Klacks.Api.Domain.Services.Assistant.Skills.SkillGapDetector>();

        services.AddSingleton(new Klacks.Api.Domain.Models.Assistant.Grounding.AnswerGroundingOptions(
            configuration.GetValue<string>("Assistant:AnswerGroundingMode")
                ?? Klacks.Api.Domain.Constants.AnswerGroundingModes.Off));
        services.AddScoped<IAnswerGroundingEvaluator, Klacks.Api.Domain.Services.Assistant.Grounding.AnswerGroundingEvaluator>();
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
