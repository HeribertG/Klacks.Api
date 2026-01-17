using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakContexts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakContextResource>, BreakContextResource?>
{
    private readonly IBreakContextRepository _breakContextRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IBreakContextRepository breakContextRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakContextRepository = breakContextRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BreakContextResource?> Handle(PutCommand<BreakContextResource> request, CancellationToken cancellationToken)
    {
        var existingBreakContext = await _breakContextRepository.Get(request.Resource.Id);
        if (existingBreakContext == null)
        {
            return null;
        }

        _settingsMapper.UpdateBreakContextEntity(request.Resource, existingBreakContext);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToBreakContextResource(existingBreakContext);
    }
}
