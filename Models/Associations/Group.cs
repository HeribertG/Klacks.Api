using Klacks.Api.Datas;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Associations;

public class Group : BaseEntity
{
    public Group()
    {
        GroupItems = new Collection<GroupItem>();
    }

    public string Description { get; set; } = string.Empty;

    public ICollection<GroupItem> GroupItems { get; set; }

    public string Name { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public Guid? Parent { get; set; }

    public Guid? Root { get; set; }

    public int Lft { get; set; } 
    public int Rgt { get; set; } 
}
