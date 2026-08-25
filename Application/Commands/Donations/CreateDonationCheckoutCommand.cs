// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Donations;

public record CreateDonationCheckoutCommand(CreateDonationCheckoutRequest Request)
    : IRequest<CreateDonationCheckoutResponse>;
