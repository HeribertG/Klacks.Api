using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Staffs;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedClientResource : BaseTruncatedResult
    {
        public ICollection<ClientResource>? Clients { get; set; }

        public string Editor { get; set; } = string.Empty;
        
        public DateTime LastChange { get; set; }
    }
}
