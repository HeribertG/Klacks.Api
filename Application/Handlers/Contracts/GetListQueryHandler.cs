using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<ContractResource>, IEnumerable<ContractResource>>
    {
        private readonly ContractApplicationService _contractApplicationService;

        public GetListQueryHandler(ContractApplicationService contractApplicationService)
        {
            _contractApplicationService = contractApplicationService;
        }

        public async Task<IEnumerable<ContractResource>> Handle(ListQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            return await _contractApplicationService.GetAllContractsAsync(cancellationToken);
        }
    }
}