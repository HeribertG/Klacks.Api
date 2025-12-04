using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand, Domain.Models.Settings.MacroType>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ISettingsRepository settingsRepository,
                                IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    async Task<Domain.Models.Settings.MacroType> IRequestHandler<DeleteCommand, Domain.Models.Settings.MacroType>.Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        var macroType = await _settingsRepository.DeleteMacroType(request.Id);

        await _unitOfWork.CompleteAsync();

        return macroType;
    }
}
