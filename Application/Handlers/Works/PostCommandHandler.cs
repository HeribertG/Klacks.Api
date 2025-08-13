using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly WorkApplicationService _workApplicationService;

    public PostCommandHandler(WorkApplicationService workApplicationService)
    {
        _workApplicationService = workApplicationService;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await _workApplicationService.CreateWorkAsync(request.Resource, cancellationToken);
    }
}
