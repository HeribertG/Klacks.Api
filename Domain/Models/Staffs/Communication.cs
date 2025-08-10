using Klacks.Api.Datas;
using Klacks.Api.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Klacks.Api.Domain.Models.Staffs;

public class Communication : BaseEntity
{

    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    [Required]
    public CommunicationTypeEnum Type { get; set; }


    [StringLength(100)]
    public string Value { get; set; } = String.Empty;

    public string Prefix { get; set; } = String.Empty;

    public string Description { get; set; } = String.Empty;


}
