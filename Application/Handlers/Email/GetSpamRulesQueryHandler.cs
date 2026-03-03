// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetSpamRulesQueryHandler : BaseHandler, IRequestHandler<GetSpamRulesQuery, List<SpamRuleResource>>
{
    private readonly ISpamRuleRepository _repository;

    public GetSpamRulesQueryHandler(
        ISpamRuleRepository repository,
        ILogger<GetSpamRulesQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<SpamRuleResource>> Handle(GetSpamRulesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rules = await _repository.GetAllAsync();
            var mapper = new SpamRuleMapper();
            return mapper.ToResources(rules);
        }, nameof(GetSpamRulesQuery));
    }
}
