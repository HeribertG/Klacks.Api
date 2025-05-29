using Klacks.Api.Datas;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Models.Settings
{
    /// <summary>
    /// States means "Länder(de) or Cantons(CH) or States(USA) or Départements(F) or Regioni(I) or Bundesländer(Ö).
    /// </summary>
    public class State : BaseEntity
    {
        [StringLength(10)]
        public string Abbreviation { get; set; } = string.Empty;

        [StringLength(10)]
        public string CountryPrefix { get; set; } = string.Empty;

        public MultiLanguage Name { get; set; } = null!;
    }
}
