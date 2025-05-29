using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Addresses;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AddressResource>, AddressResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IAddressRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AddressResource?> Handle(DeleteCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var address = await repository.Delete(request.Id);
            if (address == null)
            {
                logger.LogWarning("Address with ID {AddressId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Address with ID {AddressId} deleted successfully.", request.Id);

            return mapper.Map<Models.Staffs.Address, AddressResource>(address);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting address with ID {AddressId}.", request.Id);
            throw;
        }
    }
}
