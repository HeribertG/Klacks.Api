using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Settings
{
    public class CommunicationType
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }

        public int Category { get; set; }

        public int DefaultIndex { get; set; }
    }
}
