using AutoMapper;
using Klacks.Api.Commands.Settings.Vats;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Vat
{
  public class PutCommandHandler : IRequestHandler<PutCommand, Models.Settings.Vat?>
  {
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(IMapper mapper,
                              ISettingsRepository repository,
                              IUnitOfWork unitOfWork)
    {
      this.mapper = mapper;
      this.repository = repository;
      this.unitOfWork = unitOfWork;
    }

    public async Task<Models.Settings.Vat?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
      var dbVat = await repository.GetVAT(request.model.Id);
      var updatedVat = mapper.Map(request.model, dbVat);

      if (updatedVat != null)
      {
        updatedVat = repository.PutVAT(updatedVat);
        await unitOfWork.CompleteAsync();
        return updatedVat;
      }

      return null;
    }
  }
}
