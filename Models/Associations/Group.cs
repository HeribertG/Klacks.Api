using Klacks_api.Datas;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks_api.Models.Associations;

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
}
