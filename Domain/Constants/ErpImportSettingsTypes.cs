// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Generic Settings row keys for the ERP import cron schedule (one global schedule per instance).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportSettingsTypes
{
    public const string CronExpression = "ERP_IMPORT_CRON_EXPRESSION";
    public const string CronTimeZoneId = "ERP_IMPORT_CRON_TIMEZONE";
    public const string NextRunUtc = "ERP_IMPORT_NEXT_RUN_UTC";

    public const string DefaultCronExpression = "0 * * * *";
    public const string DefaultTimeZoneId = "Europe/Zurich";
}
