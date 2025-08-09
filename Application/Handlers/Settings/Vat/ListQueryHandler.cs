using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Vats;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Vat
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Models.Settings.Vat>>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;

        public ListQueryHandler(IMapper mapper, ISettingsRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<Models.Settings.Vat>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetVATList();
        }
    }
}
