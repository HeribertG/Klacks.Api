// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a page-key requiredFeature against the two gates the Angular router actually uses:
/// featurePluginGuard(name), which lets a plugin page open when the plugin is installed AND enabled
/// (FeaturePluginStateService.isPluginEnabled — note it does NOT consult the operational check, so
/// neither does this), and InboxGuard, which lets the inbox open once an incoming mail server is
/// configured. A feature that is neither the inbox nor a discovered plugin manifest is refused and
/// logged: it can only come from a typo in klacksy-page-keys.ts, and silently treating it as present
/// would make Klacksy offer a page the router then bounces to /no-access.
/// </summary>
/// <param name="featurePluginService">In-memory plugin state: discovered manifests, install and enable flags</param>
/// <param name="inboxAvailability">Incoming mail server configuration behind the inbox page</param>
/// <param name="logger">Logger for feature names no gate can resolve</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public sealed class FeatureAvailabilityService : IFeatureAvailabilityService
{
    private readonly IFeaturePluginService _featurePluginService;
    private readonly IInboxAvailabilityService _inboxAvailability;
    private readonly ILogger<FeatureAvailabilityService> _logger;

    public FeatureAvailabilityService(
        IFeaturePluginService featurePluginService,
        IInboxAvailabilityService inboxAvailability,
        ILogger<FeatureAvailabilityService> logger)
    {
        _featurePluginService = featurePluginService;
        _inboxAvailability = inboxAvailability;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync(string feature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feature))
        {
            return false;
        }

        if (string.Equals(feature, KlacksyFeatures.Inbox, StringComparison.OrdinalIgnoreCase))
        {
            return await _inboxAvailability.IsAvailableAsync(cancellationToken);
        }

        if (_featurePluginService.IsDiscovered(feature))
        {
            return _featurePluginService.IsInstalled(feature) && _featurePluginService.IsEnabled(feature);
        }

        _logger.LogWarning(
            "Navigation feature '{Feature}' is unknown to this installation - no plugin manifest carries that " +
            "name and it is not the inbox. Treating it as unavailable; check the requiredFeature values in " +
            "klacksy-page-keys.ts.",
            feature);

        return false;
    }
}
