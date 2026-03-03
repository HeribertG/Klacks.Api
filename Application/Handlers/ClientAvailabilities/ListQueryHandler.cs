// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListClientAvailabilitiesQuery, IEnumerable<ClientAvailabilityResource>>
{
    private readonly IClientAvailabilityRepository _repository;
    private readonly ClientAvailabilityMapper _mapper;

    public ListQueryHandler(
        IClientAvailabilityRepository repository,
        ClientAvailabilityMapper mapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClientAvailabilityResource>> Handle(
        ListClientAvailabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _repository.GetByDateRange(request.StartDate, request.EndDate);
            return entities.Select(_mapper.ToResource);
        }, "ListClientAvailabilities", new { request.StartDate, request.EndDate });
    }
}
