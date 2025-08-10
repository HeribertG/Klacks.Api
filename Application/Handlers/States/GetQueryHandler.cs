using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States
{
    public class GetQueryHandler : IRequestHandler<GetQuery<StateResource>, StateResource?>
    {
        private readonly IMapper mapper;
        private readonly IStateRepository repository;

        public GetQueryHandler(IMapper mapper, IStateRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<StateResource?> Handle(GetQuery<StateResource> request, CancellationToken cancellationToken)
        {
            var state = await this.repository.Get(request.Id);
            return this.mapper.Map<Klacks.Api.Domain.Models.Settings.State, StateResource>(state!);
        }
    }
}
