using AutoMapper;
using Klacks_api.Commands.Settings.Macros;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.Settings.Macro
{
  public class PostCommandHandler : IRequestHandler<PostCommand, MacroResource?>
  {
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(IMapper mapper,
                              ISettingsRepository repository,
                              IUnitOfWork unitOfWork)
    {
      this.mapper = mapper;
      this.repository = repository;
      this.unitOfWork = unitOfWork;
    }

    public async Task<MacroResource?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
      var macro = mapper.Map<MacroResource, Models.Settings.Macro>(request.model);

      repository.AddMacro(macro);

      await unitOfWork.CompleteAsync();

      return mapper.Map<Models.Settings.Macro, MacroResource>(macro);
    }
  }
}
