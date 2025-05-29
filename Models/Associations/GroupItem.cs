using Klacks.Api.Datas;
using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Models.Associations;

public class GroupItem : BaseEntity
{

    public Guid? ClientId { get; set; }

    public Guid GroupId { get; set; }

    public virtual Client? Client { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}
