// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the time zone the ERP import cron schedule runs in: the explicit ERP_IMPORT_CRON_TIMEZONE
/// setting when configured, otherwise the installation's own configured company time zone - never a
/// hard-coded regional default. Shared by every reader of the cron schedule (the runner and the status/
/// settings/setup-guidance skills) so they cannot drift from each other or from what the scheduling
/// skill itself persists. Always returns an IANA id: a configured setting still holding a Windows id
/// from before it was normalized on write is converted, and a configured value that resolves to no
/// known time zone at all (e.g. a typo) is discarded in favour of the company zone, with a warning
/// logged, rather than being echoed back as an unusable, non-IANA string.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Imports;

public static class ErpImportCronTimeZone
{
    private const string UnresolvableSettingWarning =
        "ERP import cron time zone setting '{Configured}' does not resolve to a known time zone - " +
        "falling back to the company zone";

    public static async Task<string> ResolveAsync(
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var configured = (await settingsReader.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (IanaTimeZoneId.TryFrom(configured, out var configuredIanaId))
            {
                return configuredIanaId!;
            }

            logger.LogWarning(UnresolvableSettingWarning, configured);
        }

        var companyZone = await companyClock.GetTimeZoneAsync(cancellationToken);
        return IanaTimeZoneId.From(companyZone);
    }
}
