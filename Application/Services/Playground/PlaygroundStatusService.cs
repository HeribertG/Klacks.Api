// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the Playground status answer. With the mode off it reports an inactive limit and mints nothing.
/// With the mode on, a visitor whose device header was valid gets no token back; every other caller gets
/// a new signed token. Minting is protected by the per-IP rate limit of the endpoint, and a device that
/// spends under a new token still draws on its IP bucket, so fresh tokens do not lift the IP cap.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>
/// <param name="tokens">Mints device tokens</param>

using Klacks.Api.Application.DTOs.Playground;
using Klacks.Api.Application.Interfaces.Playground;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Playground;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundStatusService : IPlaygroundStatusService
{
    private readonly IPlaygroundMode _mode;
    private readonly IPlaygroundDeviceTokenService _tokens;

    public PlaygroundStatusService(IPlaygroundMode mode, IPlaygroundDeviceTokenService tokens)
    {
        _mode = mode;
        _tokens = tokens;
    }

    public PlaygroundStatusResource GetStatus(PlaygroundVisitor? visitor)
    {
        if (!_mode.IsActive)
        {
            return new PlaygroundStatusResource(false, null);
        }

        return visitor?.DeviceId is null
            ? new PlaygroundStatusResource(true, _tokens.Issue())
            : new PlaygroundStatusResource(true, null);
    }
}
