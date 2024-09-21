using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Setting
{
  public class GetQueryHandler : IRequestHandler<GetQuery, Models.Settings.Settings?>
  {
    private readonly ISettingsRepository repository;

    public GetQueryHandler(ISettingsRepository repository)
    {
      this.repository = repository;
    }

    public async Task<Models.Settings.Settings?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
      return await repository.GetSetting(request.Type);
    }
  }
}
