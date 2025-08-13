using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class GetQueryHandler : IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
    private readonly WorkApplicationService _workApplicationService;

    public GetQueryHandler(WorkApplicationService workApplicationService)
    {
        _workApplicationService = workApplicationService;
    }

    public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
    {
        return await _workApplicationService.GetWorkByIdAsync(request.Id, cancellationToken) ?? new WorkResource();
    }
}
