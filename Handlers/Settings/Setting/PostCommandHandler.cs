using AutoMapper;
using Klacks.Api.Commands.Settings.Settings;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Setting
{
  public class PostCommandHandler : IRequestHandler<PostCommand, Models.Settings.Settings?>
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

    public async Task<Models.Settings.Settings?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
      var res = await repository.AddSetting(request.model);
      await unitOfWork.CompleteAsync();
      return res;
    }
  }
}
