using AutoMapper;
using Klacks.Api.Commands.Settings.MacrosTypes;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Settings.MacrosTypes;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand, Models.Settings.MacroType>
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

  async Task<Models.Settings.MacroType> IRequestHandler<DeleteCommand, Models.Settings.MacroType>.Handle(DeleteCommand request, CancellationToken cancellationToken)
  {
    var macroType = await repository.DeleteMacroType(request.Id);

    await unitOfWork.CompleteAsync();

    return macroType;
  }
}
