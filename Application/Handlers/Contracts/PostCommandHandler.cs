using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PostCommandHandler : IRequestHandler<PostCommand<ContractResource>, ContractResource?>
{
    private readonly ContractApplicationService _contractApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        ContractApplicationService contractApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _contractApplicationService = contractApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ContractResource?> Handle(PostCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _contractApplicationService.CreateContractAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new contract.");
            throw;
        }
    }
}