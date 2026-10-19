// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps an ImportedOrderPayload onto a Shift draft. Shared between ImportedOrderProcessor
/// (create/overwrite an unsealed draft) and OrderSupersessionService (build the replacement
/// draft after closing a superseded SealedOrder), so the field mapping stays in one place.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.ValueObjects;

namespace Klacks.Api.Application.Services.Imports;

public static class ImportedOrderShiftMapper
{
    private const decimal MinutesPerHour = 60m;
    private const int WorkTimeHoursDecimals = 4;

    public static Shift BuildDraft(ImportedOrderPayload order, Guid clientId)
    {
        var shift = new Shift { Id = Guid.NewGuid(), Status = ShiftStatus.OriginalOrder };
        ApplyToDraft(shift, order, clientId);
        return shift;
    }

    public static void ApplyToDraft(Shift shift, ImportedOrderPayload order, Guid clientId)
    {
        shift.ClientId = clientId;
        shift.SourceSystemId = order.SourceSystemId;
        shift.ExternalOrderReference = order.ExternalOrderReference;
        shift.Name = $"ERP {order.ExternalOrderReference}";
        shift.Abbreviation = order.ExternalOrderReference;
        shift.Description = order.Description;
        shift.WorkTime = ResolveWorkTimeHours(order);
        shift.FromDate = order.FromDate;
        shift.UntilDate = order.UntilDate;
        shift.StartShift = order.StartTime;
        shift.EndShift = order.EndTime;
        shift.IsTimeRange = order.IsTimeRange;
        shift.IsMonday = order.IsMonday;
        shift.IsTuesday = order.IsTuesday;
        shift.IsWednesday = order.IsWednesday;
        shift.IsThursday = order.IsThursday;
        shift.IsFriday = order.IsFriday;
        shift.IsSaturday = order.IsSaturday;
        shift.IsSunday = order.IsSunday;
        shift.Quantity = order.Quantity;
        shift.SumEmployees = order.SumEmployees;
    }

    /// <summary>
    /// Resolves the shift work time in decimal hours, rounded to four fraction digits to match
    /// existing Shift.WorkTime data. An explicit duration wins; otherwise the duration is the
    /// clock distance from StartTime to EndTime, wrapping midnight, so an overnight fixed shift
    /// (22:00-06:00) maps to 8 hours and a full-day duty (07:00-07:00) maps to 24 hours.
    /// </summary>
    /// <remarks>
    /// This value is compared against the stored WorkTime of a SealedOrder, so it sits on a
    /// contract boundary: changing what it returns for a given payload retroactively reclassifies
    /// sealed orders as changed and triggers supersession. The equality case is pinned by test
    /// precisely because it must not drift silently again.
    /// </remarks>
    public static decimal ResolveWorkTimeHours(ImportedOrderPayload order)
    {
        if (order.DurationMinutes is int minutes)
        {
            return Math.Round(minutes / MinutesPerHour, WorkTimeHoursDecimals);
        }

        return Math.Round(TimeRange.ForWorkingTime(order.StartTime, order.EndTime).DurationInHours, WorkTimeHoursDecimals);
    }

    /// <summary>
    /// True when applying the payload to an existing unsealed draft would change any field ApplyToDraft
    /// writes (including Name, Abbreviation, SourceSystemId and ExternalOrderReference). The expected
    /// values come from ApplyToDraft itself, so a derived field such as Name can never drift from what an
    /// update would write. Lets the importer skip the write for an unchanged re-delivery and count the
    /// order as unchanged. Deliberately separate from DiffersFromSealedOrder, which is the supersession
    /// contract and compares a narrower set of fields.
    /// </summary>
    public static bool DiffersFromDraft(Shift draft, ImportedOrderPayload order, Guid clientId)
    {
        var expected = new Shift();
        ApplyToDraft(expected, order, clientId);

        return draft.ClientId != expected.ClientId
            || !string.Equals(draft.SourceSystemId, expected.SourceSystemId, StringComparison.Ordinal)
            || !string.Equals(draft.ExternalOrderReference, expected.ExternalOrderReference, StringComparison.Ordinal)
            || !string.Equals(draft.Name, expected.Name, StringComparison.Ordinal)
            || !string.Equals(draft.Abbreviation, expected.Abbreviation, StringComparison.Ordinal)
            || !string.Equals(draft.Description, expected.Description, StringComparison.Ordinal)
            || draft.WorkTime != expected.WorkTime
            || draft.FromDate != expected.FromDate
            || draft.UntilDate != expected.UntilDate
            || draft.StartShift != expected.StartShift
            || draft.EndShift != expected.EndShift
            || draft.IsTimeRange != expected.IsTimeRange
            || draft.IsMonday != expected.IsMonday
            || draft.IsTuesday != expected.IsTuesday
            || draft.IsWednesday != expected.IsWednesday
            || draft.IsThursday != expected.IsThursday
            || draft.IsFriday != expected.IsFriday
            || draft.IsSaturday != expected.IsSaturday
            || draft.IsSunday != expected.IsSunday
            || draft.Quantity != expected.Quantity
            || draft.SumEmployees != expected.SumEmployees;
    }

    /// <summary>
    /// True when the payload carries data that differs from a SealedOrder in a way that matters
    /// for supersession -- an unchanged re-delivery of a full ERP extract must be a no-op, not a
    /// nightly false alarm that cancels and reopens the same order over and over.
    /// </summary>
    public static bool DiffersFromSealedOrder(Shift sealedOrder, ImportedOrderPayload order, Guid clientId)
    {
        return sealedOrder.ClientId != clientId
            || sealedOrder.Description != order.Description
            || sealedOrder.WorkTime != ResolveWorkTimeHours(order)
            || sealedOrder.FromDate != order.FromDate
            || sealedOrder.UntilDate != order.UntilDate
            || sealedOrder.StartShift != order.StartTime
            || sealedOrder.EndShift != order.EndTime
            || sealedOrder.IsTimeRange != order.IsTimeRange
            || sealedOrder.IsMonday != order.IsMonday
            || sealedOrder.IsTuesday != order.IsTuesday
            || sealedOrder.IsWednesday != order.IsWednesday
            || sealedOrder.IsThursday != order.IsThursday
            || sealedOrder.IsFriday != order.IsFriday
            || sealedOrder.IsSaturday != order.IsSaturday
            || sealedOrder.IsSunday != order.IsSunday
            || sealedOrder.Quantity != order.Quantity
            || sealedOrder.SumEmployees != order.SumEmployees;
    }
}
