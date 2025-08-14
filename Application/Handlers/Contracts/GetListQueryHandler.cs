using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<ContractResource>, IEnumerable<ContractResource>>
    {
        private readonly IContractRepository _contractRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(IContractRepository contractRepository, IMapper mapper)
        {
            _contractRepository = contractRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ContractResource>> Handle(ListQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            var contracts = await _contractRepository.List();
            return _mapper.Map<IEnumerable<ContractResource>>(contracts);
        }
    }
}