using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.Vats;
using MediatR;

namespace Klacks_api.Handlers.Settings.Vat
{
  public class GetQueryHandler : IRequestHandler<GetQuery, Models.Settings.Vat?>
  {
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<Models.Settings.Vat?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
      return await repository.GetVAT(request.Id);
    }
  }
}
