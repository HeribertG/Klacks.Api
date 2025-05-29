using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Settings
{
    public class Vat
    {
        [Key]
        public Guid Id { get; set; }

        public decimal VATRate { get; set; }

        public bool IsDefault { get; set; }
    }
}
