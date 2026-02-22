// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Queries.IdentityProviders;

public record ListQuery : IRequest<IEnumerable<IdentityProviderListResource>>;
