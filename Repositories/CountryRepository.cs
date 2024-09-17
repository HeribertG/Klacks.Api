using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Settings;

namespace Klacks_api.Repositories
{
  public class CountryRepository : BaseRepository<Countries>, ICountryRepository
  {
    private readonly DataBaseContext context;

    public CountryRepository(DataBaseContext context)
        : base(context)
    {
      this.context = context;
    }
  }
}
