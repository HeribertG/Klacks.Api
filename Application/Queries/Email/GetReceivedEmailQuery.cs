// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Email;

public record GetReceivedEmailQuery(Guid Id) : IRequest<ReceivedEmailResource?>;
