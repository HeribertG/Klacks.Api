using Klacks.Api.Datas;

namespace Klacks.Api.Models.Staffs;

public class Annotation : BaseEntity
{
    public Guid ClientId { get; set; }
    public virtual Client Client { get; set; } = null!; 

    public string Note { get; set; } = string.Empty;



}
