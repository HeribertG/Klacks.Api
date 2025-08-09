using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAnnotationRepository : IBaseRepository<Annotation>
{
    Task<List<Annotation>> SimpleList(Guid id);
}
