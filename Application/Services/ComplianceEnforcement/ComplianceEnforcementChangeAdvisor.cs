// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on switching working-time rules from warn to block. Pure and static, so every branch is
/// pinned by a unit test; the model only relays it. Per rule the outcome is one of: it already blocks, no open
/// warning would turn into an error, or N warnings would. The counts are the warnings the period check reports for
/// the window; they are lower bounds when that check was cut off at its cap. Under Block the booking paths that
/// partition by compliance (assistant placement, bulk entry, absence cover, automatic planning, replacements) refuse a
/// booking that newly breaks the rule, a supervisor may push it through only while the override is on; a single entry
/// saved directly in the schedule grid is only checked for hard conflicts and is kept, its breach appears as an error.
/// Existing assignments are not removed, and the errors an escalation creates stop a period close until they are
/// fixed or knowingly accepted. Nothing is changed by the advice.
/// </summary>
/// <param name="facts">The gathered facts about the evaluated rules and the window</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.ComplianceEnforcement;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ComplianceEnforcement;

public static class ComplianceEnforcementChangeAdvisor
{
    private const string RefusalNotice =
        "Bookings made through the assistant, bulk entry, absence cover, automatic planning or a replacement that newly break the rule are then refused; a single entry saved directly in the schedule grid is kept and the breach is shown as an error.";

    private const string ConfirmationNotice =
        "Nothing was changed by this check; the setting itself is changed in a separate step the system confirms once.";

    public static ComplianceEnforcementChangeEvaluation Evaluate(ComplianceEnforcementChangeFacts facts)
    {
        var effects = facts.Rules.Select(Effect).ToList();
        var total = effects.Sum(effect => effect.WarningsTurningIntoErrors);

        return new ComplianceEnforcementChangeEvaluation(
            facts.WindowStart,
            facts.WindowEnd,
            facts.SupervisorOverrideAllowed,
            facts.IssueCapReached,
            facts.IssueCapReached ? facts.CoveredUntil : null,
            total,
            effects,
            Analyse(facts, effects),
            BuildRecommendation(facts, effects, total));
    }

    private static ComplianceRuleEffect Effect(ComplianceRuleFacts rule)
    {
        if (rule.CurrentMode == RuleEnforcementMode.Block)
        {
            return new ComplianceRuleEffect(
                rule.Rule, rule.CurrentMode, 0, rule.EnforcedErrorCount, ComplianceRuleOutcome.AlreadyBlocking);
        }

        var outcome = rule.WarningCount > 0
            ? ComplianceRuleOutcome.WarningsBecomeErrors
            : ComplianceRuleOutcome.NoWarningsInWindow;

        return new ComplianceRuleEffect(rule.Rule, rule.CurrentMode, rule.WarningCount, rule.EnforcedErrorCount, outcome);
    }

    private static List<ComplianceEnforcementFinding> Analyse(
        ComplianceEnforcementChangeFacts facts, IReadOnlyList<ComplianceRuleEffect> effects)
    {
        var findings = effects.SelectMany(effect => RuleFindings(facts, effect)).ToList();

        if (facts.IssueCapReached)
        {
            findings.Add(new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.CountIsLowerBound, null, LowerBoundSentence(facts).Trim()));
        }

        findings.Add(new ComplianceEnforcementFinding(
            facts.SupervisorOverrideAllowed
                ? ComplianceEnforcementFindingCode.SupervisorOverrideOn
                : ComplianceEnforcementFindingCode.SupervisorOverrideOff,
            null,
            OverrideSentence(facts.SupervisorOverrideAllowed).Trim()));

