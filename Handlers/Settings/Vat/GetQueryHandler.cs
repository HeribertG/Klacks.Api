using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Vat
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Models.Settings.Vat?>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;

        public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<Models.Settings.Vat?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetVAT(request.Id);
        }
    }
}
