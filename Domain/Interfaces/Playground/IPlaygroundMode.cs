// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Playground;

/// <summary>
/// Tells whether this instance is the public Playground (configuration key Deployment:IsPlayground).
/// Every Playground-only behaviour asks this and does nothing at all when it answers false, so customer
/// installations behave exactly as before.
/// </summary>
public interface IPlaygroundMode
{
    bool IsActive { get; }
}
