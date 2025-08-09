using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;
using Klacks.Api.Application.Queries.Communications;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications
{
    public class GetTypeQueryHandler : IRequestHandler<GetTypeQuery, IEnumerable<CommunicationType>>
    {
        private readonly ICommunicationRepository repository;

        public GetTypeQueryHandler(ICommunicationRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IEnumerable<CommunicationType>> Handle(GetTypeQuery request, CancellationToken cancellationToken)
        {
            return await repository.TypeList();
        }
    }
}
