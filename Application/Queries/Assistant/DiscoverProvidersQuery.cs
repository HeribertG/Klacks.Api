// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class DiscoverProvidersQuery : IRequest<List<ProviderCandidateResource>>
{
}
