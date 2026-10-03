// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningRuleSetLoader"/>. Unites the approved CounterRule rows (PeriodCountRule, severity
/// from Enforcement ?? the global counterRule mode) and the approved PlanningConstraint rows valid in the
/// period with the requested AnalyseToken (IS NOT DISTINCT FROM). Every scope becomes the set of requested
/// agents it covers: Global = null (every agent), Client = that client, Group = members of the group and its
/// subgroups whose membership overlaps the period, SchedulingRule = agents whose effective contract on the
/// first period day references the rule (the CounterRuleEvaluator industry axis). A scope that covers none
/// of the requested agents drops the rule - an empty set never turns into "every agent". Night window and
/// workload come from EffectiveContractData on the first period day (one batched resolution).
/// </summary>
/// <param name="counterRuleRepository">Approved CounterRule rows</param>
/// <param name="constraintRepository">Approved PlanningConstraint rows of the period</param>
/// <param name="constraintValidator">Parses ParametersJson into typed parameters</param>
/// <param name="enforcementResolver">Global counterRule warn/block mode</param>
/// <param name="contractDataProvider">Effective contract data (scheduling rule, night window, workload)</param>
/// <param name="groupHierarchy">Expands a group into itself plus its subgroups</param>
/// <param name="dataReader">Group memberships of the requested agents</param>
/// <param name="carryInLoader">Worked segments outside the period</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Scheduling;
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
        var contractData = await LoadContractDataIfNeededAsync(agents, from, needsAlways: false, cancellationToken);
        return await LoadRulesAsync(agents, from, until, analyseToken, contractData, cancellationToken);
    }

    public async Task<PlanningRuleSet> LoadRuleSetAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        int coveredBoundaryDays,
        CancellationToken cancellationToken = default)
    {
        var agents = DistinctAgents(agentIds);
        var contractData = await LoadContractDataIfNeededAsync(agents, from, needsAlways: true, cancellationToken);
        var rules = await LoadRulesAsync(agents, from, until, analyseToken, contractData, cancellationToken);
        var ruleAgents = agents.Select(id => ToRuleAgent(id, contractData)).ToList();
        var carryIn = await _carryInLoader.LoadAsync(agents, from, until, rules, analyseToken, coveredBoundaryDays, cancellationToken);
        return new PlanningRuleSet(rules, ruleAgents, carryIn);
    }

    private async Task<IReadOnlyList<PlanRule>> LoadRulesAsync(
        List<Guid> agents,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        Dictionary<Guid, EffectiveContractData>? contractData,
        CancellationToken cancellationToken)
    {
        if (agents.Count == 0 || until < from)
        {
            return [];
        }

        var counterRules = await _counterRuleRepository.GetAllApprovedAsync(cancellationToken);
        var constraints = await _constraintRepository.GetApprovedForPeriodAsync(from, until, analyseToken, cancellationToken);
        if (counterRules.Count == 0 && constraints.Count == 0)
        {
            return [];
        }

        contractData ??= await LoadContractDataForScopesAsync(agents, from, counterRules, constraints);
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
            var rule = MapConstraint(constraint, agents, contractData, groupMembers);
            if (rule is not null)
            {
                rules.Add(rule);
            }
        }

        return rules;
    }

    private PlanRule? MapConstraint(
        PlanningConstraint constraint,
        List<Guid> agents,
        Dictionary<Guid, EffectiveContractData>? contractData,
        IReadOnlyDictionary<Guid, HashSet<Guid>> groupMembers)
    {
        var validation = _constraintValidator.Validate(constraint);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Approved planning constraint {ConstraintId} is invalid and was skipped: {Errors}",
                constraint.Id,
                string.Join(" ", validation.Errors));
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

    private async Task<Dictionary<Guid, EffectiveContractData>?> LoadContractDataIfNeededAsync(
        List<Guid> agents, DateOnly from, bool needsAlways, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return needsAlways && agents.Count > 0
            ? await _contractDataProvider.GetEffectiveContractDataForClientsAsync(agents, from)
            : null;
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

    private static RuleAgent ToRuleAgent(Guid agentId, Dictionary<Guid, EffectiveContractData>? contractData)
    {
        var data = contractData?.GetValueOrDefault(agentId);
        var nightWindow = new CoreNightWindow(
            ParseTimeOrDefault(data?.NightStart, SurchargeDefaults.NightStart),
            ParseTimeOrDefault(data?.NightEnd, SurchargeDefaults.NightEnd));
        var workload = data?.WorkloadPercent ?? RuleTimeConstants.FullWorkloadPercent;
        return new RuleAgent(agentId.ToString(), nightWindow, workload);
    }

    private static TimeOnly ParseTimeOrDefault(string? value, string fallback)
    {
        return TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : TimeOnly.Parse(fallback, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
