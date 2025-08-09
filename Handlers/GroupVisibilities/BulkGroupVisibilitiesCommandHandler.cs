using AutoMapper;
using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using MediatR;

namespace Klacks.Api.Handlers.GroupVisibilities;

public class BulkGroupVisibilitiesCommandHandler : IRequestHandler<BulkGroupVisibilitiesCommand>
{
    private readonly ILogger<BulkGroupVisibilitiesCommandHandler> logger; 
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public BulkGroupVisibilitiesCommandHandler(
        ILogger<BulkGroupVisibilitiesCommandHandler> logger,
        IMapper mapper,
        IGroupVisibilityRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.logger = logger;
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task Handle(BulkGroupVisibilitiesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting bulk update of GroupVisibility list with {Count} items.", request.List.Count);

        try
        {
            var convertedList = mapper.Map<List<GroupVisibility>>(request.List);

            await repository.SetGroupVisibilityList(convertedList);

            var updatedList = await repository.GetGroupVisibilityList();

            await unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating GroupVisibility list.");
            throw;
        }
    }
}
