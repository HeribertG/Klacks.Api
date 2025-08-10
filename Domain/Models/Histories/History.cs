using Klacks.Api.Datas;
using Klacks.Api.Domain.Models.Staffs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Histories;

public class History : BaseEntity
{
    [Required]
    [ForeignKey("Client")]
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    public int Type { get; set; }
    public string Data { get; set; } = String.Empty;
    public string OldData { get; set; } = String.Empty;
    public string NewData { get; set; } = String.Empty;
}
