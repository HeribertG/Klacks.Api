// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The confirmation binding of split_shifts_by_weekday (kind WeekdayCut): the group and every plannable shift its preview
/// would cut, each with its sealed order and the weekdays it would be cut into, so a shift that appeared, disappeared, got
/// another weekday set or was split or booked between the preview and the user's yes makes the confirmation void. The
/// shifts that are skipped are not part of it: they are not touched.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Schedules;

public static class ShiftWeekdayCutBinding
{
    private const string IdFormat = "N";
    private const string GroupPrefix = "group:";
    private const string ShiftPrefix = "shift:";
    private const string FieldSeparator = "|";
    private const string WeekdaySeparator = ",";

    public static ConfirmationBinding Of(ShiftWeekdayCutSelection selection) =>
        ConfirmationBinding.Of(
            ConfirmationBindingKinds.WeekdayCut,
            new[] { GroupPrefix + selection.GroupId.ToString(IdFormat) }
                .Concat(selection.ToCut.Select(shift => ShiftPrefix + string.Join(
                    FieldSeparator,
                    shift.Id.ToString(IdFormat),
                    shift.OriginalId?.ToString(IdFormat) ?? string.Empty,
                    string.Join(
                        WeekdaySeparator,
                        ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift).Select(day => day.ToString(CultureInfo.InvariantCulture)))))));
}
