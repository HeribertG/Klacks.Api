// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Configuration;

/// <summary>
/// Configuration for the optional Stripe donation checkout.
/// All values are empty/disabled by default so self-hosted instances
/// are not forced to use Stripe.
/// </summary>
public class StripeSettings
{
    public const string SectionName = "Stripe";

    public bool Enabled { get; set; }

    public string SecretKey { get; set; } = string.Empty;

    public string SuccessUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;
}
