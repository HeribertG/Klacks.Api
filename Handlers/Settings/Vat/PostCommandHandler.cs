using AutoMapper;
using Klacks.Api.Commands.Settings.Vats;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Vat
{
  public class PostCommandHandler : IRequestHandler<PostCommand, Models.Settings.Vat?>
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

    public async Task<Models.Settings.Vat?> Handle(PostCommand request, CancellationToken cancellationToken)
    {
      repository.AddVAT(request.model);

      await unitOfWork.CompleteAsync();

      return request.model;
    }
  }
}
