// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The settings whose presence decides whether the inbox page exists at all, mirroring the Angular
/// InboxVisibilityService. Shared so the reader that answers the question and the write paths that
/// have to invalidate the answer cannot drift apart.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class InboxAvailabilitySettingKeys
{
    public static readonly string[] All =
    [
        Settings.APP_INCOMING_SERVER,
        Settings.APP_INCOMING_SERVER_USERNAME,
        Settings.APP_INCOMING_SERVER_PASSWORD
    ];
}
