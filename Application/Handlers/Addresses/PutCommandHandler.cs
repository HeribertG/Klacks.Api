using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Addresses;

public class PutCommandHandler : IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly AddressApplicationService _addressApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        AddressApplicationService addressApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _addressApplicationService = addressApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AddressResource?> Handle(PutCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingAddress = await _addressApplicationService.GetAddressByIdAsync(request.Resource.Id, cancellationToken);
            if (existingAddress == null)
            {
                _logger.LogWarning("Address with ID {AddressId} not found.", request.Resource.Id);
                return null;
            }

            var result = await _addressApplicationService.UpdateAddressAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating address with ID {AddressId}.", request.Resource.Id);
            throw;
        }
    }
}
