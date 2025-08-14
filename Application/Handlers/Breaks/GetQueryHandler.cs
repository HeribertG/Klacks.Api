using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks
{
    public class GetQueryHandler : IRequestHandler<GetQuery<BreakResource>, BreakResource>
    {
        private readonly IBreakRepository _breakRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IBreakRepository breakRepository, IMapper mapper)
        {
            _breakRepository = breakRepository;
            _mapper = mapper;
        }

        public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
        {
            var breakEntity = await _breakRepository.Get(request.Id);
            return breakEntity != null ? _mapper.Map<BreakResource>(breakEntity) : new BreakResource();
        }
    }
}
