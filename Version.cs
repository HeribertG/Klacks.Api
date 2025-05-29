namespace Klacks.Api;

public class MyVersion : VersionConstant
{
    public static string Variant = BuildVariantConstant.CVar;
    public static int Year => CYear;
    public static int Week => CWeek;
    public static int Build => CBuild;

    public static string BuildKey => CBuildKey;
    public static string BuildTimestamp => CBuildTimestamp;


    /// <summary>Converts to string.</summary>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public string Get()
    {
        return Get(false);
    }


    /// <summary>Converts to string.</summary>
    /// <param name="includeBuildInformations">if set to <c>true</c> [include build informations].</param>
    /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
    public string Get(bool includeBuildInformations = false)
    {
        string version = BuildVariantConstant.CVar + "-" + CYear + "." + CWeek + "." + Build;
        if (includeBuildInformations)
        {
            var d = DateTime.Parse(CBuildTimestamp);
            version += " (" + CBuildKey + " / " + d.ToString("G") + ")";
        }

        return version;
    }
}
