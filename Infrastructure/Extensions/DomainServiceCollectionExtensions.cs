// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Domain-service registrations, split out of ServiceCollectionExtensions where AddDomainServices had
/// grown to 161 lines. The sub-methods are strictly contiguous slices of the original method and are
/// called in the original order, so no registration changes position relative to any other - which
/// matters for the few service types registered more than once and for every IEnumerable injection.
/// That is also why the grouping and order-sealing registrations sit between the two holistic
/// harmonizer blocks rather than in a group of their own.
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
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.Inbound;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Repositories.Associations;
using Klacks.Api.Infrastructure.Repositories.Email;
using Klacks.Api.Infrastructure.Repositories.Inbound;
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
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

internal static class DomainServiceCollectionExtensions
{
    internal static void AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCoreDomainServices();
        services.AddSchedulingRuleServices();
        services.AddWizardServices();
        services.AddHarmonizerServices();
        services.AddWizard4Services();
        services.AddHolisticHarmonizerServices();
        services.AddGroupingServices();
        services.AddHolisticHarmonizerJobServices();
        services.AddAutoWizardServices();
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
        services.AddScoped<IGroupVisibilityPreservationService, GroupVisibilityPreservationService>();
    }

    private static void AddScheduleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ScheduleTimeOptions>(configuration.GetSection(ScheduleTimeOptions.SectionName));
        services.AddHostedService<Klacks.Api.Infrastructure.Services.Schedules.ScheduleTimeZoneStartupCheckService>();
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
        services.AddScoped<ISettingsSecretResolver, SettingsSecretResolver>();
        services.AddSingleton<ILanguagePluginService, LanguagePluginService>();
        services.AddSingleton<IFeaturePluginService, FeaturePluginService>();
        services.AddScoped<IFeaturePluginAssistantSetupHintService, Klacks.Api.Application.Services.Plugins.FeaturePluginAssistantSetupHintService>();
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
        AddKlacksSelfApiClient(services, configuration);
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IInternalTokenIssuer, Services.Assistant.InternalTokenIssuer>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.ISelfApiRouteResolver, Services.Assistant.SelfApiRouteResolver>();
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
        services.AddScoped<IInboxAvailabilityService, InboxAvailabilityService>();
        services.AddScoped<IFeatureAvailabilityService, Klacks.Api.Infrastructure.Services.Plugins.FeatureAvailabilityService>();
        services.AddScoped<ISpamFilterService, SpamFilterService>();
        services.AddScoped<IEmailClientAssignmentService, EmailClientAssignmentService>();
        services.AddScoped<IInboundIntentAnalysisService, InboundIntentAnalysisService>();
        services.AddScoped<IInboundAnalysisNotifier, InboundAnalysisNotifier>();
        services.AddScoped<IInboundAnalysisRepository, InboundAnalysisRepository>();
        services.AddScoped<IInboundActionOrchestrator, InboundActionOrchestrator>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Plugins.IMessengerIntentQueue, Klacks.Api.Infrastructure.Plugins.MessengerIntentQueue>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Plugins.IMessengerIntentProcessor, Klacks.Api.Infrastructure.Plugins.MessengerIntentProcessor>();
        services.AddScoped<IEmailPeriodLoadService, EmailPeriodLoadService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Email.IEmailCapacityAdvisor, EmailCapacityAdvisor>();
        services.AddSingleton<IEmailReclassificationTrigger, EmailReclassificationTrigger>();

        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.EmailPolling)
            services.AddHostedService<EmailPollingBackgroundService>();

        if (bgOptions.MessengerIntentAnalysis)
        {
            services.AddScoped<Klacks.Plugin.Contracts.IInboundClientMessengerObserver, Klacks.Api.Infrastructure.Plugins.MessengerIntentObserver>();
            services.AddHostedService<Klacks.Api.Infrastructure.Plugins.MessengerIntentBackgroundService>();
        }

        services.AddHttpClient<IMarketplaceClientService, MarketplaceClientService>();
        services.AddHttpClient<IRegionPackageMarketplaceClient, RegionPackageMarketplaceClient>();

        if (bgOptions.RegionPackageUpdate)
            services.AddHostedService<RegionPackageUpdateService>();
    }

    private static void AddCoreDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IGetAllClientIdsFromGroupAndSubgroups, GroupClientService>();
        services.AddScoped<IGroupVisibilityService, GroupVisibilityService>();
        services.AddScoped<IHolidaysListCalculator, HolidaysListCalculator>();
        services.AddScoped<IMacroEngine, MacroEngine>();
        services.AddSingleton<IMacroCache, MacroCache>();
        services.AddSingleton<IHolidayCalculatorCache, HolidayCalculatorCache>();
        services.AddSingleton<ISettingsChangeVersion, SettingsChangeVersion>();
        services.AddScoped<IMacroDataProvider, MacroDataProvider>();
        services.AddScoped<IMacroCompilationService, MacroCompilationService>();
        services.AddScoped<IMacroScriptValidator, MacroScriptValidator>();
        services.AddScoped<IClientContractDataProvider, ClientContractDataProvider>();
    }

    private static void AddSchedulingRuleServices(this IServiceCollection services)
    {
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
    }

    private static void AddWizardServices(this IServiceCollection services)
    {
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

        services.AddSingleton<Klacks.Api.Application.Services.Schedules.AutofillStartGuard>();
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
    }

    private static void AddHarmonizerServices(this IServiceCollection services)
    {
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
    }

    private static void AddWizard4Services(this IServiceCollection services)
    {
        services.AddSingleton<Klacks.ScheduleOptimizer.Wizard4.IWizard4OptimizationCore,
                              Klacks.ScheduleOptimizer.Wizard4.Wizard4OptimizationCore>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.IHeavyWorkGate,
                              Klacks.Api.Infrastructure.Services.HeavyWorkGate>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4Runner,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4Runner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4AgentResolver,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4AgentResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4SnapshotGuard,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4SnapshotGuard>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IWizard4CandidateLifecycleService,
                           Klacks.Api.Infrastructure.Services.Schedules.Wizard4CandidateLifecycleService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IAvailabilityIneligibilityService,
                           Klacks.Api.Application.Services.Schedules.AvailabilityIneligibilityService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.IHarmonizerTelemetrySink,
                           Klacks.Api.Infrastructure.Services.Schedules.LoggingHarmonizerTelemetrySink>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.IWorkSofteningRepository,
                           Klacks.Api.Infrastructure.Repositories.Schedules.WorkSofteningRepository>();
    }

    private static void AddHolisticHarmonizerServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm.IPlanProposalProvider,
                           Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.LlmPlanProposalProvider>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerEngine>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerRunService>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerModelCheckService>();
        services.AddScoped<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.IHarmonizerEvalRunnerService,
                           Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HarmonizerEvalRunnerService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.SpeechModelCheckService>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.KlacksyModelCheckService>();
    }

    private static void AddGroupingServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.ICustomerGroupingPlanner,
            Klacks.Api.Application.Services.Grouping.CustomerGroupingPlanner>();
        services.AddScoped<Klacks.Api.Application.Interfaces.IOrderSealingService,
            Klacks.Api.Application.Services.Orders.OrderSealingService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupGeocoder,
            Klacks.Api.Application.Services.Grouping.GroupGeocoder>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupPlaceClassifier,
            Klacks.Api.Application.Services.Grouping.GroupPlaceClassifier>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Grouping.IGroupLocationResolver,
            Klacks.Api.Application.Services.Grouping.GroupLocationResolver>();
    }

    private static void AddHolisticHarmonizerJobServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer.IHolisticHarmonizerApplyService,
                           Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerApplyService>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerModelCapabilityCache>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.JobTerminalStateCache<
            Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer.HolisticHarmonizerRunResponse>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer.IHolisticHarmonizerJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer.HolisticHarmonizerJobRunner>();
    }

    private static void AddAutoWizardServices(this IServiceCollection services)
    {
        services.AddSingleton<Klacks.Api.Application.Services.Schedules.AutoWizard.AutoWizardJobRegistry>();
        services.AddSingleton<Klacks.Api.Application.Services.Schedules
            .JobTerminalStateCache<Klacks.Api.Application.DTOs.Schedules.AutoWizard.AutoWizardJobResultDto>>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.AutoWizard.IAutoWizardHubNotifier,
                              Klacks.Api.Infrastructure.Services.Schedules.AutoWizard.AutoWizardHubNotifier>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Schedules.AutoWizard.IAutoWizardJobRunner,
                              Klacks.Api.Infrastructure.Services.Schedules.AutoWizard.AutoWizardJobRunner>();
    }

    /// <summary>
    /// Registers the loopback client skills use to mutate state through the own REST API. Without a
    /// configured SelfApi:BaseUrl nothing is registered, so a mutating skill fails with a missing
    /// dependency at startup instead of silently writing past [Authorize] again.
    /// </summary>
    private static void AddKlacksSelfApiClient(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(SelfApiOptions.SectionName).Get<SelfApiOptions>();
        if (options is null || string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            return;
        }

        var builder = services.AddHttpClient<IKlacksSelfApiClient, Services.Assistant.KlacksSelfApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        if (options.AcceptAnyServerCertificate)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }
    }
}
