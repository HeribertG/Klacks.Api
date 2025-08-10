using AutoMapper;
using Klacks.Api.Application.Commands.Settings.MacrosTypes;
using Klacks.Api.Application.Interfaces;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>
{
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(IMapper mapper,
                                ISettingsRepository repository,
                                IUnitOfWork unitOfWork)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    async Task<Klacks.Api.Domain.Models.Settings.MacroType> IRequestHandler<DeleteCommand, Klacks.Api.Domain.Models.Settings.MacroType>.Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        var macroType = await repository.DeleteMacroType(request.Id);

        await unitOfWork.CompleteAsync();

        return macroType;
    }
}
