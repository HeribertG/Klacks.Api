// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists every stored standing approval, newest grant first, with IsActive computed from one single
/// "now" for the whole page - so two rows of the same answer can never be judged against two different
/// instants.
/// </summary>
/// <param name="repository">Reads the stored grants.</param>
/// <param name="timeProvider">The clock IsActive is decided against.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetStandingApprovalsQueryHandler
    : IRequestHandler<GetStandingApprovalsQuery, IReadOnlyList<StandingApprovalDto>>
{
    private readonly IStandingApprovalRepository _repository;
    private readonly TimeProvider _timeProvider;

    public GetStandingApprovalsQueryHandler(IStandingApprovalRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<StandingApprovalDto>> Handle(
        GetStandingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var stored = await _repository.GetAllAsync(cancellationToken);
        return StandingApprovalDtoMapper.ToDtos(stored, _timeProvider.GetUtcNow().UtcDateTime);
    }
}
