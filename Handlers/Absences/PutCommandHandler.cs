using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Absences;

public class PutCommandHandler : IRequestHandler<PutCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAbsenceRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IAbsenceRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AbsenceResource?> Handle(PutCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbAbsence = await repository.Get(request.Resource.Id);
            if (dbAbsence == null)
            {
                logger.LogWarning("Absence with ID {AbsenceId} not found.", request.Resource.Id);
                return null;
            }

            var updatedAbsence = mapper.Map(request.Resource, dbAbsence);
            updatedAbsence = await repository.Put(updatedAbsence);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Absence with ID {AbsenceId} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.Schedules.Absence, AbsenceResource>(updatedAbsence);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating absence with ID {AbsenceId}.", request.Resource.Id);
            throw;
        }
    }
}
