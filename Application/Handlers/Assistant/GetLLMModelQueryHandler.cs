// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetLLMModelQueryHandler : BaseHandler, IRequestHandler<GetQuery<LLMModel>, LLMModel>
{
    private readonly ILLMRepository _repository;

    public GetLLMModelQueryHandler(ILLMRepository repository, ILogger<GetLLMModelQueryHandler> logger) : base(logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(GetQuery<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var model = await _repository.Get(request.Id);
            if (model == null)
            {
                throw new KeyNotFoundException($"LLM Model with ID {request.Id} not found");
            }
            return model;
        }, "GetLLMModel", request.Id);
    }
}
