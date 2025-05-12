using Klacks.Api.Resources.Staffs;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Resources.Associations;

public class GroupCreateResource
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    public ICollection<Guid> ClientIds { get; set; } = new Collection<Guid>();
}





