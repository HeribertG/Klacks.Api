using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class PostCommandHandler : IRequestHandler<PostCommand<AbsenceResource>, AbsenceResource?>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IAbsenceRepository absenceRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _absenceRepository = absenceRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AbsenceResource?> Handle(PostCommand<AbsenceResource> request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Processing create absence command");
            
            var absence = _mapper.Map<Absence>(request.Resource);
            await _absenceRepository.Add(absence);
            
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            var result = _mapper.Map<AbsenceResource>(absence);
            
            _logger.LogInformation("New absence added successfully. ID: {AbsenceId}", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction);
            _logger.LogError(ex, "Error occurred while adding a new absence. ID: {AbsenceId}", request.Resource.Id);
            throw;
        }
    }
}
