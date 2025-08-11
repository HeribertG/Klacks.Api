using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetListQueryHandler : IRequestHandler<ListQuery<ContractResource>, IEnumerable<ContractResource>>
    {
        private readonly IMapper mapper;
        private readonly IContractRepository repository;

        public GetListQueryHandler(IMapper mapper, IContractRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<ContractResource>> Handle(ListQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            var contracts = await repository.List();
            return mapper.Map<List<Klacks.Api.Domain.Models.Associations.Contract>, List<ContractResource>>(contracts!);
        }
    }
}