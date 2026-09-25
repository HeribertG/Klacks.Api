// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assistant/LLM core registrations, split out of ServiceCollectionExtensions where AddLLMCoreServices
/// had grown to 225 lines. As with the domain block, the sub-methods are contiguous slices called in
/// the original order: the trigger detectors are registered as many implementations of one interface,
/// so their sequence is the order they run in, and the TimeProvider registration between two of them
/// has to keep its place for the same reason. AddLLMCoreServices keeps its name and internal
/// visibility because NavigationGuidanceProviderRegistrationTests composes it on its own.
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
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

internal static class LLMCoreServiceCollectionExtensions
{
    internal static void AddLLMCoreServices(this IServiceCollection services)
    {
        services.AddAssistantEvaluationServices();
        services.AddAssistantPlanningServices();
        services.AddProactiveTriggerServices();
        services.AddAssistantPendingStateServices();
        services.AddAgentTriggerDetectors();
        services.AddLLMPipelineServices();
        services.AddAssistantContextServices();
        services.AddSpeechServices();
    }

    private static void AddAssistantEvaluationServices(this IServiceCollection services)
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
    }

    private static void AddAssistantPlanningServices(this IServiceCollection services)
    {
        // Phase 2-4 autonomy (klacksy-autonomy-roadmap.md). S3 ships the executor + repository.
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanningAgent, Klacks.Api.Application.Services.Assistant.Planning.PlanningAgent>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentPlanRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentPlanRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalCandidateRepository, Klacks.Api.Infrastructure.Repositories.Assistant.GoalCandidateRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalReflectionService, Klacks.Api.Application.Services.Assistant.Reflection.GoalReflectionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalSignalSource, Klacks.Api.Application.Services.Assistant.Reflection.TriggerHistoryGoalSignalSource>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalCandidateRevalidationService, Klacks.Api.Application.Services.Assistant.Reflection.GoalCandidateRevalidationService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IScheduleActivityProbe, Klacks.Api.Infrastructure.Repositories.Assistant.ScheduleActivityProbe>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalPlanDraftService, Klacks.Api.Application.Services.Assistant.Reflection.GoalPlanDraftService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IGoalPlanExecutionService, Klacks.Api.Application.Services.Assistant.Reflection.GoalPlanExecutionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISlackOwnerBridgeService, Klacks.Api.Application.Services.Assistant.SlackOwnerBridgeService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanStepExecutor, Klacks.Api.Application.Services.Assistant.Planning.PlanStepExecutor>();
        services.AddScoped<Klacks.Api.Application.Services.Assistant.Planning.IPlanChatService, Klacks.Api.Application.Services.Assistant.Planning.PlanChatService>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPlanExecutionRegistry, Klacks.Api.Infrastructure.Services.Assistant.PlanExecutionRegistry>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IActiveTurnRegistry, Klacks.Api.Infrastructure.Services.Assistant.ActiveTurnRegistry>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IKlacksOntologyService, Klacks.Api.Application.Services.Assistant.Ontology.KlacksOntologyService>();
    }

    private static void AddProactiveTriggerServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerService, Klacks.Api.Application.Services.Assistant.Triggers.AgentTriggerService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveReminderService, Klacks.Api.Application.Services.Assistant.Triggers.ProactiveReminderService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IOfflineMessengerNotifier, Klacks.Api.Infrastructure.Plugins.MessagingPluginOfflineMessengerNotifier>();
        services.AddScoped<Klacks.Plugin.Contracts.IInboundMessengerObserver, Klacks.Api.Infrastructure.Plugins.MessagingPluginInboundMessageObserver>();
        services.AddEscalationChainServices();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveMessengerTextComposer, Klacks.Api.Application.Services.Assistant.Triggers.ProactiveMessengerTextComposer>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPlanningAudienceResolver, Klacks.Api.Infrastructure.Services.Assistant.PlanningAudienceResolver>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Authentification.IUserAbsencePeriodRepository, Klacks.Api.Infrastructure.Repositories.Authentification.UserAbsencePeriodRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerRateLimiter, Klacks.Api.Application.Services.Assistant.Triggers.AgentTriggerRateLimiter>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerPreferenceRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentTriggerPreferenceRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveTriggerDispatchRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ProactiveTriggerDispatchRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentConditionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentConditionRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentConditionScopeResolver, Klacks.Api.Infrastructure.Services.Assistant.AgentConditionScopeResolver>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentConditionLedgerService, Klacks.Api.Application.Services.Assistant.Conditions.AgentConditionLedgerService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentConditionDigestService, Klacks.Api.Application.Services.Assistant.Conditions.AgentConditionDigestService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerGovernanceRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentTriggerGovernanceRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveGovernanceResolver, Klacks.Api.Application.Services.Assistant.Conditions.ProactiveGovernanceResolver>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IConditionRemediationRegistry, Klacks.Api.Application.Services.Assistant.Conditions.ConditionRemediationRegistry>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveActionIdentityProvider, Klacks.Api.Application.Services.Assistant.Conditions.ProactiveActionIdentityProvider>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IQuietWindowService, Klacks.Api.Application.Services.Assistant.Conditions.QuietWindowService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IProactiveActionReporter, Klacks.Api.Application.Services.Assistant.Conditions.ProactiveActionReporter>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentConditionActionService, Klacks.Api.Application.Services.Assistant.Conditions.AgentConditionActionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IDismissStreakEvaluator, Klacks.Api.Application.Services.Assistant.Triggers.DismissStreakEvaluator>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IHelpfulBoostEvaluator, Klacks.Api.Application.Services.Assistant.Triggers.HelpfulBoostEvaluator>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IUserActivityTracker, Klacks.Api.Application.Services.Assistant.Triggers.UserActivityTracker>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentAutonomyPreferenceRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentAutonomyPreferenceRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAdminAutonomyLevelAggregator, Klacks.Api.Application.Services.Assistant.Autonomy.AdminAutonomyLevelAggregator>();
    }

    private static void AddAssistantPendingStateServices(this IServiceCollection services)
    {
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.ISkillRiskClassifier, Klacks.Api.Application.Skills.Meta.SkillRiskClassifier>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.ICancellableSkillPolicy, Klacks.Api.Application.Services.Assistant.CancellableSkillPolicy>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingConfirmationRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingConfirmationRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingConfirmationStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingConfirmationStore>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IPendingRecipeRepository, Klacks.Api.Infrastructure.Repositories.Assistant.PendingRecipeRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IPendingRecipeStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentPendingRecipeStore>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAssistantLastActionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AssistantLastActionRepository>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IAssistantLastActionStore, Klacks.Api.Infrastructure.Services.Assistant.PersistentAssistantLastActionStore>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ITurnPreparationService, Klacks.Api.Domain.Services.Assistant.TurnPreparationService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IDeterministicRouteProbe, Klacks.Api.Application.Services.Assistant.DeterministicRouteProbe>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.ISkillInverseResolver, Klacks.Api.Application.Services.Assistant.SkillInverseResolver>();
        services.AddScoped<Klacks.Api.Application.Interfaces.IRecipeRunRepository, Klacks.Api.Infrastructure.Repositories.Assistant.RecipeRunRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecipeRunExpirySweep, Klacks.Api.Application.Services.Assistant.RecipeRunExpirySweep>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecipeRunRecorder, Klacks.Api.Application.Services.Assistant.RecipeRunRecorder>();
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
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ITurnConfirmationDiscarder, Klacks.Api.Application.Services.Assistant.Autonomy.TurnConfirmationDiscarder>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IStoppedTurnCleanup, Klacks.Api.Application.Services.Assistant.StoppedTurnCleanup>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IEntityChangeNotifier, Klacks.Api.Application.Services.Assistant.EntityChangeNotifier>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecentEntityRepository, Klacks.Api.Infrastructure.Repositories.Assistant.RecentEntityRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IRecentEntityRegistrar, Klacks.Api.Application.Services.Assistant.RecentEntityRegistrar>();
    }

    private static void AddAgentTriggerDetectors(this IServiceCollection services)
    {
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
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.OpenOrderDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.UncutFullDayShiftDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.EmptyContainerDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.NextPeriodSchedulingDueDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.KlacksyLearnedDigestDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.NoScheduleYetDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.UngroupedWorkforceDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.UngroupedShiftsDetector>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.INextPeriodAutonomyResolver, Klacks.Api.Application.Services.Assistant.Triggers.NextPeriodAutonomyResolver>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentTriggerDetector, Klacks.Api.Application.Services.Assistant.Triggers.EvalRegressionDetector>();
        services.AddSingleton<Klacks.Api.Application.Interfaces.Assistant.INextPeriodAutoCommitService, Klacks.Api.Application.Services.Assistant.Triggers.NextPeriodAutoCommitService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IAgentSkillExecutionRepository, Klacks.Api.Infrastructure.Repositories.Assistant.AgentSkillExecutionRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientContractReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientContractReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientAvailabilityReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientAvailabilityReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IClientCoreDataReadRepository, Klacks.Api.Infrastructure.Repositories.Assistant.ClientCoreDataReadRepository>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISuggestionEntityNameReader, Klacks.Api.Infrastructure.Repositories.Assistant.SuggestionEntityNameReader>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISkillCoverageService, Klacks.Api.Application.Services.Assistant.Coverage.SkillCoverageService>();
    }

    private static void AddLLMPipelineServices(this IServiceCollection services)
    {
        services.AddScoped<IAutoMemoryExtractionService, AutoMemoryExtractionService>();
        services.AddScoped<ITurnReflectionService, TurnReflectionService>();
        services.AddScoped<IConversationCompactionService, ConversationCompactionService>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ICheapestModelResolver, Klacks.Api.Domain.Services.Assistant.CheapestModelResolver>();
        services.AddSingleton<Klacks.Api.Domain.Interfaces.Assistant.IContextBudgetPolicy, Klacks.Api.Domain.Services.Assistant.ContextBudgetPolicy>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IReadOnlyToolsetFilter, Klacks.Api.Application.Services.Assistant.ReadOnlyToolsetFilter>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IReadOnlyResearchService, Klacks.Api.Application.Services.Assistant.ReadOnlyResearchService>();
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
        services.AddScoped<IOneShotCompletionService, OneShotCompletionService>();
        services.AddScoped<LLMConversationManager>();
        services.AddScoped<TurnCompletionRecorder>();
        services.AddScoped<Klacks.Api.Domain.Models.Assistant.TurnRunState>();
        services.AddSkillToolsetServices();
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
    }

    private static void AddAssistantContextServices(this IServiceCollection services)
    {
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.ISuggestionsRanker,
            Klacks.Api.Application.Services.Assistant.SuggestionsRanker>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IOnboardingService,
            Klacks.Api.Application.Services.Assistant.OnboardingService>();
        services.AddScoped<Klacks.Api.Application.Interfaces.Assistant.IWelcomeFocusResolver,
            Klacks.Api.Application.Services.Assistant.WelcomeFocusResolver>();
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
        services.AddScoped<Klacks.Api.Domain.Interfaces.Assistant.IEffectiveTimeZoneResolver,
            Klacks.Api.Domain.Services.Assistant.Skills.EffectiveTimeZoneResolver>();
        services.AddScoped<Klacks.Api.Domain.Interfaces.Settings.IWeekConfiguration,
            Klacks.Api.Infrastructure.Services.WeekConfiguration>();
    }

    private static void AddSpeechServices(this IServiceCollection services)
    {
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

    internal static void AddLLMProviders(this IServiceCollection services)
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
}
