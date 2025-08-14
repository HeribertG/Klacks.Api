using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class PostCommandHandler : IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var group = _mapper.Map<Klacks.Api.Domain.Models.Associations.Group>(request.Resource);
            await _groupRepository.Add(group);
            var createdGroup = _mapper.Map<GroupResource>(group);

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);

            _logger.LogInformation("Command {CommandName} processed successfully.", "PostCommand<GroupResource>");

            return createdGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group: {ErrorMessage}", ex.Message);
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}