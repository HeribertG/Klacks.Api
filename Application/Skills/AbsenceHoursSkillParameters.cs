// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameter names shared by the absence-hours skills (calculation and setup advice) and the one parser they
/// share for the optional payment interval.
/// </summary>
/// <param name="parameters">Raw skill parameters as the model supplied them</param>

using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

internal static class AbsenceHoursSkillParameters
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string Date = "date";
    public const string AbsenceName = "absenceName";
    public const string PaymentInterval = "paymentInterval";
    public const string DesiredHoursPerDay = "desiredHoursPerDay";

    public static bool TryReadPaymentInterval(Dictionary<string, object> parameters, out int? paymentInterval, out string? error)
    {
        paymentInterval = null;
        error = null;

        var raw = SkillParameterReader.Read<string>(parameters, PaymentInterval, null);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!ContractPaymentIntervalParser.TryParse(raw, out var parsed, out error))
        {
            return false;
        }

        paymentInterval = (int)parsed;
        return true;
    }
}
