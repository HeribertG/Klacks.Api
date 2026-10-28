// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads Deployment:IsPlayground. Absent, empty or unparsable means false, the secure default of every
/// customer deployment.
/// </summary>
/// <param name="configuration">Application configuration holding the Deployment:IsPlayground key</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Playground;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundMode : IPlaygroundMode
{
    private readonly IConfiguration _configuration;

    public PlaygroundMode(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsActive =>
        bool.TryParse(_configuration[DeploymentConstants.IsPlaygroundConfigKey], out var isPlayground) && isPlayground;
}
