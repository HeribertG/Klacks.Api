using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakContexts;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<BreakContextResource>, BreakContextResource?>
{
    private readonly IBreakContextRepository _breakContextRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IBreakContextRepository breakContextRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakContextRepository = breakContextRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BreakContextResource?> Handle(DeleteCommand<BreakContextResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting break context with ID: {BreakContextId}", request.Id);

        var existingBreakContext = await _breakContextRepository.Get(request.Id);
        if (existingBreakContext == null)
        {
            _logger.LogWarning("BreakContext not found: {BreakContextId}", request.Id);
            return null;
        }

        var breakContextResource = _settingsMapper.ToBreakContextResource(existingBreakContext);

        await _breakContextRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("BreakContext deleted successfully: {BreakContextId}", request.Id);
        return breakContextResource;
    }
}
