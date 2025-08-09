using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Breaks
{
    public class GetQueryHandler : IRequestHandler<GetQuery<BreakResource>, BreakResource>
    {
        private readonly IMapper mapper;
        private readonly IBreakRepository repository;

        public GetQueryHandler(
                               IMapper mapper,
                               IBreakRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
        {
            var breaks = await repository.Get(request.Id);
            return mapper.Map<Models.Schedules.Break, BreakResource>(breaks!);
        }
    }
}
