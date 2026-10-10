// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure, deterministic comparison of existing contract templates with an administrator's wishes. For every
/// template it lists the wished fields in which the template differs, ranks the templates by the number of
/// differences (ties broken by name) and builds the recommendation text. A workload-path mismatch (a percent
/// wish against a template with fixed guaranteed hours, or the reverse) and a region mismatch each count as one
/// difference; a region mismatch is never a hard filter, because an installation may have no matching calendar.
/// ImpliedChanges lists what the workload-path switch does on top of the wishes (the other path's values are
/// dropped, the minimum/maximum band is reset or collapses to the new guaranteed hours); they are reported next
/// to the differences, never counted as differences.
/// Nothing here reads or writes data, so every number can be reproduced from the inputs.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractTemplateComparer
{
    private const string Confirmation =
        "Present it to the administrator and ask which template to use; the system itself asks for the final " +
        "confirmation before anything is written, so do not ask a separate \"shall I create it?\" first.";

    public static bool IsInheritedWorkload(Contract template) =>
        template.GuaranteedHours is null || template.PaymentInterval == PaymentInterval.MonthlyTargetHours;

    public static IReadOnlyList<ContractTemplateCandidate> Rank(
        IEnumerable<Contract> templates,
        ContractTemplateWish wish,
        IReadOnlyDictionary<Guid, string> calendarNames,
        int maxCandidates)
    {
        return templates
            .Select(template => ToCandidate(template, wish, calendarNames))
            .OrderBy(candidate => candidate.DifferenceCount)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Id)
            .Take(maxCandidates)
            .ToList();
    }

    public static IReadOnlyList<ContractTemplateDifference> Compare(
        Contract template,
        ContractTemplateWish wish,
        IReadOnlyDictionary<Guid, string> calendarNames)
    {
        var differences = new List<ContractTemplateDifference>();

        if (wish.PaymentInterval.HasValue && wish.PaymentInterval.Value != template.PaymentInterval)
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.PaymentInterval,
                template.PaymentInterval.ToString(),
                wish.PaymentInterval.Value.ToString()));
        }

        AddGuaranteedHoursDifference(template, wish, differences);
        AddPercentDifference(template, wish, differences);

        var templateFullTime = template.FullTime ?? decimal.Zero;
        if (wish.FullTime.HasValue && wish.FullTime.Value != templateFullTime)
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.FullTime, Format(templateFullTime), Format(wish.FullTime.Value)));
        }

        if (wish.PerformsShiftWork.HasValue && wish.PerformsShiftWork != template.PerformsShiftWork)
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.PerformsShiftWork,
                FormatShiftWork(template.PerformsShiftWork),
                FormatShiftWork(wish.PerformsShiftWork)));
        }

        if (wish.Workdays != null)
        {
            var templateDays = ContractWorkdays.Of(template);
            if (!templateDays.SetEquals(wish.Workdays))
            {
                differences.Add(new ContractTemplateDifference(
                    ContractFieldNames.Workdays,
                    ContractWorkdays.Format(templateDays),
                    ContractWorkdays.Format(wish.Workdays)));
            }
        }

        if (wish.Region != null
            && (template.CalendarSelectionId is null || !wish.Region.CalendarSelectionIds.Contains(template.CalendarSelectionId.Value)))
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.Region, CalendarName(template, calendarNames), wish.Region.Code));
        }

        return differences;
    }

    public static IReadOnlyList<ContractTemplateImpliedChange> ImpliedChanges(Contract template, ContractTemplateWish wish)
    {
        var changes = new List<ContractTemplateImpliedChange>();

        if (wish.GuaranteedHours.HasValue)
        {
            if (template.Percent.HasValue)
            {
                changes.Add(new ContractTemplateImpliedChange(
                    ContractFieldNames.Percent,
                    $"{Format(template.Percent.Value)} %",
                    ContractTemplateAdvisoryDefaults.NoPercentLabel));
            }

            AddBandChanges(changes, template, wish.GuaranteedHours.Value, wish.GuaranteedHours.Value);
        }
        else if (wish.Percent.HasValue)
        {
            if (template.GuaranteedHours.HasValue)
            {
                changes.Add(new ContractTemplateImpliedChange(
                    ContractFieldNames.GuaranteedHours,
                    Format(template.GuaranteedHours.Value),
                    ContractTemplateAdvisoryDefaults.InheritedWorkloadLabel));
            }

            AddBandChanges(changes, template, decimal.Zero, decimal.Zero);
        }

        return changes;
    }

    public static string BuildRecommendation(
        IReadOnlyList<ContractTemplateCandidate> ranked,
        int activeTemplateCount,
        ContractTemplateWish wish)
    {
        if (activeTemplateCount == 0 || ranked.Count == 0)
        {
            return "No active contract exists — create the new contract from scratch with create_contract.";
        }

        if (wish.WishCount == 0)
        {
            return $"{activeTemplateCount} active contract template(s) exist, but no wishes were given, so nothing could " +
                   "be compared. Ask the administrator for the payment interval, the workload path (fixed guaranteed " +
                   "hours or a percent of the company-wide value) and the holiday-calendar region first.";
        }

        var best = ranked[0];

        if (best.DifferenceCount == 0)
        {
            return $"Best match: '{best.Name}' (id {best.Id}) fits all {wish.WishCount} wished field(s) unchanged. " +
                   $"Create the new contract as a copy of it with create_contract_from_template.{DescribeImpliedChanges(best)} {Confirmation}";
        }

        var fields = string.Join(", ", best.Differences.Select(difference => difference.Field));
        return $"Best match: '{best.Name}' (id {best.Id}) differs in {best.DifferenceCount} of {wish.WishCount} wished " +
               $"field(s): {fields}. Creating the new contract from it with create_contract_from_template means " +
               $"adapting {best.DifferenceCount} field(s).{DescribeImpliedChanges(best)} {Confirmation}";
    }

    private static ContractTemplateCandidate ToCandidate(
        Contract template,
        ContractTemplateWish wish,
        IReadOnlyDictionary<Guid, string> calendarNames)
    {
        var differences = Compare(template, wish, calendarNames);
        var impliedChanges = ImpliedChanges(template, wish);

        return new ContractTemplateCandidate(
            template.Id,
            template.Name,
            template.GuaranteedHours,
            template.Percent,
            template.MinimumHours ?? decimal.Zero,
            template.MaximumHours ?? decimal.Zero,
            template.FullTime ?? decimal.Zero,
            template.PaymentInterval.ToString(),
            template.PerformsShiftWork,
            ContractWorkdays.Format(ContractWorkdays.Of(template)),
            CalendarName(template, calendarNames),
            template.ValidFrom,
            template.ValidUntil,
            differences.Count,
            differences,
            impliedChanges);
    }

    private static void AddBandChanges(
        List<ContractTemplateImpliedChange> changes, Contract template, decimal minimum, decimal maximum)
    {
        var templateMinimum = template.MinimumHours ?? decimal.Zero;
        if (templateMinimum != minimum)
        {
            changes.Add(new ContractTemplateImpliedChange(
                ContractFieldNames.MinimumHours, Format(templateMinimum), Format(minimum)));
        }

        var templateMaximum = template.MaximumHours ?? decimal.Zero;
        if (templateMaximum != maximum)
        {
            changes.Add(new ContractTemplateImpliedChange(
                ContractFieldNames.MaximumHours, Format(templateMaximum), Format(maximum)));
        }
    }

    private static string DescribeImpliedChanges(ContractTemplateCandidate candidate)
    {
        if (candidate.ImpliedChanges.Count == 0)
        {
            return string.Empty;
        }

        var listed = string.Join(
            ", ",
            candidate.ImpliedChanges.Select(change => $"{change.Field} ({change.TemplateValue} -> {change.ResultingValue})"));
        return $" The workload path also changes (implied, not wishes - name them in the proposal): {listed}.";
    }

    private static void AddGuaranteedHoursDifference(
        Contract template, ContractTemplateWish wish, List<ContractTemplateDifference> differences)
    {
        if (!wish.GuaranteedHours.HasValue)
        {
            return;
        }

        if (IsInheritedWorkload(template))
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.GuaranteedHours,
                $"inherited from the company-wide value ({Format(EffectivePercent(template))} %)",
                Format(wish.GuaranteedHours.Value)));
            return;
        }

        if (template.GuaranteedHours!.Value != wish.GuaranteedHours.Value)
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.GuaranteedHours,
                Format(template.GuaranteedHours.Value),
                Format(wish.GuaranteedHours.Value)));
        }
    }

    private static void AddPercentDifference(
        Contract template, ContractTemplateWish wish, List<ContractTemplateDifference> differences)
    {
        if (!wish.Percent.HasValue)
        {
            return;
        }

        if (!IsInheritedWorkload(template))
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.Percent,
                $"fixed {Format(template.GuaranteedHours!.Value)} guaranteed hours (no percent)",
                $"{Format(wish.Percent.Value)} %"));
            return;
        }

        if (EffectivePercent(template) != wish.Percent.Value)
        {
            differences.Add(new ContractTemplateDifference(
                ContractFieldNames.Percent,
                $"{Format(EffectivePercent(template))} %",
                $"{Format(wish.Percent.Value)} %"));
        }
    }

    private static decimal EffectivePercent(Contract template) =>
        template.Percent ?? MonthlyTargetHoursConstants.FullWorkloadPercent;

    private static string CalendarName(Contract template, IReadOnlyDictionary<Guid, string> calendarNames) =>
        template.CalendarSelectionId.HasValue && calendarNames.TryGetValue(template.CalendarSelectionId.Value, out var name)
            ? name
            : ContractTemplateAdvisoryDefaults.NoOwnCalendarLabel;

    private static string FormatShiftWork(bool? value) =>
        value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()
            : ContractTemplateAdvisoryDefaults.StandardValueLabel;

    private static string Format(decimal value) =>
        value.ToString(ContractTemplateAdvisoryDefaults.DecimalFormat, CultureInfo.InvariantCulture);
}
