// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Emits the "configured ERP cron zone differs from the company zone" warning at most once per
/// distinct (configured, company) pair for the lifetime of the process. The zone is resolved on every
/// due check of the ERP import runner, which runs once a minute, so warning per resolution buried the
/// operator in roughly 1440 identical lines a day and made the one line that matters unreadable. A
/// pair that changes - because someone edited the setting or the company zone - is a new situation and
/// is warned about again. Registered as a singleton so the memory outlives the scoped resolvers that
/// use it; it holds nothing scoped itself.
/// </summary>

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Imports;

public sealed class ErpCronTimeZoneDriftNotifier
{
    private const string DriftWarning =
        "ERP import cron time zone setting resolves to '{ConfiguredTimeZoneId}', which differs from the " +
        "company's own configured time zone '{CompanyTimeZoneId}'. Imports are scheduled in a different " +
        "zone than the rest of the application computes business days in - a stored value left over from " +
        "an earlier default is the usual cause. Clear the setting to follow the company zone, unless " +
        "this is intentional. This is logged once per distinct pair of zones.";

    private readonly ConcurrentDictionary<(string Configured, string Company), byte> _reportedPairs = new();

    /// <param name="logger">Logger of the component that resolved the zone</param>
    /// <param name="configuredIanaId">IANA id the ERP_IMPORT_CRON_TIMEZONE setting resolves to</param>
    /// <param name="companyIanaId">IANA id of the installation's own configured company zone</param>
    public void WarnOnce(ILogger logger, string configuredIanaId, string companyIanaId)
    {
        if (!_reportedPairs.TryAdd((configuredIanaId, companyIanaId), default))
        {
            return;
        }

        logger.LogWarning(DriftWarning, configuredIanaId, companyIanaId);
    }
}
