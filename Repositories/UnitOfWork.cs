using Klacks.Api.Datas;
using Klacks.Api.Interfaces;

namespace Klacks.Api.Repositories
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
