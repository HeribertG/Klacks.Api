using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.PeriodHours;

public class GetPeriodHoursQueryHandler : IRequestHandler<GetPeriodHoursQuery, Dictionary<Guid, PeriodHoursResource>>
{
    private readonly IPeriodHoursService _periodHoursService;

    public GetPeriodHoursQueryHandler(IPeriodHoursService periodHoursService)
    {
        _periodHoursService = periodHoursService;
    }

    public async Task<Dictionary<Guid, PeriodHoursResource>> Handle(GetPeriodHoursQuery query, CancellationToken cancellationToken)
    {
        return await _periodHoursService.GetPeriodHoursAsync(
            query.Request.ClientIds,
            query.Request.StartDate,
            query.Request.EndDate);
    }
}
