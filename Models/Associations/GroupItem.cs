using Klacks_api.Datas;
using Klacks_api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks_api.Models.Associations;

public class GroupItem : BaseEntity
{

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public virtual Client Client { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}
