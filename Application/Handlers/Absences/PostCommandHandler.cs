using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMapper _mapper;

    public PostCommandHandler(
        IAbsenceRepository absenceRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _absenceRepository = absenceRepository;
        _mapper = mapper;
    }

    public async Task<AbsenceResource?> Handle(PostCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        ValidateAbsenceRequest(request.Resource);

        return await ExecuteWithTransactionAsync(async () =>
        {
            var absence = _mapper.Map<Absence>(request.Resource);
            await _absenceRepository.Add(absence);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AbsenceResource>(absence);
        }, 
        "creating absence", 
        new { AbsenceId = request.Resource?.Id });
    }

    private void ValidateAbsenceRequest(AbsenceResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Absence data is required.");
        }

        if (resource.Name == null ||
            (string.IsNullOrWhiteSpace(resource.Name.De) &&
             string.IsNullOrWhiteSpace(resource.Name.En) &&
             string.IsNullOrWhiteSpace(resource.Name.Fr) &&
             string.IsNullOrWhiteSpace(resource.Name.It)))
        {
            throw new InvalidRequestException("Absence name is required in at least one language.");
        }
    }
}