        return findings;
    }

    private static IEnumerable<ComplianceEnforcementFinding> RuleFindings(
        ComplianceEnforcementChangeFacts facts, ComplianceRuleEffect effect)
    {
        var label = ComplianceRuleCatalog.Label(effect.Rule);

        if (effect.Outcome == ComplianceRuleOutcome.AlreadyBlocking)
        {
            yield return new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.AlreadyBlocking,
                effect.Rule,
                $"For {label}, the reaction is already block; switching to block changes nothing.");
            yield break;
        }

        yield return effect.Outcome == ComplianceRuleOutcome.NoWarningsInWindow
            ? new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.NoWarningsInWindow,
                effect.Rule,
                $"No warning about {label} is open in the window, so no existing finding would turn into an error.")
            : new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.WarningsBecomeErrors,
                effect.Rule,
                $"{Count(facts, effect.WarningsTurningIntoErrors)} warning(s) about {label} in the window would turn into errors.");

        if (ComplianceRuleCatalog.IsEvaluatedPerPeriod(effect.Rule))
        {
            yield return new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.CountedPerPeriod,
                effect.Rule,
                $"For {label}, the check works per period or rolling window, not day by day, so the count covers only what that check reports for this window.");
        }

        if (ComplianceRuleCatalog.CanBeOverriddenPerItem(effect.Rule)
            && effect.Outcome == ComplianceRuleOutcome.WarningsBecomeErrors)
        {
            yield return new ComplianceEnforcementFinding(
                ComplianceEnforcementFindingCode.CountIsUpperBound,
                effect.Rule,
                "Single counting rules can carry their own reaction, which this switch does not change; the real number may be lower.");
        }
    }

    private static string BuildRecommendation(
        ComplianceEnforcementChangeFacts facts, IReadOnlyList<ComplianceRuleEffect> effects, int total)
    {
        var advice = effects.Count == 1
            ? SingleRuleAdvice(facts, effects[0])
            : ManyRulesAdvice(facts, effects, total);

        return $"{advice}{OverrideSentence(facts.SupervisorOverrideAllowed)}{LowerBoundSentence(facts)} {ConfirmationNotice}";
    }

    private static string SingleRuleAdvice(ComplianceEnforcementChangeFacts facts, ComplianceRuleEffect effect)
    {
        var label = ComplianceRuleCatalog.Label(effect.Rule);
        return effect.Outcome switch
        {
            ComplianceRuleOutcome.AlreadyBlocking =>
                $"For {label}, the reaction is already block; switching to block changes nothing.",
            ComplianceRuleOutcome.NoWarningsInWindow =>
                $"Switching {label} to block turns no existing finding {WindowLabel(facts)} into an error. {RefusalNotice} Existing assignments stay as they are.",
            _ =>
                $"Switching {label} to block turns {Count(facts, effect.WarningsTurningIntoErrors)} open warning(s) {WindowLabel(facts)} into errors. Existing assignments stay as they are, but an error stops a period close until it is fixed or knowingly accepted. {RefusalNotice} Resolve or accept those warnings first if the next close should stay clean."
        };
    }

    private static string ManyRulesAdvice(
        ComplianceEnforcementChangeFacts facts, IReadOnlyList<ComplianceRuleEffect> effects, int total)
    {
        var blocking = effects.Count(effect => effect.Outcome == ComplianceRuleOutcome.AlreadyBlocking);
        var affected = effects
            .Where(effect => effect.Outcome == ComplianceRuleOutcome.WarningsBecomeErrors)
            .OrderByDescending(effect => effect.WarningsTurningIntoErrors)
            .ToList();
        var blockingNote = blocking > 0 ? $" {blocking} rule(s) already refuse assignments." : string.Empty;

        if (affected.Count == 0)
        {
            return $"No open warning {WindowLabel(facts)} would turn into an error if the rules were switched to block. {RefusalNotice}{blockingNote}";
        }

        var breakdown = string.Join(
            "; ",
            affected.Select(effect => $"{ComplianceRuleCatalog.Label(effect.Rule)}: {Count(facts, effect.WarningsTurningIntoErrors)}"));

        return $"Switching all rules to block would turn {Count(facts, total)} open warning(s) {WindowLabel(facts)} into errors ({breakdown}). Existing assignments stay as they are, but an error stops a period close until it is fixed or knowingly accepted. {RefusalNotice}{blockingNote}";
    }

    private static string OverrideSentence(bool allowed) => allowed
        ? " A supervisor can still push a refused booking through, because the supervisor override is switched on."
        : " Nobody can push a refused booking through, because the supervisor override is switched off.";

    private static string LowerBoundSentence(ComplianceEnforcementChangeFacts facts)
    {
        if (!facts.IssueCapReached)
        {
            return string.Empty;
        }

        var reach = facts.CoveredUntil.HasValue ? $" at {Format(facts.CoveredUntil.Value)}, possibly only in part" : string.Empty;
        return $" The finding list was cut off at its limit{reach}, so these counts are lower bounds and later days of the window are not included.";
    }

    private static string Count(ComplianceEnforcementChangeFacts facts, int count) =>
        facts.IssueCapReached ? $"at least {count}" : count.ToString(CultureInfo.InvariantCulture);

    private static string WindowLabel(ComplianceEnforcementChangeFacts facts) =>
        $"between {Format(facts.WindowStart)} and {Format(facts.WindowEnd)}";

    private static string Format(DateOnly date) => date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
