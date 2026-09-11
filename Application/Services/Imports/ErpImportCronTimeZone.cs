// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the time zone the ERP import cron schedule runs in: the explicit ERP_IMPORT_CRON_TIMEZONE
/// setting when configured, otherwise the installation's own configured company time zone - never a
/// hard-coded regional default. Shared by every reader of the cron schedule (the runner and the status/
/// settings/setup-guidance skills) so they cannot drift from each other or from what the scheduling
/// skill itself persists. Always returns an IANA id, even when the configured setting row still holds a
/// Windows id from before it was normalized on write.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Application.Services.Imports;

public static class ErpImportCronTimeZone
{
    public static async Task<string> ResolveAsync(
        ISettingsReader settingsReader, ICompanyClock companyClock, CancellationToken cancellationToken = default)
    {
        var configured = (await settingsReader.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return IanaTimeZoneId.TryFrom(configured, out var configuredIanaId) ? configuredIanaId! : configured.Trim();
        }

        var companyZone = await companyClock.GetTimeZoneAsync(cancellationToken);
        return IanaTimeZoneId.From(companyZone);
    }
}
