using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.Settings;
using MediatR;

namespace Klacks_api.Handlers.Settings.Setting
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
