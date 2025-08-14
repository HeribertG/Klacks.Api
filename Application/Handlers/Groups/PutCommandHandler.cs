using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var group = _mapper.Map<Klacks.Api.Domain.Models.Associations.Group>(request.Resource);
            var updatedGroup = await _groupRepository.Put(group);
            var result = _mapper.Map<GroupResource>(updatedGroup);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Group with ID {GroupId} updated successfully.", request.Resource.Id);

            return result;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Group with ID {GroupId} not found.", request.Resource.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating group with ID {GroupId}.", request.Resource.Id);
            throw;
        }
    }
}
