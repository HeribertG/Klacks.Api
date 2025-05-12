using Klacks.Api.Datas;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Models.Staffs;

public class AssignedGroup : BaseEntity
{

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public virtual Client Client { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}
