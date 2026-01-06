using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class GetQueryHandler : IRequestHandler<GetQuery, IdentityProviderResource?>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        ILogger<GetQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IdentityProviderResource?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching identity provider with ID: {Id}", request.Id);

        var entity = await _repository.Get(request.Id);
        if (entity == null)
        {
            _logger.LogWarning("Identity provider not found: {Id}", request.Id);
            return null;
        }

        return _mapper.ToResource(entity);
    }
}
