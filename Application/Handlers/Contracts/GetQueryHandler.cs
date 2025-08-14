using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ContractResource>, ContractResource>
    {
        private readonly IContractRepository _contractRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IContractRepository contractRepository, IMapper mapper)
        {
            _contractRepository = contractRepository;
            _mapper = mapper;
        }

        public async Task<ContractResource> Handle(GetQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            var contract = await _contractRepository.Get(request.Id);
            return contract != null ? _mapper.Map<ContractResource>(contract) : new ContractResource();
        }
    }
}