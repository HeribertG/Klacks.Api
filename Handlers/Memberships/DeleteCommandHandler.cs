using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Memberships;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<MembershipResource>, MembershipResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IMembershipRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IMembershipRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<MembershipResource?> Handle(DeleteCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var membership = await repository.Delete(request.Id);
            if (membership == null)
            {
                logger.LogWarning("Membership with ID {MembershipId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Membership with ID {MembershipId} deleted successfully.", request.Id);

            return mapper.Map<Models.Associations.Membership, MembershipResource>(membership);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting membership with ID {MembershipId}.", request.Id);
            throw;
        }
    }
}
