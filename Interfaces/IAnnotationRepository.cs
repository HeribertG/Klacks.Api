using Klacks_api.Models.Staffs;

namespace Klacks_api.Interfaces;

public interface IAnnotationRepository : IBaseRepository<Annotation>
{
  Task<List<Annotation>> SimpleList(Guid id);
}
