using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class CountryRepository : BaseRepository<Countries>, ICountryRepository
    {
        private readonly DataBaseContext context;

        public CountryRepository(DataBaseContext context, ILogger<Countries> logger)
            : base(context, logger)
        {
            this.context = context;
        }
    }
}
