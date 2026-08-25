// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Donations;
using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

/// <summary>
/// Endpoints for the donation feature (Stripe checkout).
/// </summary>
[ApiController]
public class DonationController : BaseController
{
    private readonly IMediator mediator;

    public DonationController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost("checkout-session")]
    public async Task<ActionResult<CreateDonationCheckoutResponse>> CreateCheckoutSession(
        [FromBody] CreateDonationCheckoutRequest request)
    {
        var response = await this.mediator.Send(new CreateDonationCheckoutCommand(request));

        if (response.Url == null)
        {
            return BadRequest(new { message = response.ErrorMessage ?? "Donation checkout is not available." });
        }

        return Ok(response);
    }
}
