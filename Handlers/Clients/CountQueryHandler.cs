using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Clients;
using MediatR;

namespace Klacks.Api.Handlers.Clients
{
    public class CountQueryHandler : IRequestHandler<CountQuery, int>
    {
        private readonly IClientRepository repository;

        public CountQueryHandler(IClientRepository repository)
        {
            this.repository = repository;
        }

        public Task<int> Handle(CountQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(repository.Count());
        }
    }
}
