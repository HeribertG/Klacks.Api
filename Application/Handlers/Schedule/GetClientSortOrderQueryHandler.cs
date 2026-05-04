// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the persisted client sort order for a user and group, mapped to DTOs.
/// </summary>

using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedule;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Schedule;

public class GetClientSortOrderQueryHandler
    : BaseHandler, IRequestHandler<GetClientSortOrderQuery, List<ClientSortOrderDto>>
{
    private readonly IClientSortPreferenceRepository _repository;

    public GetClientSortOrderQueryHandler(
        IClientSortPreferenceRepository repository,
        ILogger<GetClientSortOrderQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<ClientSortOrderDto>> Handle(
        GetClientSortOrderQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entries = await _repository.GetByUserAndGroupAsync(
                request.UserId, request.GroupId, cancellationToken);

            return entries
                .Select(e => new ClientSortOrderDto(e.ClientId, e.SortOrder))
                .ToList();
        }, "get client sort order", new { request.UserId, request.GroupId });
    }
}
