// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for accepting an AnalyseScenario. Runs the read-only end-state compliance gate BEFORE
/// any mutation: when the scenario would newly introduce Block-mode enforced violations into the
/// real plan and the caller holds no authorised supervisor override, the accept is refused with a
/// conflict. Otherwise it validates the scenario can be applied without conflicting concurrent
/// real-side changes, soft-deletes the real schedule data in the scenario's scope, then promotes
/// scenario works/breaks/schedule-notes to real with shift-id remapping back to the original
/// source shifts. Clone shifts/preferences/shift-expenses are soft-deleted (NOT promoted), so
/// original shift definitions remain untouched and stable across multi-user use. After the
/// promote the real-plan error list is refreshed for the scenario's date range.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class AcceptAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<AcceptAnalyseScenarioCommand, bool>
{
    private const string UnknownUserName = "Unknown";
    private const string BlockedMessageFormat =
        "Scenario acceptance blocked: {0} compliance violation(s) enforced in Block mode ({1}). Request a supervisor override to accept anyway.";

    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly IScenarioComplianceService _complianceService;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcceptAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository,
        IScenarioComplianceService complianceService,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IScheduleTimelineService timelineService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AcceptAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
        _complianceService = complianceService;
        _overrideAuthorizer = overrideAuthorizer;
        _timelineService = timelineService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(AcceptAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.ValidateNoAcceptConflictsAsync(scenario.Token, cancellationToken);

            await EnforceComplianceGateAsync(scenario.FromDate, scenario.UntilDate, scenario.GroupId, scenario.Token, command.OverrideBlock, cancellationToken);

            await _scenarioService.SoftDeleteRealScheduleDataAsync(scenario.GroupId, scenario.Token, scenario.FromDate, scenario.UntilDate, cancellationToken);
            await _scenarioService.PromoteScenarioWorksAsync(scenario.Token, scenario.FromDate, scenario.UntilDate, cancellationToken);

            await _softeningRepository.DeleteByAnalyseTokenAsync(scenario.Token, cancellationToken);
            await _softeningRepository.DeleteByRangeAndTokenAsync(scenario.FromDate, scenario.UntilDate, null, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Accepted;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            _timelineService.QueueRangeCheck(scenario.FromDate, scenario.UntilDate, null);

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }

    /// <summary>
    /// Read-only accept gate: evaluates the end-state compliance diff of the scenario versus the
    /// real plan BEFORE any mutation. Blocking issues refuse the accept unless the caller holds an
    /// authorised supervisor override, which is audit-logged.
    /// </summary>
    private async Task EnforceComplianceGateAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? groupId,
        Guid token,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        var report = await _complianceService.EvaluateAsync(fromDate, untilDate, groupId, token, cancellationToken);
        if (report.BlockingIssues.Count == 0)
        {
            return;
        }

        var blockedRules = ResolveBlockedRules(report.BlockingIssues);

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(overrideBlockRequested);
        if (!authorized)
        {
            throw new ConflictException(string.Format(
                BlockedMessageFormat,
                report.BlockingIssues.Count,
                string.Join(", ", blockedRules)));
        }

        LogOverride(blockedRules, report.BlockingIssues.Count);
    }

    private static List<string> ResolveBlockedRules(IReadOnlyList<PeriodIssueDto> blockingIssues)
        => blockingIssues
            .Select(issue => issue.MessageParams.TryGetValue(ComplianceRuleNames.EnforcementRuleParamKey, out var rule)
                ? rule
                : issue.Code)
            .Distinct()
            .ToList();

    private void LogOverride(IReadOnlyList<string> blockedRules, int issueCount)
    {
        var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? UnknownUserName;

        _logger.LogWarning(
            "Compliance override: user {User} overrode {RuleCount} blocked rule(s) ({Rules}) to accept a scenario with {IssueCount} blocking issue(s).",
            userName,
            blockedRules.Count,
            string.Join(",", blockedRules),
            issueCount);
    }
}
