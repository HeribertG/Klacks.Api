using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(DeleteCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupRepository.Get(request.Id);
            if (group == null)
            {
                _logger.LogWarning("Group with ID {GroupId} not found for deletion.", request.Id);
                return null;
            }

            var groupToDelete = _mapper.Map<GroupResource>(group);
            await _groupRepository.Delete(request.Id);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Group with ID {GroupId} deleted successfully.", request.Id);

            return groupToDelete;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Group with ID {GroupId} not found for deletion.", request.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting group with ID {GroupId}.", request.Id);
            throw;
        }
    }
}
