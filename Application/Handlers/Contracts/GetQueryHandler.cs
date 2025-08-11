using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ContractResource>, ContractResource>
    {
        private readonly IMapper mapper;
        private readonly IContractRepository repository;

        public GetQueryHandler(IMapper mapper,
                               IContractRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<ContractResource> Handle(GetQuery<ContractResource> request, CancellationToken cancellationToken)
        {
            var contract = await repository.Get(request.Id);
            return mapper.Map<Klacks.Api.Domain.Models.Associations.Contract, ContractResource>(contract!);
        }
    }
}