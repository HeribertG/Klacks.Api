// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class AdminSetupMessages
{
    public const string AlreadyCompleted = "Own admin account setup was already completed.";

    public const string NotRequiredInEnvironment = "Own admin account setup is not required in this environment (Development or Playground); the seeded admin account stays active.";
}
