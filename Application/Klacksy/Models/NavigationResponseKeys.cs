// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// i18n keys for deterministic chat responses emitted by the Klacksy navigate_to
/// fast-path. The frontend resolves them via its translation store.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationResponseKeys
{
    public const string SuccessScrolled = "nav.success.scrolled";
    public const string SuccessRouted = "nav.success.routed";
    public const string FailTargetMissing = "nav.fail.targetMissing";
    public const string FailPermissionDenied = "nav.fail.permissionDenied";
    public const string EmptyUtteranceGreeting = "nav.greeting.empty";

    /// <summary>
    /// i18n key for the short acknowledgment emitted as a content chunk during a
    /// Tier-1 fast-path navigation. The frontend resolves it via its translation
    /// store, so it is localized for every installed language (no hardcoded text).
    /// </summary>
    public const string FastPathAck = "nav.ack.fastPath";
}
