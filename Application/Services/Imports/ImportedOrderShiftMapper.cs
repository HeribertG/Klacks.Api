// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps an ImportedOrderPayload onto a Shift draft. Shared between ErpOrderImportRunner
/// (create/overwrite an unsealed draft) and OrderSupersessionService (build the replacement
/// draft after closing a superseded SealedOrder), so the field mapping stays in one place.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

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
    /// (22:00-06:00) maps to 8 hours.
    /// </summary>
    public static decimal ResolveWorkTimeHours(ImportedOrderPayload order)
    {
        if (order.DurationMinutes is int minutes)
        {
            return Math.Round(minutes / MinutesPerHour, WorkTimeHoursDecimals);
        }

        return Math.Round((decimal)(order.EndTime - order.StartTime).TotalMinutes / MinutesPerHour, WorkTimeHoursDecimals);
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
