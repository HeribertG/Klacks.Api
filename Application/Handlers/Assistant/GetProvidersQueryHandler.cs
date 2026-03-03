// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

using Klacks.Api.Application.Queries.Assistant;
namespace Klacks.Api.Application.Handlers.Assistant;

public class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, List<LLMProvider>>
{
    private readonly ILLMRepository _repository;

    public GetProvidersQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<LLMProvider>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetProvidersAsync();
    }
}