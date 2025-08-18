using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetQueryHandler : IRequestHandler<GetQuery<ClientResource>, ClientResource>
    {
        private readonly IClientRepository _clientRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IClientRepository clientRepository, IMapper mapper)
        {
            _clientRepository = clientRepository;
            _mapper = mapper;
        }

        public async Task<ClientResource> Handle(GetQuery<ClientResource> request, CancellationToken cancellationToken)
        {
            var client = await _clientRepository.Get(request.Id);
            
            if (client == null)
            {
                throw new KeyNotFoundException($"Employee with ID {request.Id} not found.");
            }
            
            return _mapper.Map<ClientResource>(client);
        }
    }
}
