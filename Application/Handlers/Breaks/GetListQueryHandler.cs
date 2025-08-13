using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetListQueryHandler(ClientApplicationService clientApplicationService) : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
{
    private readonly ClientApplicationService _clientApplicationService = clientApplicationService;

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        return await _clientApplicationService.GetBreakListAsync(request.Filter, cancellationToken);
    }
}
