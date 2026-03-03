// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetProviderByIdQuery : IRequest<LLMProvider?>
{
    public Guid Id { get; set; }

    public GetProviderByIdQuery(Guid id)
    {
        Id = id;
    }
}