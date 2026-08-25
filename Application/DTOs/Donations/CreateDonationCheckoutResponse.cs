// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Donations;

/// <summary>
/// Response for a Stripe donation checkout session request.
/// </summary>
public class CreateDonationCheckoutResponse
{
    public string? Url { get; set; }

    public string? ErrorMessage { get; set; }
}
