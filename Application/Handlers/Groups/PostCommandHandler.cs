using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class PostCommandHandler : IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly GroupApplicationService _groupApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        GroupApplicationService groupApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _groupApplicationService = groupApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var createdGroup = await _groupApplicationService.CreateGroupAsync(request.Resource, cancellationToken);

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