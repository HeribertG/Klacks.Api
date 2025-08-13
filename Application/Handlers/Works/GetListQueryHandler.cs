using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly WorkApplicationService _workApplicationService;

    public GetListQueryHandler(WorkApplicationService workApplicationService)
    {
        _workApplicationService = workApplicationService;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _workApplicationService.GetAllWorksAsync(request.Filter, cancellationToken);
    }
}
