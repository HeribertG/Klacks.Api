using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Addresses;

public class PutCommandHandler : IRequestHandler<PutCommand<AddressResource>, AddressResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IAddressRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AddressResource?> Handle(PutCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbAddress = await repository.Get(request.Resource.Id);
            if (dbAddress == null)
            {
                logger.LogWarning("Address with ID {AddressId} not found.", request.Resource.Id);
                return null;
            }

            var updatedAddress = mapper.Map(request.Resource, dbAddress);
            updatedAddress = await repository.Put(updatedAddress);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Address with ID {AddressId} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.Staffs.Address, AddressResource>(updatedAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating address with ID {AddressId}.", request.Resource.Id);
            throw;
        }
    }
}
