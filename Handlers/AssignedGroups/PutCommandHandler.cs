using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.AssignedGroups
{
    public class PutCommandHandler : IRequestHandler<PutCommand<AssignedGroupResource>, AssignedGroupResource?>
    {
        private readonly ILogger<PutCommandHandler> logger;
        private readonly IMapper mapper;
        private readonly IAssignedGroupRepository repository;
        private readonly IUnitOfWork unitOfWork;

        public PutCommandHandler(
                                  IMapper mapper,
                                  IAssignedGroupRepository repository,
                                  IUnitOfWork unitOfWork,
                                  ILogger<PutCommandHandler> logger)
        {
            this.mapper = mapper;
            this.repository = repository;
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        public async Task<AssignedGroupResource?> Handle(PutCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
        {
            try
            {
                var dbAssignedGroup = await repository.Get(request.Resource.Id);
                if (dbAssignedGroup == null)
                {
                    logger.LogWarning("AssignedGroup with ID {AssignedGroupId} not found.", request.Resource.Id);
                    return null;
                }

                var updatedAssignedGroup = mapper.Map(request.Resource, dbAssignedGroup);
                updatedAssignedGroup = await repository.Put(updatedAssignedGroup);
                await unitOfWork.CompleteAsync();

                logger.LogInformation("AssignedGroup with ID {AssignedGroupId} updated successfully.", request.Resource.Id);

                return mapper.Map<AssignedGroup, AssignedGroupResource>(updatedAssignedGroup);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating AssignedGroup with ID {AssignedGroupId}.", request.Resource.Id);
                throw;
            }
        }
    }
}
