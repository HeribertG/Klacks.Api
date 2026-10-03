// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningRuleSetLoader"/>. Unites the approved CounterRule rows (PeriodCountRule, severity
/// from Enforcement ?? the global counterRule mode) and the approved PlanningConstraint rows valid in the
/// period with the requested AnalyseToken (IS NOT DISTINCT FROM). Every scope becomes the set of requested
/// agents it covers: Global = null (every agent), Client = that client, Group = members of the group and its
/// subgroups whose membership overlaps the period, SchedulingRule = agents whose effective contract on the
/// first period day references the rule (the CounterRuleEvaluator industry axis). A scope that covers none
/// of the requested agents drops the rule - an empty set never turns into "every agent". An approved row that
/// fails validation (manual DB edit, schema without upgrade step) fails closed when it is Hard: the load throws
/// PlanningRuleConfigurationException, because silently dropping a binding rule would let every engine accept
/// plans that break it; an invalid Soft row is logged as an error and reported in PlanningRuleSet.SkippedRuleIds,
/// since losing a preference must not stop planning. Night window and
/// workload come from EffectiveContractData on the first period day (one batched resolution). Without any
/// applicable rule neither the contracts nor the carry-in are read; the returned agents then carry the default
/// night window and full workload, which no evaluation uses.
/// </summary>
/// <param name="counterRuleRepository">Approved CounterRule rows</param>
/// <param name="constraintRepository">Approved PlanningConstraint rows of the period</param>
/// <param name="constraintValidator">Parses ParametersJson into typed parameters</param>
/// <param name="enforcementResolver">Global counterRule warn/block mode</param>
/// <param name="contractDataProvider">Effective contract data (scheduling rule, night window, workload)</param>
/// <param name="groupHierarchy">Expands a group into itself plus its subgroups</param>
/// <param name="dataReader">Group memberships of the requested agents</param>
/// <param name="carryInLoader">Worked segments outside the period</param>
/// <param name="settingsReader">Reads NIGHT_RULE_MIN_OVERLAP_MINUTES for the night classification of the sequence rules</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.ScheduleOptimizer.Constraints.Rules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public sealed class PlanningRuleSetLoader : IPlanningRuleSetLoader
{
    private readonly ICounterRuleRepository _counterRuleRepository;
    private readonly IPlanningConstraintRepository _constraintRepository;
    private readonly IPlanningConstraintValidator _constraintValidator;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupHierarchy;
    private readonly IPlanningRuleDataReader _dataReader;
    private readonly IPlanningRuleCarryInLoader _carryInLoader;
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<PlanningRuleSetLoader> _logger;

    public PlanningRuleSetLoader(
        ICounterRuleRepository counterRuleRepository,
        IPlanningConstraintRepository constraintRepository,
        IPlanningConstraintValidator constraintValidator,
        IComplianceEnforcementResolver enforcementResolver,
        IClientContractDataProvider contractDataProvider,
        IGetAllClientIdsFromGroupAndSubgroups groupHierarchy,
        IPlanningRuleDataReader dataReader,
        IPlanningRuleCarryInLoader carryInLoader,
        ISettingsReader settingsReader,
        ILogger<PlanningRuleSetLoader> logger)
    {
        _counterRuleRepository = counterRuleRepository;
        _constraintRepository = constraintRepository;
        _constraintValidator = constraintValidator;
        _enforcementResolver = enforcementResolver;
        _contractDataProvider = contractDataProvider;
        _groupHierarchy = groupHierarchy;
        _dataReader = dataReader;
        _carryInLoader = carryInLoader;
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlanRule>> LoadAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        var agents = DistinctAgents(agentIds);
        cancellationToken.ThrowIfCancellationRequested();
        var (rules, _) = await LoadRulesAsync(agents, from, until, analyseToken, PlanningRuleSources.All, [], cancellationToken);
        return rules;
    }

    public Task<PlanningRuleSet> LoadRuleSetAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        int coveredBoundaryDays,
        CancellationToken cancellationToken = default)
        => LoadRuleSetAsync(agentIds, from, until, analyseToken, coveredBoundaryDays, PlanningRuleSources.All, cancellationToken);

    public async Task<PlanningRuleSet> LoadRuleSetAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        int coveredBoundaryDays,
        PlanningRuleSources sources,
        CancellationToken cancellationToken = default)
    {
        var agents = DistinctAgents(agentIds);
        cancellationToken.ThrowIfCancellationRequested();
        var skippedRuleIds = new List<Guid>();
        var (rules, contractData) = await LoadRulesAsync(agents, from, until, analyseToken, sources, skippedRuleIds, cancellationToken);
        if (rules.Count == 0)
        {
            // Nothing will be evaluated: skip the contract resolution and the carry-in read, which every write gate
            // and live check would otherwise pay while an installation has no planning rule at all.
            return new PlanningRuleSet(rules, agents.Select(id => ToRuleAgent(id, null, PlanningConstraintDefaults.DefaultNightRuleMinOverlapMinutes)).ToList(), [], skippedRuleIds);
        }

        contractData ??= await _contractDataProvider.GetEffectiveContractDataForClientsAsync(agents, from);
        var nightMinOverlap = await ReadNightRuleMinOverlapAsync();
        var ruleAgents = agents.Select(id => ToRuleAgent(id, contractData, nightMinOverlap)).ToList();
        var carryIn = await _carryInLoader.LoadAsync(agents, from, until, rules, analyseToken, coveredBoundaryDays, cancellationToken);
        return new PlanningRuleSet(rules, ruleAgents, carryIn, skippedRuleIds);
    }

    private async Task<(IReadOnlyList<PlanRule> Rules, Dictionary<Guid, EffectiveContractData>? ContractData)> LoadRulesAsync(
        List<Guid> agents,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        PlanningRuleSources sources,
        List<Guid> skippedRuleIds,
        CancellationToken cancellationToken)
    {
        if (agents.Count == 0 || until < from)
        {
            return ([], null);
        }

        var counterRules = sources.HasFlag(PlanningRuleSources.CounterRules)
            ? await _counterRuleRepository.GetAllApprovedAsync(cancellationToken)
            : [];
        var constraints = sources.HasFlag(PlanningRuleSources.PlanningConstraints)
            ? await _constraintRepository.GetApprovedForPeriodAsync(from, until, analyseToken, cancellationToken)
            : [];
        if (counterRules.Count == 0 && constraints.Count == 0)
        {
            return ([], null);
        }

        var contractData = await LoadContractDataForScopesAsync(agents, from, counterRules, constraints);
        var groupMembers = await ResolveGroupMembersAsync(agents, from, until, analyseToken, constraints, cancellationToken);
        var rules = new List<PlanRule>(counterRules.Count + constraints.Count);

        if (counterRules.Count > 0)
        {
            var globalMode = await _enforcementResolver.GetModeAsync(ComplianceRuleNames.CounterRule);
            foreach (var rule in counterRules)
            {
                var scope = rule.SchedulingRuleId.HasValue
                    ? SchedulingRuleScope(agents, rule.SchedulingRuleId.Value, contractData)
                    : null;
                if (scope is not { Count: 0 })
                {
                    rules.Add(PlanningRuleMapper.FromCounterRule(rule, globalMode, scope));
                }
            }
        }

        foreach (var constraint in constraints)
        {
            var rule = MapConstraint(constraint, agents, contractData, groupMembers, skippedRuleIds);
            if (rule is not null)
            {
                rules.Add(rule);
            }
        }

        return (rules, contractData);
    }

    private PlanRule? MapConstraint(
        PlanningConstraint constraint,
        List<Guid> agents,
        Dictionary<Guid, EffectiveContractData>? contractData,
        IReadOnlyDictionary<Guid, HashSet<Guid>> groupMembers,
        List<Guid> skippedRuleIds)
    {
        var validation = _constraintValidator.Validate(constraint);
        if (!validation.IsValid)
        {
            var errors = string.Join(" ", validation.Errors);
            if (constraint.Severity == PlanningConstraintSeverity.Hard)
            {
                _logger.LogError(
                    "Approved HARD planning constraint {ConstraintId} is invalid, planning-rule loading is refused: {Errors}",
                    constraint.Id,
                    errors);
                throw new PlanningRuleConfigurationException(constraint.Id, errors);
            }

            _logger.LogError(
                "Approved soft planning constraint {ConstraintId} is invalid and was skipped: {Errors}",
                constraint.Id,
                errors);
            skippedRuleIds.Add(constraint.Id);
            return null;
        }

        IReadOnlySet<string>? scope = constraint.ScopeType switch
        {
            PlanningConstraintScopeType.Global => null,
            PlanningConstraintScopeType.Client => agents.Contains(constraint.ScopeId!.Value)
                ? new HashSet<string>(StringComparer.Ordinal) { constraint.ScopeId.Value.ToString() }
                : EmptyScope(),
            PlanningConstraintScopeType.Group => ToScope(groupMembers.GetValueOrDefault(constraint.ScopeId!.Value)),
            PlanningConstraintScopeType.SchedulingRule => SchedulingRuleScope(agents, constraint.ScopeId!.Value, contractData),
            _ => EmptyScope(),
        };

        return scope is { Count: 0 } ? null : PlanningRuleMapper.FromConstraint(constraint, validation.Parameters!, scope);
    }

    private async Task<IReadOnlyDictionary<Guid, HashSet<Guid>>> ResolveGroupMembersAsync(
        List<Guid> agents,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        List<PlanningConstraint> constraints,
        CancellationToken cancellationToken)
    {
        var scopeGroupIds = constraints
            .Where(c => c.ScopeType == PlanningConstraintScopeType.Group && c.ScopeId.HasValue)
            .Select(c => c.ScopeId!.Value)
            .Distinct()
            .ToList();
        if (scopeGroupIds.Count == 0)
        {
            return new Dictionary<Guid, HashSet<Guid>>();
        }

        var expanded = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var groupId in scopeGroupIds)
        {
            expanded[groupId] = await _groupHierarchy.GetAllGroupIdsIncludingSubgroups(groupId);
        }

        var allGroupIds = expanded.Values.SelectMany(ids => ids).ToHashSet();
        if (allGroupIds.Count == 0)
        {
            return expanded.ToDictionary(entry => entry.Key, _ => new HashSet<Guid>());
        }

        var memberships = await _dataReader.GetGroupMembershipsAsync(allGroupIds, agents, from, until, analyseToken, cancellationToken);
        var membersByGroup = memberships
            .GroupBy(m => m.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.ClientId).ToHashSet());

        return expanded.ToDictionary(
            entry => entry.Key,
            entry => entry.Value
                .SelectMany(groupId => membersByGroup.GetValueOrDefault(groupId) ?? [])
                .ToHashSet());
    }

    private async Task<Dictionary<Guid, EffectiveContractData>?> LoadContractDataForScopesAsync(
        List<Guid> agents, DateOnly from, List<CounterRule> counterRules, List<PlanningConstraint> constraints)
    {
        var needsSchedulingRule = counterRules.Any(r => r.SchedulingRuleId.HasValue)
            || constraints.Any(c => c.ScopeType == PlanningConstraintScopeType.SchedulingRule);
        return needsSchedulingRule
            ? await _contractDataProvider.GetEffectiveContractDataForClientsAsync(agents, from)
            : null;
    }

    private static IReadOnlySet<string> SchedulingRuleScope(
        List<Guid> agents, Guid schedulingRuleId, Dictionary<Guid, EffectiveContractData>? contractData)
    {
        var scope = new HashSet<string>(StringComparer.Ordinal);
        if (contractData is null)
        {
            return scope;
        }

        foreach (var agent in agents)
        {
            if (contractData.TryGetValue(agent, out var data) && data.SchedulingRuleId == schedulingRuleId)
            {
                scope.Add(agent.ToString());
            }
        }

        return scope;
    }

    private static IReadOnlySet<string> ToScope(HashSet<Guid>? members)
    {
        var scope = EmptyScope();
        foreach (var member in members ?? [])
        {
            scope.Add(member.ToString());
        }

        return scope;
    }

    private static HashSet<string> EmptyScope() => new(StringComparer.Ordinal);

    private static List<Guid> DistinctAgents(IReadOnlyCollection<Guid> agentIds)
    {
        ArgumentNullException.ThrowIfNull(agentIds);
        return agentIds.Where(id => id != Guid.Empty).Distinct().ToList();
    }

    private async Task<int> ReadNightRuleMinOverlapAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.NightRuleMinOverlapMinutes);
        return int.TryParse(setting?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) && minutes >= 0
            ? minutes
            : PlanningConstraintDefaults.DefaultNightRuleMinOverlapMinutes;
    }

    private static RuleAgent ToRuleAgent(Guid agentId, Dictionary<Guid, EffectiveContractData>? contractData, int nightMinOverlapMinutes)
    {
        var data = contractData?.GetValueOrDefault(agentId);
        var nightWindow = new CoreNightWindow(
            ParseTimeOrDefault(data?.NightStart, SurchargeDefaults.NightStart),
            ParseTimeOrDefault(data?.NightEnd, SurchargeDefaults.NightEnd));
        var workload = data?.WorkloadPercent ?? RuleTimeConstants.FullWorkloadPercent;
        return new RuleAgent(agentId.ToString(), nightWindow, workload, nightMinOverlapMinutes);
    }

    private static TimeOnly ParseTimeOrDefault(string? value, string fallback)
    {
        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : TimeOnly.Parse(fallback, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
