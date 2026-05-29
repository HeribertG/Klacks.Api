// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the most recent update/rollback operations as an audit list, newest first.
/// </summary>
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Mappers.Update;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class GetUpdateHistoryQueryHandler : IRequestHandler<GetUpdateHistoryQuery, IReadOnlyList<UpdateHistoryItem>>
{
    private const int MinTake = 1;
    private const int MaxTake = 100;

    private readonly IUpdateHistoryRepository _repository;

    public GetUpdateHistoryQueryHandler(IUpdateHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UpdateHistoryItem>> Handle(GetUpdateHistoryQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, MinTake, MaxTake);
        var entries = await _repository.GetRecentAsync(take, cancellationToken);
        return entries.Select(UpdateHistoryMapper.ToItem).ToList();
    }
}
