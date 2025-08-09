using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Addresses;

public class PostCommandHandler : IRequestHandler<PostCommand<AddressResource>, AddressResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAddressRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IAddressRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AddressResource?> Handle(PostCommand<AddressResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var address = mapper.Map<AddressResource, Models.Staffs.Address>(request.Resource);

            await repository.Add(address);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New address added successfully. ID: {AddressId}", address.Id);

            return mapper.Map<Models.Staffs.Address, AddressResource>(address);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new address. ID: {AddressId}", request.Resource.Id);
            throw;
        }
    }
}
