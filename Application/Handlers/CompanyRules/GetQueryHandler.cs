// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading a single applied company rule by its registry id.
/// </summary>
/// <param name="request">Contains the registry id of the company rule.</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.CompanyRules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CompanyRules;

public class GetQueryHandler : IRequestHandler<GetCompanyRuleQuery, CompanyRuleResource?>
{
    private readonly ICompanyRuleRepository _repository;
    private readonly SettingsMapper _mapper;

    public GetQueryHandler(ICompanyRuleRepository repository, SettingsMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CompanyRuleResource?> Handle(GetCompanyRuleQuery request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetAsync(request.Id);
        return rule is null ? null : _mapper.ToCompanyRuleResource(rule);
    }
}
