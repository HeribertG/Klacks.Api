using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScheduleChanges;

public class GetListQueryHandler : BaseHandler, IRequestHandler<GetListQuery, List<ScheduleChangeResource>>
{
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public GetListQueryHandler(
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<GetListQueryHandler> logger)
        : base(logger)
    {
        _scheduleChangeTracker = scheduleChangeTracker;
    }

    public async Task<List<ScheduleChangeResource>> Handle(GetListQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var changes = await _scheduleChangeTracker.GetChangesAsync(request.StartDate, request.EndDate);
            return changes.Select(c => new ScheduleChangeResource
            {
                ClientId = c.ClientId,
                ChangeDate = c.ChangeDate
            }).ToList();
        }, nameof(Handle), new { request.StartDate, request.EndDate });
    }
}
