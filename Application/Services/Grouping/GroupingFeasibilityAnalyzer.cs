// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs one grouping feasibility analysis: loads the snapshot, derives the run days of every plannable
/// shift with the wizard's own shift builder (so analysis and autofill see the same days), loads the
/// effective contract data for the whole period in one range call, reads the expired-mandatory-blocks
/// setting, evaluates eligibility, builds the plan (F1-F6), estimates capacity (F7) and fingerprints the
/// result. Shifts without a single run day in the period are not analysed. Finally every planning unit is
/// summarised (GroupingUnitSummaryBuilder) against the real memberships, so the report can name root causes
/// such as missing contracts; the duration of that step is logged separately.
/// </summary>
/// <param name="dataSource">Snapshot loader.</param>
/// <param name="shiftBuilder">Wizard shift expansion (weekday flags and validity, no holiday rule).</param>
/// <param name="contractProvider">Effective contract data per day and client.</param>
/// <param name="settingsReader">Reads QUALIFICATION_EXPIRED_MANDATORY_BLOCKS.</param>
/// <param name="companyClock">Company "today" for upcoming works and addresses.</param>
/// <param name="logger">Duration of the planning-unit summary step.</param>

using System.Diagnostics;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingFeasibilityAnalyzer : IGroupingFeasibilityAnalyzer
{
    private const string InvalidPeriodMessage =
        "The analysis period must start on or before its end and span at most {0} days.";

    private readonly IGroupingFeasibilityDataSource _dataSource;
    private readonly IWizardShiftBuilder _shiftBuilder;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<GroupingFeasibilityAnalyzer> _logger;

    public GroupingFeasibilityAnalyzer(
        IGroupingFeasibilityDataSource dataSource,
        IWizardShiftBuilder shiftBuilder,
        IClientContractDataProvider contractProvider,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        ILogger<GroupingFeasibilityAnalyzer> logger)
    {
        _dataSource = dataSource;
        _shiftBuilder = shiftBuilder;
        _contractProvider = contractProvider;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _logger = logger;
    }

    public async Task<GroupingFeasibilityReport> AnalyzeAsync(GroupingAnalysisRequest request, CancellationToken cancellationToken)
    {
        EnsureValidPeriod(request);

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var snapshot = await _dataSource.LoadAsync(request.From, request.Until, today, cancellationToken);
        var runDays = await LoadRunDaysAsync(snapshot.Shifts, request, cancellationToken);
        var shifts = snapshot.Shifts.Where(shift => runDays.ContainsKey(shift.Id)).ToList();
        var clients = snapshot.Clients;

        var context = new GroupingEligibilityContext(
            shifts.ToDictionary(shift => shift.Id),
            runDays,
            await LoadContractsAsync(clients, request),
            snapshot.Requirements
                .GroupBy(requirement => requirement.ShiftId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<ShiftRequiredQualification>)group.ToList()),
            snapshot.Qualifications
                .GroupBy(qualification => qualification.ClientId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<ClientQualification>)group.ToList()),
            snapshot.Blacklist,
            snapshot.Availability
                .GroupBy(entry => (entry.ClientId, entry.Date))
                .ToDictionary(group => (group.Key.ClientId, group.Key.Date), group => (IReadOnlyList<ClientAvailability>)group.ToList()),
            await IsExpiredMandatoryBlockingAsync());
        var eligibility = new StaticEligibilityEvaluator(context);

        var tree = new GroupingGroupTree(snapshot.Groups);
        var plan = new GroupingPlanBuilder(new GroupingPlanInput(
            clients,
            shifts,
            tree,
            snapshot.Memberships,
            snapshot.FutureWorkPairs,
            eligibility,
            request.ScopeGroupId)).Build();

        var capacity = new CapacityEstimator().Estimate(new CapacityInput(
            plan.Tree, plan.State, context.Shifts, runDays, eligibility, request.ScopeGroupId));
        var findings = plan.Findings.Concat(capacity).ToList();

        var summaryWatch = Stopwatch.StartNew();
        var realState = GroupingMembershipState.From(
            snapshot.Memberships,
            tree,
            clients.Select(client => client.Id).ToHashSet(),
            shifts.Select(shift => shift.Id).ToHashSet());
        var unitSummaries = GroupingUnitSummaryBuilder.Build(tree, realState, eligibility, findings, request.ScopeGroupId);
        summaryWatch.Stop();
        _logger.LogInformation(
            "Grouping feasibility summarised {Units} planning unit(s) in {ElapsedMs} ms",
            unitSummaries.Count, summaryWatch.ElapsedMilliseconds);

        return new GroupingFeasibilityReport(
            request,
            findings,
            plan.Proposals,
            GroupingFingerprint.ForPlan(request.ScopeGroupId, findings, plan.Proposals),
            GroupingFingerprint.ForReport(findings),
            snapshot.Groups.ToDictionary(group => group.Id, group => group.Name),
            tree.GroupIds.ToDictionary(groupId => groupId, tree.SelfAndAncestors),
            snapshot.Memberships
                .SelectMany(membership => new[] { membership.ClientId, membership.ShiftId }
                    .OfType<Guid>()
                    .Select(memberId => (MemberId: memberId, membership.GroupId)))
                .GroupBy(item => item.MemberId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)group.Select(item => item.GroupId).Distinct().ToList()),
            clients.ToDictionary(client => client.Id, client => client.DisplayName),
            shifts.ToDictionary(shift => shift.Id, shift => shift.DisplayName),
            clients.Count,
            shifts.Count)
        {
            UnitSummaries = unitSummaries,
        };
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> LoadRunDaysAsync(
        IReadOnlyList<GroupingShiftRecord> shifts, GroupingAnalysisRequest request, CancellationToken cancellationToken)
    {
        if (shifts.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<DateOnly>>();
        }

        var expanded = await _shiftBuilder.BuildAsync(
            shifts.Select(shift => shift.Id).ToList(), request.From, request.Until, null, cancellationToken);

        return EligibilityMatrixBuilder.SlotsFromShifts(expanded)
            .GroupBy(slot => slot.ShiftId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DateOnly>)group.Select(slot => slot.Date).OrderBy(day => day).ToList());
    }

    private async Task<IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<Guid, EffectiveContractData>>> LoadContractsAsync(
        IReadOnlyList<GroupingClientRecord> clients, GroupingAnalysisRequest request)
    {
        if (clients.Count == 0)
        {
            return new Dictionary<DateOnly, IReadOnlyDictionary<Guid, EffectiveContractData>>();
        }

        var byDay = await _contractProvider.GetEffectiveContractDataForClientsRangeAsync(
            clients.Select(client => client.Id).ToList(), request.From, request.Until);
        return byDay.ToDictionary(entry => entry.Key, entry => (IReadOnlyDictionary<Guid, EffectiveContractData>)entry.Value);
    }

    private async Task<bool> IsExpiredMandatoryBlockingAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.QualificationExpiredMandatoryBlocks);
        return bool.TryParse(setting?.Value, out var enabled) && enabled;
    }

    private static void EnsureValidPeriod(GroupingAnalysisRequest request)
    {
        var span = request.Until.DayNumber - request.From.DayNumber;
        if (span < 0 || span > GroupingFeasibilityDefaults.MaxHorizonDays)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                string.Format(InvalidPeriodMessage, GroupingFeasibilityDefaults.MaxHorizonDays));
        }
    }
}
