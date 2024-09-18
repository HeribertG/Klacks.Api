using Klacks_api.Datas;

namespace Klacks_api.Models.Staffs;

public class Annotation : BaseEntity
{
    public Guid ClientId { get; set; }
    public virtual Client Client { get; set; } = null!; 

    public string Note { get; set; } = string.Empty;



}
