// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks the onboarding state save action: on the public Playground a save of the installation-wide setup tour
/// (kind "setup" or empty) is answered with HTTP 403, while the per-account intro tour still goes through. Without the
/// Playground mode it is a pass-through.
/// </summary>

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BlockedSetupOnboardingOnPlaygroundAttribute : ServiceFilterAttribute
{
    public BlockedSetupOnboardingOnPlaygroundAttribute()
        : base(typeof(BlockedSetupOnboardingOnPlaygroundFilter))
    {
    }
}
