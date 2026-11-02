// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Projects WorkChange rows into per-person, per-day hour and surcharge deltas with the same rules as the period hours
/// (PeriodHoursService.CalculatePeriodHoursForClientsAsync): an hour-bearing correction (CorrectionWorkChangeTypes
/// .HourBearing: CorrectionStart/End, TravelStart/End/Within) adds its ChangeTime and Surcharges to the work's own
/// employee - this includes the briefing/debriefing and travel rows the container expansion writes as
/// CorrectionStart/CorrectionEnd; a replacement (ReplacementWorkChangeTypes) subtracts them from the work's employee and
/// adds them to the replacing person (ReplaceClientId) - unless that person is no Employee/ExternEmp, who is never
/// exported (the subtraction stays, as in the period hours); every other type (Briefing, Debriefing) adds nothing. Each
/// delta lands on the day of the carrying work.
/// @param rows - The WorkChange rows to project
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.Exports;

public static class PayrollWorkChangeProjection
{
    public static Dictionary<(Guid ClientId, DateOnly Date), (decimal Hours, decimal Surcharges)> ProjectDayDeltas(
        IEnumerable<PayrollWorkChangeRow> rows)
    {
        var deltas = new Dictionary<(Guid ClientId, DateOnly Date), (decimal Hours, decimal Surcharges)>();

        foreach (var row in rows)
        {
            if (CorrectionWorkChangeTypes.IsHourBearing(row.Type))
            {
                Add(deltas, row.WorkClientId, row.Date, row.ChangeTime, row.Surcharges);
            }
            else if (ReplacementWorkChangeTypes.IsReplacement(row.Type))
            {
                Add(deltas, row.WorkClientId, row.Date, -row.ChangeTime, -row.Surcharges);

                if (IsReplacementWithSubstitute(row))
                {
                    Add(deltas, row.ReplaceClientId!.Value, row.Date, row.ChangeTime, row.Surcharges);
                }
            }
        }

        return deltas;
    }

    public static bool IsReplacementWithSubstitute(PayrollWorkChangeRow row)
    {
        return ReplacementWorkChangeTypes.IsReplacement(row.Type)
            && row.ReplaceClientId.HasValue
            && row.ReplaceClientIsPayrollPerson;
    }

    private static void Add(
        Dictionary<(Guid ClientId, DateOnly Date), (decimal Hours, decimal Surcharges)> deltas,
        Guid clientId,
        DateOnly date,
        decimal hours,
        decimal surcharges)
    {
        var current = deltas.GetValueOrDefault((clientId, date));
        deltas[(clientId, date)] = (current.Hours + hours, current.Surcharges + surcharges);
    }
}
