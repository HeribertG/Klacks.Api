// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class AgentSkillSeedService
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ILogger<AgentSkillSeedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly Dictionary<string, string> NavigateToRoutes = new()
    {
        { "dashboard", "/workplace/dashboard" },
        { "employees", "/workplace/client" },
        { "employee-details", "/workplace/client" },
        { "schedule", "/workplace/schedule" },
        { "absences", "/workplace/absence" },
        { "reports", "/workplace/dashboard" },
        { "settings", "/workplace/settings" },
        { "groups", "/workplace/group" },
        { "contracts", "/workplace/client" },
        { "holidays", "/workplace/settings" }
    };

    private const string NavigateToSkillName = "navigate_to";
    private const string UpdateGeneralSettingsName = "update_general_settings";
    private const string UpdateOwnerAddressName = "update_owner_address";
    private const string UpdateEmailSettingsName = "update_email_settings";
    private const string UpdateImapSettingsName = "update_imap_settings";
    private const string UpdateWorkSettingsName = "update_work_settings";
    private const string UpdateSchedulingDefaultsName = "update_scheduling_defaults";
    private const string UpdateDeeplSettingsName = "update_deepl_settings";
    private const string CreateLlmProviderName = "create_llm_provider";
    private const string UpdateLlmProviderName = "update_llm_provider";
    private const string DeleteLlmProviderName = "delete_llm_provider";
    private const string CreateLlmModelName = "create_llm_model";
    private const string UpdateLlmModelName = "update_llm_model";
    private const string DeleteLlmModelName = "delete_llm_model";
    private const string CreateSchedulingRuleName = "create_scheduling_rule";
    private const string UpdateSchedulingRuleName = "update_scheduling_rule";
    private const string DeleteSchedulingRuleName = "delete_scheduling_rule";
    private const string UpdateBranchName = "update_branch";
    private const string DefaultAgentName = "klacks-default";

    private static readonly HashSet<string> UiActionSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        NavigateToSkillName,
        UpdateGeneralSettingsName,
        UpdateOwnerAddressName,
        UpdateEmailSettingsName,
        UpdateImapSettingsName,
        UpdateWorkSettingsName,
        UpdateSchedulingDefaultsName,
        UpdateDeeplSettingsName,
        CreateLlmProviderName,
        UpdateLlmProviderName,
        DeleteLlmProviderName,
        CreateLlmModelName,
        UpdateLlmModelName,
        DeleteLlmModelName,
        CreateSchedulingRuleName,
        UpdateSchedulingRuleName,
        DeleteSchedulingRuleName,
        UpdateBranchName
    };

    private record DbOnlySkillDefinition(
        string Name,
        string Description,
        SkillCategory Category,
        string? RequiredPermission,
        SkillParameter[] Parameters);

    private static SkillParameter Param(string name, string desc, bool required = false) =>
        new(name, desc, SkillParameterType.String, required);

    private static DbOnlySkillDefinition[] GetDbOnlySkillDefinitions() =>
    [
        new(UpdateGeneralSettingsName,
            "Updates general application settings. Currently supports changing the application name. The application name is displayed in the browser title bar and the application header.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [Param("appName", "The new application name to set. This is displayed in the browser title and application header.", required: true)]),

        new(UpdateOwnerAddressName,
            "Updates the owner/company address in settings. IMPORTANT: state and country are REQUIRED and must always be provided.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("addressName", "Company or owner name"),
                Param("supplementAddress", "Additional address line"),
                Param("street", "Street and house number"),
                Param("zip", "Postal code / ZIP"),
                Param("city", "City name"),
                Param("state", "State or canton abbreviation (e.g. BE, ZH)", required: true),
                Param("country", "Country abbreviation (e.g. CH, DE, AT)", required: true),
                Param("phone", "Phone number"),
                Param("email", "Email address")
            ]),

        new(UpdateEmailSettingsName,
            "Updates the outgoing email (SMTP) settings. All parameters are optional - only provided values will be updated.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("outgoingServer", "SMTP server hostname"),
                Param("outgoingServerPort", "SMTP server port (e.g. 587, 465)"),
                Param("outgoingServerTimeout", "Connection timeout in milliseconds"),
                Param("enabledSSL", "Enable SSL/TLS (true or false)"),
                Param("authenticationType", "Authentication type (None, LOGIN, PLAIN, CRAM-MD5)"),
                Param("dispositionNotification", "Request disposition notification (true or false)"),
                Param("readReceipt", "Read receipt email address"),
                Param("replyTo", "Reply-to email address"),
                Param("mark", "Email mark/label"),
                Param("smtpUsername", "SMTP authentication username"),
                Param("smtpPassword", "SMTP authentication password")
            ]),

        new(UpdateImapSettingsName,
            "Updates the incoming email (IMAP) settings. All parameters are optional - only provided values will be updated.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("server", "IMAP server hostname"),
                Param("port", "IMAP server port (e.g. 993, 143)"),
                Param("enableSSL", "Enable SSL/TLS (true or false)"),
                Param("folder", "IMAP folder to monitor (e.g. INBOX)"),
                Param("pollInterval", "Poll interval in seconds"),
                Param("username", "IMAP authentication username"),
                Param("password", "IMAP authentication password")
            ]),

        new(UpdateWorkSettingsName,
            "Updates work-related settings. All parameters are optional - only provided values will be updated. Includes vacation days, probation/notice periods, payment interval, surcharge rates, and visibility ranges.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("vacationDaysPerYear", "Number of vacation days per year"),
                Param("probationPeriod", "Probation period in months"),
                Param("noticePeriod", "Notice period in months"),
                Param("paymentInterval", "Payment interval (e.g. monthly, weekly)"),
                Param("nightRate", "Surcharge rate for night shifts"),
                Param("holidayRate", "Surcharge rate for holidays"),
                Param("saRate", "Surcharge rate for saturdays"),
                Param("soRate", "Surcharge rate for sundays"),
                Param("dayVisibleBefore", "Days visible before current date"),
                Param("dayVisibleAfter", "Days visible after current date")
            ]),

        new(UpdateSchedulingDefaultsName,
            "Updates scheduling default settings. All parameters are optional - only provided values will be updated.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("defaultWorkingHours", "Default working hours per day"),
                Param("overtimeThreshold", "Threshold for overtime calculation"),
                Param("guaranteedHours", "Guaranteed minimum hours"),
                Param("maximumHours", "Maximum allowed hours"),
                Param("minimumHours", "Minimum required hours"),
                Param("fullTime", "Full-time hours threshold"),
                Param("schedulingMaxWorkDays", "Maximum consecutive work days per scheduling period"),
                Param("schedulingMinRestDays", "Minimum rest days between work periods"),
                Param("schedulingMinPauseHours", "Minimum pause hours between shifts"),
                Param("schedulingMaxOptimalGap", "Maximum optimal gap between shifts in hours"),
                Param("schedulingMaxDailyHours", "Maximum daily working hours for scheduling"),
                Param("schedulingMaxWeeklyHours", "Maximum weekly working hours for scheduling"),
                Param("schedulingMaxConsecutiveDays", "Maximum consecutive working days"),
                Param("nightRate", "Surcharge rate for night shifts"),
                Param("holidayRate", "Surcharge rate for holidays"),
                Param("saRate", "Surcharge rate for saturdays"),
                Param("soRate", "Surcharge rate for sundays")
            ]),

        new(UpdateDeeplSettingsName,
            "Updates the DeepL translation API key. The API key is stored encrypted in settings.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [Param("apiKey", "The DeepL API key for translation services", required: true)]),

        new(CreateLlmProviderName,
            "Creates a new LLM provider configuration. The provider will be visible in Settings > LLM Providers.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("providerId", "Unique provider identifier (e.g., openai, anthropic, deepseek)", required: true),
                Param("providerName", "Display name of the provider", required: true),
                Param("baseUrl", "Base URL for the provider API"),
                Param("apiVersion", "API version string"),
                Param("priority", "Provider priority (lower number = higher priority)"),
                Param("apiKey", "API key for the provider"),
                Param("isEnabled", "Whether provider is enabled (true/false), defaults to true")
            ]),

        new(UpdateLlmProviderName,
            "Updates an existing LLM provider configuration. Only provided values will be changed.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("providerId", "The provider ID to update", required: true),
                Param("providerName", "Display name of the provider"),
                Param("baseUrl", "Base URL for the provider API"),
                Param("apiVersion", "API version string"),
                Param("priority", "Provider priority (lower number = higher priority)"),
                Param("apiKey", "API key for the provider"),
                Param("isEnabled", "Whether provider is enabled (true/false)")
            ]),

        new(DeleteLlmProviderName,
            "Deletes an LLM provider configuration.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [Param("providerId", "The provider ID to delete", required: true)]),

        new(CreateLlmModelName,
            "Creates a new LLM model configuration. The model will be available in the chat model selector.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("modelId", "Unique model identifier", required: true),
                Param("modelName", "Display name", required: true),
                Param("providerId", "Provider ID this model belongs to", required: true),
                Param("apiModelId", "API model identifier (defaults to modelId)"),
                Param("description", "Model description"),
                Param("contextWindow", "Context window size (default 128000)"),
                Param("maxTokens", "Max output tokens (default 4096)"),
                Param("costPerInputToken", "Cost per 1K input tokens"),
                Param("costPerOutputToken", "Cost per 1K output tokens"),
                Param("apiKey", "Model-specific API key override"),
                Param("isEnabled", "Enabled (true/false, default true)"),
                Param("isDefault", "Set as default model (true/false, default false)")
            ]),

        new(UpdateLlmModelName,
            "Updates an existing LLM model configuration. Only provided values will be changed.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("modelId", "The model ID to update", required: true),
                Param("modelName", "Display name"),
                Param("providerId", "Provider ID this model belongs to"),
                Param("apiModelId", "API model identifier"),
                Param("description", "Model description"),
                Param("contextWindow", "Context window size"),
                Param("maxTokens", "Max output tokens"),
                Param("costPerInputToken", "Cost per 1K input tokens"),
                Param("costPerOutputToken", "Cost per 1K output tokens"),
                Param("apiKey", "Model-specific API key override"),
                Param("isEnabled", "Enabled (true/false)"),
                Param("isDefault", "Set as default model (true/false)")
            ]),

        new(DeleteLlmModelName,
            "Deletes an LLM model configuration.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [Param("modelId", "The model ID to delete", required: true)]),

        new(CreateSchedulingRuleName,
            "Creates a new scheduling rule with work time limits and surcharge rates.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("name", "Name of the scheduling rule", required: true),
                Param("maxWorkDays", "Maximum work days per week"),
                Param("minRestDays", "Minimum rest days per week"),
                Param("minPauseHours", "Minimum pause hours between shifts"),
                Param("maxOptimalGap", "Maximum optimal gap between shifts"),
                Param("maxDailyHours", "Maximum daily working hours"),
                Param("maxWeeklyHours", "Maximum weekly working hours"),
                Param("maxConsecutiveDays", "Maximum consecutive working days"),
                Param("defaultWorkingHours", "Default working hours per day"),
                Param("overtimeThreshold", "Overtime threshold in hours"),
                Param("guaranteedHours", "Guaranteed minimum hours"),
                Param("maximumHours", "Maximum allowed hours"),
                Param("minimumHours", "Minimum required hours"),
                Param("fullTimeHours", "Full-time hours definition"),
                Param("vacationDaysPerYear", "Vacation days per year"),
                Param("nightRate", "Night surcharge rate (0-1)"),
                Param("holidayRate", "Holiday surcharge rate (0-1)"),
                Param("saRate", "Saturday surcharge rate (0-1)"),
                Param("soRate", "Sunday surcharge rate (0-1)")
            ]),

        new(UpdateSchedulingRuleName,
            "Updates an existing scheduling rule. Only provided values will be changed.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("ruleId", "The scheduling rule ID to update", required: true),
                Param("name", "Name of the scheduling rule"),
                Param("maxWorkDays", "Maximum work days per week"),
                Param("minRestDays", "Minimum rest days per week"),
                Param("minPauseHours", "Minimum pause hours between shifts"),
                Param("maxOptimalGap", "Maximum optimal gap between shifts"),
                Param("maxDailyHours", "Maximum daily working hours"),
                Param("maxWeeklyHours", "Maximum weekly working hours"),
                Param("maxConsecutiveDays", "Maximum consecutive working days"),
                Param("defaultWorkingHours", "Default working hours per day"),
                Param("overtimeThreshold", "Overtime threshold in hours"),
                Param("guaranteedHours", "Guaranteed minimum hours"),
                Param("maximumHours", "Maximum allowed hours"),
                Param("minimumHours", "Minimum required hours"),
                Param("fullTimeHours", "Full-time hours definition"),
                Param("vacationDaysPerYear", "Vacation days per year"),
                Param("nightRate", "Night surcharge rate (0-1)"),
                Param("holidayRate", "Holiday surcharge rate (0-1)"),
                Param("saRate", "Saturday surcharge rate (0-1)"),
                Param("soRate", "Sunday surcharge rate (0-1)")
            ]),

        new(DeleteSchedulingRuleName,
            "Deletes a scheduling rule.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [Param("ruleId", "The scheduling rule ID to delete", required: true)]),

        new(UpdateBranchName,
            "Updates an existing branch/location. Only provided values will be changed.",
            SkillCategory.Crud, Permissions.CanEditSettings,
            [
                Param("branchId", "The branch ID to update", required: true),
                Param("name", "New branch name"),
                Param("address", "New branch address"),
                Param("phone", "New phone number"),
                Param("email", "New email address")
            ])
    ];

    public AgentSkillSeedService(
        ISkillRegistry skillRegistry,
        IAgentRepository agentRepository,
        IAgentSkillRepository agentSkillRepository,
        ILogger<AgentSkillSeedService> logger)
    {
        _skillRegistry = skillRegistry;
        _agentRepository = agentRepository;
        _agentSkillRepository = agentSkillRepository;
        _logger = logger;
    }

    private void RegisterDbOnlySkills()
    {
        foreach (var definition in GetDbOnlySkillDefinitions())
        {
            if (_skillRegistry.GetSkillByName(definition.Name) != null)
                continue;

            var descriptor = new SkillDescriptor(
                definition.Name,
                definition.Description,
                definition.Category,
                definition.Parameters,
                definition.RequiredPermission != null ? [definition.RequiredPermission] : [],
                [],
                null);

            _skillRegistry.Register(descriptor);
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var agent = await EnsureDefaultAgentAsync(cancellationToken);

        RegisterDbOnlySkills();

        var descriptors = _skillRegistry.GetAllSkills();

        if (descriptors.Count == 0)
        {
            _logger.LogWarning("No skills registered in SkillRegistry. Skipping seed.");
            return;
        }

        var existingSkills = await _agentSkillRepository.GetAllByAgentIdAsync(agent.Id, cancellationToken);
        var existingByName = existingSkills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;

        foreach (var descriptor in descriptors)
        {
            if (existingByName.TryGetValue(descriptor.Name, out var existing))
            {
                UpdateExistingSkill(existing, descriptor);
                await _agentSkillRepository.UpdateAsync(existing, cancellationToken);
                updated++;
            }
            else
            {
                var newSkill = CreateNewSkill(agent.Id, descriptor);
                await _agentSkillRepository.AddAsync(newSkill, cancellationToken);
                inserted++;
            }
        }

        _logger.LogInformation(
            "Seeded {Total} skills for default agent (inserted: {Inserted}, updated: {Updated})",
            descriptors.Count, inserted, updated);
    }

    private async Task<Agent> EnsureDefaultAgentAsync(CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent != null)
            return agent;

        agent = new Agent
        {
            Name = DefaultAgentName,
            DisplayName = "Klacks Assistant",
            Description = "Default AI assistant",
            IsActive = true,
            IsDefault = true
        };

        await _agentRepository.AddAsync(agent, cancellationToken);
        _logger.LogInformation("Created default agent: {AgentName} ({AgentId})", agent.Name, agent.Id);
        return agent;
    }

    private AgentSkill CreateNewSkill(Guid agentId, SkillDescriptor descriptor)
    {
        var skill = new AgentSkill
        {
            AgentId = agentId,
            Name = descriptor.Name,
            Description = descriptor.Description,
            ParametersJson = SerializeParameters(descriptor.Parameters),
            RequiredPermission = descriptor.RequiredPermissions.FirstOrDefault(),
            ExecutionType = MapExecutionType(descriptor),
            Category = descriptor.Category.ToString().ToLowerInvariant()
        };

        var handlerConfig = GetDefaultHandlerConfig(descriptor.Name);
        if (handlerConfig != null)
        {
            skill.HandlerConfig = handlerConfig;
        }

        return skill;
    }

    private void UpdateExistingSkill(AgentSkill existing, SkillDescriptor descriptor)
    {
        existing.Description = descriptor.Description;
        existing.ParametersJson = SerializeParameters(descriptor.Parameters);
        existing.RequiredPermission = descriptor.RequiredPermissions.FirstOrDefault();
        existing.Category = descriptor.Category.ToString().ToLowerInvariant();
        existing.ExecutionType = MapExecutionType(descriptor);

        var handlerConfig = GetDefaultHandlerConfig(existing.Name);
        if (handlerConfig != null)
        {
            if (UiActionSkills.Contains(existing.Name) || existing.HandlerConfig == "{}" || string.IsNullOrEmpty(existing.HandlerConfig))
            {
                existing.HandlerConfig = handlerConfig;
            }
        }
    }

    private static string MapExecutionType(SkillDescriptor descriptor)
    {
        if (UiActionSkills.Contains(descriptor.Name))
            return LlmExecutionTypes.UiAction;

        if (descriptor.Category == SkillCategory.UI)
            return LlmExecutionTypes.FrontendOnly;

        return LlmExecutionTypes.Skill;
    }

    private static string? GetDefaultHandlerConfig(string skillName)
    {
        if (skillName.Equals(NavigateToSkillName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", routeMap = NavigateToRoutes, routeKeyFrom = "params.page", appendParamFrom = "params.entityId" }
                }
            });

        if (skillName.Equals(UpdateGeneralSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "setting-general-name", timeout = 5000 },
                    new { action = "setValue", selector = "setting-general-name", valueFrom = "params.appName" }
                }
            });

        if (skillName.Equals(UpdateOwnerAddressName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "setting-owner-address-name", timeout = 5000 },
                    new { action = "delay", delay = 1000 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "addressName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-name", valueFrom = "params.addressName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "phone", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-tel", valueFrom = "params.phone" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "supplementAddress", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-supplement", valueFrom = "params.supplementAddress" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "email", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-email", valueFrom = "params.email" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "street", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-street", valueFrom = "params.street" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "zip", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-zip", valueFrom = "params.zip" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "city", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-city", valueFrom = "params.city" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "country", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "setting-owner-address-country", valueFrom = "params.country" } } },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "state", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "setting-owner-address-state", valueFrom = "params.state" } } }
                }
            });

        if (skillName.Equals(UpdateEmailSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "outgoingServer", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-email-setting" },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "outgoingServer", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "outgoingServer", valueFrom = "params.outgoingServer" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "outgoingServerPort", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "outgoingServerPort", valueFrom = "params.outgoingServerPort" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "outgoingServerTimeout", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "outgoingServerTimeout", valueFrom = "params.outgoingServerTimeout" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "enabledSSL", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "enabledSSL", valueFrom = "params.enabledSSL" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "authenticationType", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "authenticationType", valueFrom = "params.authenticationType" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "dispositionNotification", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "dispositionNotification", valueFrom = "params.dispositionNotification" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "readReceipt", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "readReceipt", valueFrom = "params.readReceipt" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "replyTo", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "replyTo", valueFrom = "params.replyTo" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "mark", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "mark", valueFrom = "params.mark" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "smtpUsername", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "outgoingServerAuthUser", valueFrom = "params.smtpUsername" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "smtpPassword", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "outgoingServerAuthKey", valueFrom = "params.smtpPassword" } } }
                }
            });

        if (skillName.Equals(UpdateImapSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "imapServer", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-imap-setting" },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "server", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapServer", valueFrom = "params.server" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "port", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapPort", valueFrom = "params.port" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "enableSSL", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "imapEnableSSL", valueFrom = "params.enableSSL" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "folder", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapFolder", valueFrom = "params.folder" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "pollInterval", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapPollInterval", valueFrom = "params.pollInterval" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "username", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapUsername", valueFrom = "params.username" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "password", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "imapPassword", valueFrom = "params.password" } } }
                }
            });

        if (skillName.Equals(UpdateWorkSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "vacationDaysPerYear", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-work-setting" },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "vacationDaysPerYear", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "vacationDaysPerYear", valueFrom = "params.vacationDaysPerYear" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "probationPeriod", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "probationPeriod", valueFrom = "params.probationPeriod" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "noticePeriod", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "noticePeriod", valueFrom = "params.noticePeriod" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "paymentInterval", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "paymentInterval", valueFrom = "params.paymentInterval" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "nightRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "nightRate", valueFrom = "params.nightRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "holidayRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "holidayRate", valueFrom = "params.holidayRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "saRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "saRate", valueFrom = "params.saRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "soRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "soRate", valueFrom = "params.soRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "dayVisibleBefore", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "dayVisibleBefore", valueFrom = "params.dayVisibleBefore" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "dayVisibleAfter", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "dayVisibleAfter", valueFrom = "params.dayVisibleAfter" } } }
                }
            });

        if (skillName.Equals(UpdateSchedulingDefaultsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "sdDefaultWorkingHours", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-scheduling-defaults" },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "defaultWorkingHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdDefaultWorkingHours", valueFrom = "params.defaultWorkingHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "overtimeThreshold", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdOvertimeThreshold", valueFrom = "params.overtimeThreshold" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "guaranteedHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdGuaranteedHours", valueFrom = "params.guaranteedHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maximumHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdMaximumHours", valueFrom = "params.maximumHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minimumHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdMinimumHours", valueFrom = "params.minimumHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "fullTime", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdFullTime", valueFrom = "params.fullTime" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMaxWorkDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMaxWorkDays", valueFrom = "params.schedulingMaxWorkDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMinRestDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMinRestDays", valueFrom = "params.schedulingMinRestDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMinPauseHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMinPauseHours", valueFrom = "params.schedulingMinPauseHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMaxOptimalGap", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMaxOptimalGap", valueFrom = "params.schedulingMaxOptimalGap" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMaxDailyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMaxDailyHours", valueFrom = "params.schedulingMaxDailyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMaxWeeklyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMaxWeeklyHours", valueFrom = "params.schedulingMaxWeeklyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "schedulingMaxConsecutiveDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSchedulingMaxConsecutiveDays", valueFrom = "params.schedulingMaxConsecutiveDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "nightRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdNightRate", valueFrom = "params.nightRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "holidayRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdHolidayRate", valueFrom = "params.holidayRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "saRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSaRate", valueFrom = "params.saRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "soRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "sdSoRate", valueFrom = "params.soRate" } } }
                }
            });

        if (skillName.Equals(UpdateDeeplSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "deepl-apikey", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-deepl" },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiKey", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "deepl-apikey", valueFrom = "params.apiKey" } } }
                }
            });

        if (skillName.Equals(CreateLlmProviderName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-providers-add-btn", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-providers" },
                    new { action = "delay", delay = 300 },
                    new { action = "click", selector = "llm-providers-add-btn" },
                    new { action = "waitForElement", selector = "llm-providers-modal-provider-id", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "providerId", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-provider-id", valueFrom = "params.providerId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "providerName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-provider-name", valueFrom = "params.providerName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "baseUrl", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-base-url", valueFrom = "params.baseUrl" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiVersion", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-api-version", valueFrom = "params.apiVersion" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "priority", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-priority", valueFrom = "params.priority" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiKey", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-api-key", valueFrom = "params.apiKey" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "isEnabled", @operator = "==" }, thenSteps = new object[] { new { action = "click", selector = "llm-providers-modal-is-enabled" } } },
                    new { action = "click", selector = "llm-providers-modal-save-btn" }
                }
            });

        if (skillName.Equals(UpdateLlmProviderName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-providers-row-display-{providerId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-providers" },
                    new { action = "click", selector = "llm-providers-row-display-{providerId}" },
                    new { action = "waitForElement", selector = "llm-providers-modal-provider-name", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "providerName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-provider-name", valueFrom = "params.providerName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "baseUrl", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-base-url", valueFrom = "params.baseUrl" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiVersion", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-api-version", valueFrom = "params.apiVersion" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "priority", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-priority", valueFrom = "params.priority" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiKey", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-providers-modal-api-key", valueFrom = "params.apiKey" } } },
                    new { action = "click", selector = "llm-providers-modal-save-btn" }
                }
            });

        if (skillName.Equals(DeleteLlmProviderName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-providers-row-delete-{providerId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-providers" },
                    new { action = "click", selector = "llm-providers-row-delete-{providerId}" }
                }
            });

        if (skillName.Equals(CreateLlmModelName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-models-add-btn", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-models" },
                    new { action = "delay", delay = 300 },
                    new { action = "click", selector = "llm-models-add-btn" },
                    new { action = "waitForElement", selector = "llm-models-modal-model-id", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "modelId", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-model-id", valueFrom = "params.modelId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "modelName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-model-name", valueFrom = "params.modelName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "providerId", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "llm-models-modal-provider", valueFrom = "params.providerId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "description", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-description", valueFrom = "params.description" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiModelId", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-api-model-id", valueFrom = "params.apiModelId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "contextWindow", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-context-window", valueFrom = "params.contextWindow" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxTokens", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-max-tokens", valueFrom = "params.maxTokens" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "costPerInputToken", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-input-cost", valueFrom = "params.costPerInputToken" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "costPerOutputToken", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-output-cost", valueFrom = "params.costPerOutputToken" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiKey", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-api-key", valueFrom = "params.apiKey" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "isEnabled", @operator = "==" }, thenSteps = new object[] { new { action = "click", selector = "llm-models-modal-is-enabled" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "isDefault", @operator = "==" }, thenSteps = new object[] { new { action = "click", selector = "llm-models-modal-is-default" } } },
                    new { action = "click", selector = "llm-models-modal-save-btn" }
                }
            });

        if (skillName.Equals(UpdateLlmModelName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-models-row-display-{modelId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-models" },
                    new { action = "click", selector = "llm-models-row-display-{modelId}" },
                    new { action = "waitForElement", selector = "llm-models-modal-model-name", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "modelName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-model-name", valueFrom = "params.modelName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "providerId", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "llm-models-modal-provider", valueFrom = "params.providerId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "description", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-description", valueFrom = "params.description" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiModelId", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-api-model-id", valueFrom = "params.apiModelId" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "contextWindow", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-context-window", valueFrom = "params.contextWindow" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxTokens", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-max-tokens", valueFrom = "params.maxTokens" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "costPerInputToken", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-input-cost", valueFrom = "params.costPerInputToken" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "costPerOutputToken", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-output-cost", valueFrom = "params.costPerOutputToken" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "apiKey", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "llm-models-modal-api-key", valueFrom = "params.apiKey" } } },
                    new { action = "click", selector = "llm-models-modal-save-btn" }
                }
            });

        if (skillName.Equals(DeleteLlmModelName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "llm-models-row-delete-{modelId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-llm-models" },
                    new { action = "click", selector = "llm-models-row-delete-{modelId}" }
                }
            });

        if (skillName.Equals(CreateSchedulingRuleName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "scheduling-rules-add-btn", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-scheduling-rules" },
                    new { action = "delay", delay = 300 },
                    new { action = "click", selector = "scheduling-rules-add-btn" },
                    new { action = "waitForElement", selector = "ruleName", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "name", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleName", valueFrom = "params.name" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxWorkDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxWorkDays", valueFrom = "params.maxWorkDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minRestDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "minRestDays", valueFrom = "params.minRestDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minPauseHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "minPauseHours", valueFrom = "params.minPauseHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxOptimalGap", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxOptimalGap", valueFrom = "params.maxOptimalGap" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxDailyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxDailyHours", valueFrom = "params.maxDailyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxWeeklyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxWeeklyHours", valueFrom = "params.maxWeeklyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxConsecutiveDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxConsecutiveDays", valueFrom = "params.maxConsecutiveDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "defaultWorkingHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleDefaultWorkingHours", valueFrom = "params.defaultWorkingHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "overtimeThreshold", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleOvertimeThreshold", valueFrom = "params.overtimeThreshold" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "guaranteedHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleGuaranteedHours", valueFrom = "params.guaranteedHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maximumHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleMaximumHours", valueFrom = "params.maximumHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minimumHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleMinimumHours", valueFrom = "params.minimumHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "fullTimeHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleFullTimeHours", valueFrom = "params.fullTimeHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "vacationDaysPerYear", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleVacationDaysPerYear", valueFrom = "params.vacationDaysPerYear" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "nightRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleNightRate", valueFrom = "params.nightRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "holidayRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleHolidayRate", valueFrom = "params.holidayRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "saRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleSaRate", valueFrom = "params.saRate" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "soRate", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleSoRate", valueFrom = "params.soRate" } } },
                    new { action = "click", selector = "scheduling-rule-modal-save-btn" }
                }
            });

        if (skillName.Equals(UpdateSchedulingRuleName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "scheduling-rule-row-name-{ruleId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-scheduling-rules" },
                    new { action = "click", selector = "scheduling-rule-row-name-{ruleId}" },
                    new { action = "waitForElement", selector = "ruleName", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "name", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "ruleName", valueFrom = "params.name" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxWorkDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxWorkDays", valueFrom = "params.maxWorkDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minRestDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "minRestDays", valueFrom = "params.minRestDays" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "minPauseHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "minPauseHours", valueFrom = "params.minPauseHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxOptimalGap", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxOptimalGap", valueFrom = "params.maxOptimalGap" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxDailyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxDailyHours", valueFrom = "params.maxDailyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxWeeklyHours", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxWeeklyHours", valueFrom = "params.maxWeeklyHours" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "maxConsecutiveDays", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "maxConsecutiveDays", valueFrom = "params.maxConsecutiveDays" } } },
                    new { action = "click", selector = "scheduling-rule-modal-save-btn" }
                }
            });

        if (skillName.Equals(DeleteSchedulingRuleName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "scheduling-rule-row-delete-{ruleId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-scheduling-rules" },
                    new { action = "click", selector = "scheduling-rule-row-delete-{ruleId}" }
                }
            });

        if (skillName.Equals(UpdateBranchName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "branches-row-name-{branchId}", timeout = 5000 },
                    new { action = "scrollTo", selector = "settings-branches" },
                    new { action = "click", selector = "branches-row-name-{branchId}" },
                    new { action = "waitForElement", selector = "branches-modal-name", timeout = 3000 },
                    new { action = "delay", delay = 300 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "name", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "branches-modal-name", valueFrom = "params.name" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "address", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "branches-modal-address", valueFrom = "params.address" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "phone", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "branches-modal-phone", valueFrom = "params.phone" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "email", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "branches-modal-email", valueFrom = "params.email" } } },
                    new { action = "click", selector = "branches-modal-save-btn" }
                }
            });

        return null;
    }

    private static string SerializeParameters(IReadOnlyList<SkillParameter> parameters)
    {
        var paramDtos = parameters.Select(p => new
        {
            name = p.Name,
            type = SkillParameterTypeMapping.ToJsonSchemaType(p.Type),
            description = p.Description,
            required = p.Required,
            enumValues = p.EnumValues
        });

        return JsonSerializer.Serialize(paramDtos, JsonOptions);
    }

    private static string SerializeHandlerConfig(object config)
    {
        return JsonSerializer.Serialize(config, JsonOptions);
    }
}
