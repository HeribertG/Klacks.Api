// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// i18n keys for deterministic chat responses emitted by navigate_to.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationResponseKeys
{
    public const string SuccessScrolled = "nav.success.scrolled";
    public const string SuccessRouted = "nav.success.routed";
    public const string FailTargetMissing = "nav.fail.targetMissing";
    public const string FailPermissionDenied = "nav.fail.permissionDenied";
    public const string EmptyUtteranceGreeting = "nav.greeting.empty";
}
