using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class PostCommandHandler : IRequestHandler<PostCommand, IdentityProviderResource?>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IdentityProviderResource?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new identity provider: {Name}", request.Model.Name);

        var entity = _mapper.ToEntity(request.Model);
        entity.Id = Guid.NewGuid();

        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Created identity provider with ID: {Id}", entity.Id);

        return _mapper.ToResource(entity);
    }
}
