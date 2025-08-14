using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States
{
    public class GetQueryHandler : IRequestHandler<GetQuery<StateResource>, StateResource?>
    {
        private readonly IStateRepository _stateRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IStateRepository stateRepository, IMapper mapper)
        {
            _stateRepository = stateRepository;
            _mapper = mapper;
        }

        public async Task<StateResource?> Handle(GetQuery<StateResource> request, CancellationToken cancellationToken)
        {
            var state = await _stateRepository.Get(request.Id);
            return state != null ? _mapper.Map<StateResource>(state) : null;
        }
    }
}
