// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of the contract fields as the contract skills expose them: they are the skill parameter names and,
/// at the same time, the field names reported in differences, adapted-field and copied-field lists.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContractFieldNames
{
    public const string TemplateContractId = "templateContractId";

    public const string Name = "name";

    public const string ValidFrom = "validFrom";

    public const string ValidUntil = "validUntil";

    public const string GuaranteedHours = "guaranteedHours";

    public const string Percent = "percent";

    public const string MinimumHours = "minimumHours";

    public const string MaximumHours = "maximumHours";

    public const string FullTime = "fullTime";

    public const string PaymentInterval = "paymentInterval";

    public const string PerformsShiftWork = "performsShiftWork";

    public const string Workdays = "workdays";

    public const string Region = "region";

    public const string NightRate = "nightRate";

    public const string HolidayRate = "holidayRate";

    public const string SaRate = "saRate";

    public const string SoRate = "soRate";

    public const string We3Rate = "we3Rate";

    public const string NightWindow = "nightWindow";

    public const string SchedulingRule = "schedulingRule";

    public const string IndividualPeriod = "individualPeriod";
}
