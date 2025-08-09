using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients
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
