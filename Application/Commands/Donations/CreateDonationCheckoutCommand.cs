// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Donations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Donations;

public record CreateDonationCheckoutCommand(CreateDonationCheckoutRequest Request)
    : IRequest<CreateDonationCheckoutResponse>;
