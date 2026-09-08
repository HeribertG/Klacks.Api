// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the focus button does. "navigate" carries an ActionRoute the frontend opens;
/// "consultation" carries none and starts the setup consultation in the chat instead, the same
/// way the inbox button does.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class WelcomeFocusActionKinds
{
    public const string Navigate = "navigate";
    public const string Consultation = "consultation";
}
