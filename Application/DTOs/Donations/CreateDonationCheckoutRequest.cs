// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Donations;

/// <summary>
/// Request for creating a Stripe donation checkout session.
/// </summary>
public class CreateDonationCheckoutRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;
}
