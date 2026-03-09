// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ListLLMModelsQueryHandler : BaseHandler, IRequestHandler<ListQuery<LLMModel>, IEnumerable<LLMModel>>
{
    private readonly ILLMRepository _repository;

    public ListLLMModelsQueryHandler(ILLMRepository repository, ILogger<ListLLMModelsQueryHandler> logger) : base(logger)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LLMModel>> Handle(ListQuery<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() =>
        {
            return _repository.GetModelsAsync(onlyEnabled: false);
        }, "ListLLMModels");
    }
}
