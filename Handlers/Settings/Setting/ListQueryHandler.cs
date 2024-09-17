using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.Settings;
using MediatR;

namespace Klacks_api.Handlers.Settings.Setting
{
  public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Models.Settings.Settings>>
  {
    private readonly ISettingsRepository repository;

    public ListQueryHandler(ISettingsRepository repository)
    {
      this.repository = repository;
    }

    public async Task<IEnumerable<Models.Settings.Settings>> Handle(ListQuery request, CancellationToken cancellationToken) => await repository.GetSettingsList();
  }
}
