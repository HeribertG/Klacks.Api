// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetProvidersQuery : IRequest<List<LLMProvider>>
{
}