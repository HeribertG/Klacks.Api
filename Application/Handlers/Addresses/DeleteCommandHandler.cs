using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AddressResource>, AddressResource?>
{
    private readonly AddressApplicationService _addressApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        AddressApplicationService addressApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _addressApplicationService = addressApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(DeleteCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAddress = await _addressApplicationService.GetAddressByIdAsync(request.Id, cancellationToken);
            if (existingAddress == null)
            {
                _logger.LogWarning("Address with ID {AddressId} not found for deletion.", request.Id);
                return null;
            }

            await _addressApplicationService.DeleteAddressAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingAddress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting address with ID {AddressId}.", request.Id);
            throw;
        }
    }
}
