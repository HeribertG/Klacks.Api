// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="ListQuery"/>. Returns all active qualifications ordered by name.
/// </summary>
/// <param name="repository">Provides ordered, non-tracking qualification query</param>

using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Qualification>>
{
    private readonly IQualificationRepository _repository;

    public ListQueryHandler(IQualificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Qualification>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }
}
