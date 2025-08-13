using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly WorkApplicationService _workApplicationService;

    public PutCommandHandler(WorkApplicationService workApplicationService)
    {
        _workApplicationService = workApplicationService;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await _workApplicationService.UpdateWorkAsync(request.Resource, cancellationToken);
    }
}
