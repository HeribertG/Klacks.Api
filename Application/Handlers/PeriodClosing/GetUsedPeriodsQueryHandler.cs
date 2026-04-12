// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Handler that returns distinct (StartDate, EndDate, PaymentInterval) tuples
/// for billing periods that are actually populated with non-deleted work or
/// break entries on non-deleted clients.
/// </summary>
public class GetUsedPeriodsQueryHandler : BaseHandler, IRequestHandler<GetUsedPeriodsQuery, List<UsedPeriodDto>>
{
    private readonly IWorkRepository _workRepository;

    public GetUsedPeriodsQueryHandler(
        IWorkRepository workRepository,
        ILogger<GetUsedPeriodsQueryHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
    }

    public async Task<List<UsedPeriodDto>> Handle(GetUsedPeriodsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () => await _workRepository.GetUsedPeriodsAsync(cancellationToken),
            "loading used billing periods");
    }
}
