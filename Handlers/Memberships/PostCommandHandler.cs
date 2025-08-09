using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Memberships;

public class PostCommandHandler : IRequestHandler<PostCommand<MembershipResource>, MembershipResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IMembershipRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IMembershipRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<MembershipResource?> Handle(PostCommand<MembershipResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var membership = mapper.Map<MembershipResource, Models.Associations.Membership>(request.Resource);

            await repository.Add(membership);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New membership added successfully. ID: {MembershipId}", membership.Id);

            return mapper.Map<Models.Associations.Membership, MembershipResource>(membership);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new membership.");
            throw;
        }
    }
}
