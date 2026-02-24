// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetGlobalRulesQueryHandler : IRequestHandler<GetGlobalRulesQuery, object>
{
    private readonly IGlobalAgentRuleRepository _ruleRepository;

    public GetGlobalRulesQueryHandler(IGlobalAgentRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository;
    }

    public async Task<object> Handle(GetGlobalRulesQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeHistory)
        {
            var history = await _ruleRepository.GetHistoryAsync(request.HistoryLimit, cancellationToken);
            return history.Select(h => new
            {
                h.Id, h.Name, h.ContentBefore, h.ContentAfter,
                h.Version, h.ChangeType, h.ChangedBy, h.CreateTime
            }).ToList();
        }

        var rules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);
        return rules.Select(r => new
        {
            r.Id, r.Name, r.Content, r.SortOrder,
            r.IsActive, r.Version, r.Source, r.CreateTime
        }).ToList();
    }
}
