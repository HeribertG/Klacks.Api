namespace Klacks.Api.Resources.Filter
{
  public class AbsenceTokenFilter
  {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool Checked { get; set; }
  }
}
