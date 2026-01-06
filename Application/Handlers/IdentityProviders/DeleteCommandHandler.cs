using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, IdentityProviderResource?>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IdentityProviderResource?> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting identity provider: {Id}", request.Id);

        var entity = await _repository.Delete(request.Id);
        if (entity == null)
        {
            _logger.LogWarning("Identity provider not found: {Id}", request.Id);
            return null;
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Deleted identity provider: {Id}", request.Id);

        return _mapper.ToResource(entity);
    }
}
