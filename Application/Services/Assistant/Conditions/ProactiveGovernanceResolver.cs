// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IProactiveGovernanceResolver"/>. Precedence, strongest first: the global kill
/// switch pins everything to Hint, then a disabled kind pins itself to Hint, then the stored MaxAction
/// applies. A scope-specific rule wins over the installation-wide one, and a kind with no stored rule
/// resolves to the fail-safe defaults - so a trigger kind introduced in a later stage reports and waits
/// instead of inheriting somebody else's permission. The kill switch gates the ACTION branch only;
/// notifications keep flowing, because mute and snooze are per-user notification gates that the plan
/// forbids reusing as action gates.
/// </summary>
/// <param name="repository">Reads the stored governance rules.</param>
/// <param name="settingsReader">Reads the global kill-switch setting row.</param>
/// <param name="logger">Reports an unparseable kill-switch value.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ProactiveGovernanceResolver : IProactiveGovernanceResolver
{
    private const bool KillSwitchDefault = false;
    private const string UnparseableKillSwitchMessage =
        "Unrecognized value '{Value}' in setting {SettingKey}; treating the kill switch as {Fallback}";

    private readonly IAgentTriggerGovernanceRepository _repository;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<ProactiveGovernanceResolver> _logger;

    public ProactiveGovernanceResolver(
        IAgentTriggerGovernanceRepository repository,
        ISettingsReader settingsReader,
        ILogger<ProactiveGovernanceResolver> logger)
    {
        _repository = repository;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<ProactiveGovernanceDecision> ResolveAsync(
        string triggerKind, Guid? groupId, CancellationToken cancellationToken)
    {
        var killSwitchActive = await IsKillSwitchActiveAsync(cancellationToken);

        var stored = groupId is null
            ? null
            : await _repository.FindAsync(triggerKind, groupId, cancellationToken);

        stored ??= await _repository.FindAsync(triggerKind, null, cancellationToken);

        return Decide(triggerKind, groupId, stored, killSwitchActive);
    }

    public async Task<IReadOnlyList<ProactiveGovernanceDecision>> ResolveAllAsync(
        CancellationToken cancellationToken)
    {
        var killSwitchActive = await IsKillSwitchActiveAsync(cancellationToken);
        var stored = await _repository.GetAllAsync(cancellationToken);
        var globalRules = stored
            .Where(rule => rule.GroupId is null)
            .ToDictionary(rule => rule.TriggerKind, StringComparer.Ordinal);

        return ProactiveGovernanceDefaults.GovernedKinds
            .Select(kind => Decide(
                kind,
                null,
                globalRules.GetValueOrDefault(kind),
                killSwitchActive))
            .ToList();
    }

    public async Task<bool> IsKillSwitchActiveAsync(CancellationToken cancellationToken)
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.KlacksyProactiveKillSwitch);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return KillSwitchDefault;
        }

        if (bool.TryParse(setting.Value, out var active))
        {
            return active;
        }

        _logger.LogWarning(
            UnparseableKillSwitchMessage,
            setting.Value,
            SettingKeys.KlacksyProactiveKillSwitch,
            KillSwitchDefault);
        return KillSwitchDefault;
    }

    private static ProactiveGovernanceDecision Decide(
        string triggerKind,
        Guid? groupId,
        AgentTriggerGovernance? stored,
        bool killSwitchActive)
    {
        var configuredMaxAction = stored?.MaxAction ?? ProactiveGovernanceDefaults.MaxAction;
        var enabled = stored?.Enabled ?? ProactiveGovernanceDefaults.Enabled;
        var effectiveMaxAction = killSwitchActive || !enabled
            ? ProactiveMaxAction.Hint
            : configuredMaxAction;

        return new ProactiveGovernanceDecision(
            TriggerKind: triggerKind,
            GroupId: groupId,
            EffectiveMaxAction: effectiveMaxAction,
            ConfiguredMaxAction: configuredMaxAction,
            Enabled: enabled,
            KillSwitchActive: killSwitchActive,
            ResponsibleOwnerUserId: stored?.ResponsibleOwnerUserId,
            DailyActionBudget: stored?.DailyActionBudget ?? ProactiveGovernanceDefaults.DailyActionBudget,
            WindowActionLimit: stored?.WindowActionLimit ?? ProactiveGovernanceDefaults.WindowActionLimit,
            WindowMinutes: stored?.WindowMinutes ?? ProactiveGovernanceDefaults.WindowMinutes,
            IsStored: stored is not null);
    }
}
