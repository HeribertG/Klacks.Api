// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class AdminSetupMessages
{
    public const string AlreadyCompleted = "Own admin account setup was already completed.";

    public const string NotRequiredInEnvironment = "Own admin account setup is not required in this environment (Development or Playground); the seeded admin account stays active.";
}
