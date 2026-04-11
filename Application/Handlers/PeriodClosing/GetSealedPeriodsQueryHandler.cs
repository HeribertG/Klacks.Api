// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Handler for building a per-day sealed-period summary by merging work and break sealing counts.
/// </summary>
public class GetSealedPeriodsQueryHandler : BaseHandler, IRequestHandler<GetSealedPeriodsQuery, List<SealedPeriodSummaryDto>>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;

    public GetSealedPeriodsQueryHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        ILogger<GetSealedPeriodsQueryHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
    }

    public async Task<List<SealedPeriodSummaryDto>> Handle(GetSealedPeriodsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workSummary = await _workRepository.GetSealingSummaryAsync(request.From, request.To, request.GroupId, cancellationToken);
            var breakSummary = await _breakRepository.GetSealingSummaryAsync(request.From, request.To, request.GroupId, cancellationToken);

            var result = new Dictionary<DateOnly, SealedPeriodSummaryDto>();

            foreach (var (date, total, sealed_) in workSummary)
            {
                if (!result.TryGetValue(date, out var dto))
                {
                    dto = new SealedPeriodSummaryDto { Date = date };
                    result[date] = dto;
                }

                dto.TotalWorkCount = total;
                dto.SealedWorkCount = sealed_;
            }

            foreach (var (date, total, sealed_) in breakSummary)
            {
                if (!result.TryGetValue(date, out var dto))
                {
                    dto = new SealedPeriodSummaryDto { Date = date };
                    result[date] = dto;
                }

                dto.TotalBreakCount = total;
                dto.SealedBreakCount = sealed_;
            }

            return result.Values.OrderBy(d => d.Date).ToList();
        },
        "loading sealed periods summary",
        new { request.From, request.To, request.GroupId });
    }
}
