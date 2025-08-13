using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ContractResource>, ContractResource>
    {
        private readonly ContractApplicationService _contractApplicationService;

        public GetQueryHandler(ContractApplicationService contractApplicationService)
        {
            _contractApplicationService = contractApplicationService;
        }

        public async Task<ContractResource> Handle(GetQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            return await _contractApplicationService.GetContractByIdAsync(request.Id, cancellationToken) ?? new ContractResource();
        }
    }
}