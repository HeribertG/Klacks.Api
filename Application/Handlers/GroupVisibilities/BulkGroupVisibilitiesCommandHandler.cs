using AutoMapper;
using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class BulkGroupVisibilitiesCommandHandler : IRequestHandler<BulkGroupVisibilitiesCommand>
{
    private readonly ILogger<BulkGroupVisibilitiesCommandHandler> _logger; 
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public BulkGroupVisibilitiesCommandHandler(
        ILogger<BulkGroupVisibilitiesCommandHandler> logger,
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BulkGroupVisibilitiesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk update of GroupVisibility list with {Count} items.", request.List.Count);

        try
        {
            var groupVisibilities = _mapper.Map<List<GroupVisibility>>(request.List);
            await _groupVisibilityRepository.SetGroupVisibilityList(groupVisibilities);
            await _unitOfWork.CompleteAsync();
            
            _logger.LogInformation("Bulk update of GroupVisibility list completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating GroupVisibility list.");
            throw;
        }
    }
}
