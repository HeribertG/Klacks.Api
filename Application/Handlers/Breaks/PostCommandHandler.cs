using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PostCommandHandler : IRequestHandler<PostCommand<BreakResource>, BreakResource?>
{
    private readonly BreakApplicationService _breakApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        BreakApplicationService breakApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _breakApplicationService = breakApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(PostCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _breakApplicationService.CreateBreakAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new break. ID: {BreakId}", request.Resource.Id);
            throw;
        }
    }
}
