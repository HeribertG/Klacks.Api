using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(ISettingsRepository settingsRepository,
                                IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        this.unitOfWork = unitOfWork;
    }

    async Task<Klacks.Api.Domain.Models.Settings.MacroType> IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>.Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        var macroType = await _settingsRepository.DeleteMacroType(request.Id);

        await unitOfWork.CompleteAsync();

        return macroType;
    }
}
