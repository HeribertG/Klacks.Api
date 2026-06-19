// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
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
    private readonly ISealedDayRepository _sealedDayRepository;

    public GetSealedPeriodsQueryHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        ISealedDayRepository sealedDayRepository,
        ILogger<GetSealedPeriodsQueryHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _sealedDayRepository = sealedDayRepository;
    }

    public async Task<List<SealedPeriodSummaryDto>> Handle(GetSealedPeriodsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workSummary = await _workRepository.GetSealingSummaryAsync(request.From, request.To, request.GroupId, cancellationToken);
            var breakSummary = await _breakRepository.GetSealingSummaryAsync(request.From, request.To, request.GroupId, cancellationToken);
            var sealedDays = await _sealedDayRepository.GetRangeAsync(request.From, request.To, request.GroupId, cancellationToken);
            var sealedDayDates = new HashSet<DateOnly>(sealedDays.Select(s => s.Date));

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

            foreach (var date in sealedDayDates)
            {
                if (!result.TryGetValue(date, out var dto))
                {
                    dto = new SealedPeriodSummaryDto { Date = date };
                    result[date] = dto;
                }
                dto.IsDaySealed = true;
            }

            return result.Values.OrderBy(d => d.Date).ToList();
        },
        "loading sealed periods summary",
        new { request.From, request.To, request.GroupId });
    }
}
