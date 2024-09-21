using Klacks.Api.Datas;
using Klacks.Api.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Models.Associations;

public class GroupItem : BaseEntity
{

    public Guid ClientId { get; set; }

    public Guid GroupId { get; set; }

    public virtual Client Client { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}
