// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all active (non-deleted) applied company rules, newest first.
/// </summary>
/// <param name="request">Takes no parameters.</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CompanyRules;

public class GetListQueryHandler : IRequestHandler<ListQuery<CompanyRuleResource>, IEnumerable<CompanyRuleResource>>
{
    private readonly ICompanyRuleRepository _repository;
    private readonly SettingsMapper _mapper;

    public GetListQueryHandler(ICompanyRuleRepository repository, SettingsMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CompanyRuleResource>> Handle(ListQuery<CompanyRuleResource> request, CancellationToken cancellationToken)
    {
        var rules = await _repository.GetAllActiveAsync();
        return rules.Select(_mapper.ToCompanyRuleResource).ToList();
    }
}
