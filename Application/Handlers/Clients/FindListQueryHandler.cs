using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class FindListQueryHandler : IRequestHandler<FindListQuery, IEnumerable<Klacks.Api.Domain.Models.Staffs.Client>>
    {
        private readonly IClientRepository repository;
        private readonly IMapper mapper;

        public FindListQueryHandler(IClientRepository repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Staffs.Client>> Handle(FindListQuery request, CancellationToken cancellationToken)
        {
            return await repository.FindList(request.Company, request.Name, request.FirstName);
        }
    }
}
