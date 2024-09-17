using Klacks_api.Datas;
using Klacks_api.Interfaces;

namespace Klacks_api.Repositories
{
  public class UnitOfWork : IUnitOfWork
  {
    private readonly DataBaseContext context;

    public UnitOfWork(DataBaseContext context)
    {
      this.context = context;
    }

    public async Task CompleteAsync()
    {
      await context.SaveChangesAsync();
    }

    public int Complete()
    {
      return context.SaveChanges();
    }
  }
}
