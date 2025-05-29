using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;

namespace Klacks.Api.Repositories
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
