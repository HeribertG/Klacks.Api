// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Markers a proactive message can carry so the frontend knows how to render it. A summary that
/// starts with <see cref="I18nPrefix"/> is an i18n key the frontend resolves in the user's UI
/// language (the server does not know each connected user's language); anything else is literal text.
/// </summary>
public static class ProactiveMessageMarkers
{
    public const string I18nPrefix = "i18n:";
}
