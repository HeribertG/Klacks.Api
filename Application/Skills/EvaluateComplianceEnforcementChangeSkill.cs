// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises what switching working-time rules from warn to block would do: per rule its reaction now, how many of the
/// open warnings in a window of four weeks before and after the company's today would turn into errors, and whether
/// a supervisor may still push a refused assignment through. The counts come from GetPeriodIssuesQuery, the source the
/// period close and list_period_issues use, so a warning here is a warning there; only counts leave, no names of people.
/// The query reads the whole installation (no group), under the group visibility of the caller; the skill is Admin
/// only in effect (CanViewSettings is held by the Admin role alone), the same reach as list_period_issues.
/// SIDE EFFECT of that path: the validation loader reconciles the compensatory-rest obligations of every client with
/// work in the window and saves them (one SaveChanges per client). That is the state the live editing path derives from
/// the same work rows and it converges (a second run changes nothing), but a wider window than a period means more
/// clients and also obligations of planned future work; list_period_issues accepts any window and has the same effect.
/// The loader truncates its findings at PeriodIssueLimits.MaxValidationIssues ordered by date; when that cap is hit the
/// counts are reported as lower bounds and the last covered day is named. Nothing is changed by the skill.
/// </summary>
/// <param name="rule">Optional. The rule to evaluate (maxDailyHours, maxWeeklyHours, minRestHours, minRestDays, maxConsecutiveDays, periodCap, rollingAverage, restDayRotation, counterRule, compensatoryRest, restrictedTimeWindow, holidayWork); omitted = all rules.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.ComplianceEnforcement;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Application.Services.ComplianceEnforcement;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateComplianceEnforcementChangeSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_compliance_enforcement_change";
    public const int WindowWeeksEachSide = 4;

    private const int DaysPerWeek = 7;
    private const string RuleParameter = "rule";

    private readonly IMediator _mediator;
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly ICompanyClock _companyClock;

    public EvaluateComplianceEnforcementChangeSkill(
        IMediator mediator,
        IComplianceEnforcementResolver enforcementResolver,
        ICompanyClock companyClock)
    {
        _mediator = mediator;
        _enforcementResolver = enforcementResolver;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var raw = GetParameter<string>(parameters, RuleParameter);
        var rules = ComplianceRuleCatalog.Rules;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var rule = ComplianceRuleCatalog.Normalize(raw);
            if (rule == null)
            {
                return SkillResult.Error(
                    $"Unknown rule '{raw.Trim()}'. Use one of: {string.Join(", ", ComplianceRuleCatalog.Rules)}, or omit the rule to evaluate all of them.");
            }

            rules = [rule];
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var windowStart = today.AddDays(-WindowWeeksEachSide * DaysPerWeek);
        var windowEnd = today.AddDays(WindowWeeksEachSide * DaysPerWeek);

        var issues = await _mediator.Send(new GetPeriodIssuesQuery(windowStart, windowEnd, null), cancellationToken);
        var validationIssues = issues.Where(issue => issue.Code != PeriodIssueLimits.ScheduleNoteCode).ToList();
        var capReached = validationIssues.Count >= PeriodIssueLimits.MaxValidationIssues;

        var ruleFacts = new List<ComplianceRuleFacts>();
        foreach (var rule in rules)
        {
            var mode = await _enforcementResolver.GetModeAsync(rule);
            ruleFacts.Add(ComplianceRuleIssueCounter.Count(rule, mode, validationIssues));
        }

        var evaluation = ComplianceEnforcementChangeAdvisor.Evaluate(new ComplianceEnforcementChangeFacts(
            windowStart,
            windowEnd,
            await _enforcementResolver.IsSupervisorOverrideAllowedAsync(),
            capReached,
            capReached ? validationIssues.Max(issue => issue.Date) : null,
            ruleFacts));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
