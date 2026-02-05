using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class PutCommandHandler : IRequestHandler<PutCommand, IdentityProviderResource?>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IdentityProviderResource?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating identity provider: {Id}", request.Model.Id);

        var existingEntity = await _repository.Get(request.Model.Id);
        if (existingEntity == null)
        {
            _logger.LogWarning("Identity provider not found: {Id}", request.Model.Id);
            return null;
        }

        _mapper.UpdateEntity(request.Model, existingEntity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Updated identity provider: {Id}", request.Model.Id);

        return _mapper.ToResource(existingEntity);
    }
}
