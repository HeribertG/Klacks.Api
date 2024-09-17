using Npgsql;


namespace Klacks_api
{
  public static class Extensions
  {
    public static void ConfigurePostgres(IServiceCollection service)
    {
      NpgsqlConnection.GlobalTypeMapper.UseJsonNet(settings: new Newtonsoft.Json.JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });
    }
  }
}
