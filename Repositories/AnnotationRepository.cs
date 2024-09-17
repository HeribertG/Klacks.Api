using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class AnnotationRepository : BaseRepository<Annotation>, IAnnotationRepository
  {
    private readonly DataBaseContext context;

    public AnnotationRepository(DataBaseContext context)
        : base(context)
    {
      this.context = context;
    }

    public async Task<List<Annotation>> SimpleList(Guid id)
    {
      return await this.context.Annotation.Where(x => x.ClientId == id).ToListAsync();
    }
  }
}
