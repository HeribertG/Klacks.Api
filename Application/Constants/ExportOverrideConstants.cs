// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Whitelisted patch keys for export format overrides, per format family. Only these parameters can
/// be hotfixed via the database; structural output changes still require a code release.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ExportOverrideConstants
{
    public const string KeyDateFormat = "dateFormat";
    public const string KeyTimeFormat = "timeFormat";
    public const string KeyCurrencyCode = "currencyCode";
    public const string KeyLanguage = "language";
    public const string KeyDelimiter = "delimiter";
    public const string KeyEncoding = "encoding";
    public const string KeyBaseWageType = "baseWageType";
    public const string KeySurchargeWageType = "surchargeWageType";
    public const string KeyAbsenceMapping = "absenceMapping";

    public const string ClientPeriodFormatKeyPrefix = "clientperiod-";

    public static readonly IReadOnlyList<string> OrderFamilyKeys =
        [KeyDateFormat, KeyTimeFormat, KeyCurrencyCode, KeyLanguage];

    public static readonly IReadOnlyList<string> PayrollFamilyKeys =
        [KeyDelimiter, KeyEncoding, KeyBaseWageType, KeySurchargeWageType, KeyAbsenceMapping];

    public static IReadOnlyList<string> KeysForFamily(string family) =>
        family == ExportConstants.FormatFamilyPayroll ? PayrollFamilyKeys : OrderFamilyKeys;
}
