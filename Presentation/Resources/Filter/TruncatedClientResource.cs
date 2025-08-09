using Klacks.Api.Presentation.Resources.Filter;
using Klacks.Api.Presentation.Resources.Staffs;

namespace Klacks.Api.Presentation.Resources.Filter
{
    public class TruncatedClientResource : BaseTruncatedResult
    {
        public ICollection<ClientResource>? Clients { get; set; }

        public string Editor { get; set; } = string.Empty;
        
        public DateTime LastChange { get; set; }
    }
}
