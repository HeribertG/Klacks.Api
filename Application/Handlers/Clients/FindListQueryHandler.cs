using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services;
using Klacks.Api.Domain.Models.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<Client>>
    {
        private readonly ClientApplicationService _clientApplicationService;

        public FindListQueryHandler(ClientApplicationService clientApplicationService)
        {
            _clientApplicationService = clientApplicationService;
        }

        public async Task<IEnumerable<Client>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            return await _clientApplicationService.FindClientsAsync(request.Company, request.Name, request.FirstName, cancellationToken);
        }
    }
}
