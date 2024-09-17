using AutoMapper;
using Klacks_api.Commands.Settings.Macros;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.Settings.Macro
{
  public class DeleteCommandHandler : IRequestHandler<DeleteCommand, MacroResource>
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

    async Task<MacroResource> IRequestHandler<DeleteCommand, MacroResource>.Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
      var client = await repository.DeleteMacro(request.Id);

      await unitOfWork.CompleteAsync();

      return mapper.Map<Models.Settings.Macro, MacroResource>(client!);
    }
  }
}
