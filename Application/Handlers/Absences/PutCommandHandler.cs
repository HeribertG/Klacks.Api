using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<AbsenceResource>, AbsenceResource?>
{    
    private readonly IMapper _mapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IAbsenceRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _mapper = mapper;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceResource?> Handle(PutCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var dbAbsence = await _repository.Get(request.Resource.Id);
            if (dbAbsence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Resource.Id} not found.");
            }

            var updatedAbsence = _mapper.Map(request.Resource, dbAbsence);
            updatedAbsence = await _repository.Put(updatedAbsence);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<AbsenceResource>(updatedAbsence);
        }, 
        "updating absence", 
        new { AbsenceId = request.Resource.Id });
    }
}
