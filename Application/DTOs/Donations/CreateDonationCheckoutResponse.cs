// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Donations;

/// <summary>
/// Response for a Stripe donation checkout session request.
/// </summary>
public class CreateDonationCheckoutResponse
{
    public string? Url { get; set; }

    public string? ErrorMessage { get; set; }
}
