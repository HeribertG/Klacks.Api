using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<IdentityProviderListResource>>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly ILogger<ListQueryHandler> _logger;

    public ListQueryHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        ILogger<ListQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<IdentityProviderListResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching identity providers list");

        var entities = await _repository.List();
        var sortedEntities = entities.OrderBy(e => e.SortOrder).ToList();

        _logger.LogInformation("Found {Count} identity providers", sortedEntities.Count);

        return _mapper.ToListResources(sortedEntities);
    }
}
