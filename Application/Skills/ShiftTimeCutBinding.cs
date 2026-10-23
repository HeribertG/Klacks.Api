// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns a cut_shift plan into the facts its confirmation is bound to (kind ShiftCut): the plannable
/// shift that is cut, every part (position, times, name) and every default expense the parts carry (description, amount,
/// taxable). The ids of new parts are left out on purpose: they are drawn fresh for every plan, so the preview and the
/// confirmed replay would never match. The preview provider stores the binding with the token and the skill compares it
/// with its own fresh plan right before it cuts.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public static class ShiftTimeCutBinding
{
    private const string IdFormat = "N";
    private const string TargetPrefix = "target:";
    private const string PartPrefix = "part:";
    private const string ExpensePrefix = "expense:";
    private const string FieldSeparator = "|";
    private const string RangeSeparator = "-";

    public static ConfirmationBinding Of(ShiftTimeCutPlan plan) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.ShiftCut,
            new[] { TargetPrefix + plan.OriginalShift!.Id.ToString(IdFormat) }
                .Concat(plan.Parts.Select(part => PartPrefix + string.Join(
                    FieldSeparator,
                    part.Index.ToString(CultureInfo.InvariantCulture),
                    Time(part.Start) + RangeSeparator + Time(part.End),
                    part.Name)))
                .Concat(plan.DefaultExpenses.Select(expense => ExpensePrefix + string.Join(
                    FieldSeparator,
                    expense.Description,
                    expense.Amount.ToString(CultureInfo.InvariantCulture),
                    expense.Taxable.ToString(CultureInfo.InvariantCulture)))));

    private static string Time(TimeOnly time) => time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
