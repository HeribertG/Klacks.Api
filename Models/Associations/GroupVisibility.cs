using Klacks.Api.Datas;
using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Models.Associations;

public class GroupVisibility : BaseEntity
{
    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public Guid GroupId { get; set; }

    public virtual Group Group { get; set; } = null!;
}
