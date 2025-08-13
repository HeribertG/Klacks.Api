using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>
{
    private readonly SettingsApplicationService _settingsApplicationService;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(SettingsApplicationService settingsApplicationService,
                                IUnitOfWork unitOfWork)
    {
        _settingsApplicationService = settingsApplicationService;
        this.unitOfWork = unitOfWork;
    }

    async Task<Klacks.Api.Domain.Models.Settings.MacroType> IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>.Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        var macroType = await _settingsApplicationService.DeleteMacroTypeAsync(request.Id, cancellationToken);

        await unitOfWork.CompleteAsync();

        return macroType;
    }
}
