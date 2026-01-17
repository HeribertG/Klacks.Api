using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakContexts;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<BreakContextResource>, BreakContextResource?>
{
    private readonly IBreakContextRepository _breakContextRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IBreakContextRepository breakContextRepository,
        SettingsMapper settingsMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _breakContextRepository = breakContextRepository;
        _settingsMapper = settingsMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<BreakContextResource?> Handle(PostCommand<BreakContextResource> request, CancellationToken cancellationToken)
    {
        var breakContext = _settingsMapper.ToBreakContextEntity(request.Resource);
        await _breakContextRepository.Add(breakContext);
        await _unitOfWork.CompleteAsync();
        return _settingsMapper.ToBreakContextResource(breakContext);
    }
}
