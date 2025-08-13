using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PostCommandHandler : IRequestHandler<PostCommand<AddressResource>, AddressResource?>
{
    private readonly AddressApplicationService _addressApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        AddressApplicationService addressApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _addressApplicationService = addressApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(PostCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _addressApplicationService.CreateAddressAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new address. ID: {AddressId}", request.Resource.Id);
            throw;
        }
    }
}
