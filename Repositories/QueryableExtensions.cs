using Klacks.Api.Datas;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Klacks.Api.Repositories
{
  public static class QueryableExtensions
  {
    public static IQueryable<TEntity> IncludeMultiple<TEntity>(
        this IQueryable<TEntity> query, params Expression<Func<TEntity, object>>[] includes)
        where TEntity : BaseEntity
    {
      foreach (var include in includes)
      {
        query = query.Include(include);
      }

      return query;
    }
  }
}
