// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration keys describing the kind of deployment the running instance is.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class DeploymentConstants
{
    /// <summary>
    /// Boolean configuration key. True only on the public demo/Playground instance, where the seeded
    /// "admin@test.com" account staying active and known is intentional. Absent or false (the secure
    /// default) on every customer deployment.
    /// </summary>
    public const string IsPlaygroundConfigKey = "Deployment:IsPlayground";

    /// <summary>
    /// ProblemDetails errorCode extension the frontend uses to tell "own admin setup required" apart
    /// from an ordinary 403 permission error and route to the setup screen instead.
    /// </summary>
    public const string SetupRequiredErrorCode = "SETUP_REQUIRED";
}
