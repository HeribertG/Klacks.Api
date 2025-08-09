using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Memberships;

public class PutCommandHandler : IRequestHandler<PutCommand<MembershipResource>, MembershipResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IMembershipRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                             IMapper mapper,
                             IMembershipRepository repository,
                             IUnitOfWork unitOfWork,
                             ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<MembershipResource?> Handle(PutCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbMembership = await repository.Get(request.Resource.Id);
            if (dbMembership == null)
            {
                logger.LogWarning("Membership with ID {MembershipId} not found.", request.Resource.Id);
                return null;
            }

            var updatedMembership = mapper.Map(request.Resource, dbMembership);
            updatedMembership = await repository.Put(updatedMembership);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Membership with ID {MembershipId} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.Associations.Membership, MembershipResource>(updatedMembership);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating membership with ID {MembershipId}.", request.Resource.Id);
            throw;
        }
    }
}
