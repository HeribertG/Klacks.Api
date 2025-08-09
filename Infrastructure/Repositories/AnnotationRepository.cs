using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class AnnotationRepository : BaseRepository<Annotation>, IAnnotationRepository
    {
        private readonly DataBaseContext context;

        public AnnotationRepository(DataBaseContext context, ILogger<Annotation> logger)
            : base(context, logger)
        {
            this.context = context;
        }

        public async Task<List<Annotation>> SimpleList(Guid id)
        {
            return await this.context.Annotation.Where(x => x.ClientId == id).ToListAsync();
        }
    }
}
