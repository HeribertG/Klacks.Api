using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Absences;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly IMapper _mapper;
    private readonly IAbsenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IMapper mapper,
        IAbsenceRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _mapper = mapper;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AbsenceResource?> Handle(DeleteCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var absence = await _repository.Get(request.Id);
            if (absence == null)
            {
                throw new KeyNotFoundException($"Absence with ID {request.Id} not found.");
            }

            var absenceResource = _mapper.Map<AbsenceResource>(absence);
            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return absenceResource;
        },
        "deleting absence",
        new { AbsenceId = request.Id });
    }
}
