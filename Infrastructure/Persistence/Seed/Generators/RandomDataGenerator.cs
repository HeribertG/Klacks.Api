// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Data.Seed;

namespace Klacks.Api.Data.Seed.Generators;

public static class RandomDataGenerator
{
    public static DateTime GenerateRandomBirthday()
    {
        var random = Random.Shared;
        var start = new DateTime(1950, 1, 1);
        var end = new DateTime(2000, 12, 31);
        var range = (end - start).Days;
        return start.AddDays(random.Next(range));
    }

    public static string GenerateRandomEmail()
    {
        var domains = new[] { "gmail.com", "outlook.com", "yahoo.com", "bluewin.ch", "sunrise.ch" };
        var name = GenerateRandomString(8).ToLower();
        var domain = domains[Random.Shared.Next(domains.Length)];
        return $"{name}@{domain}";
    }

    public static GenderEnum GenerateRandomGender()
    {
        return Random.Shared.Next(0, 2) == 0 ? GenderEnum.Female : GenderEnum.Male;
    }

    public static string GenerateRandomPhoneNumber()
    {
        var random = new Random();
        var prefixes = new[] { "076", "077", "078", "079" };
        var prefix = prefixes[random.Next(prefixes.Length)];
        var number = (random.Next(0, 10000000) + 10000000).ToString("D7");
        return $"{prefix}{number}";
    }

    public static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    public static string GetRandomFirstName(GenderEnum gender, int fallbackIndex)
    {
        Random rand = new Random();
        switch (gender)
        {
            case GenderEnum.Female:
                return SeedNamesAndDescriptions.FemaleFirstNames[rand.Next(SeedNamesAndDescriptions.FemaleFirstNames.Count)];

            case GenderEnum.Male:
                return SeedNamesAndDescriptions.MaleFirstNames[rand.Next(SeedNamesAndDescriptions.MaleFirstNames.Count)];

            default:
                return GenerateMockName(fallbackIndex + 10);
        }
    }

    public static string GenerateMockName(int index)
    {
        string[] prefixes = { "Dr.", "Prof.", "Ing.", "Mag.", "" };
        string[] firstNames = { "Max", "Anna", "Peter", "Maria", "Hans", "Lisa", "Karl", "Eva" };
        string[] lastNames = { "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker" };

        var prefix = prefixes[index % prefixes.Length];
        var firstName = firstNames[index % firstNames.Length];
        var lastName = lastNames[index % lastNames.Length];

        return string.IsNullOrEmpty(prefix)
            ? $"{firstName} {lastName}"
            : $"{prefix} {firstName} {lastName}";
    }

    public static string GenerateCompanyName(Random rand)
    {
        string[] companyPrefixes = { "Swiss", "Global", "Tech", "Digital", "Smart", "Advanced", "Modern" };
        string[] companySuffixes = { "Solutions AG", "Services GmbH", "Consulting AG", "Systems GmbH", "Technologies AG", "Enterprises AG" };

        var prefix = companyPrefixes[rand.Next(companyPrefixes.Length)];
        var suffix = companySuffixes[rand.Next(companySuffixes.Length)];

        return $"{prefix} {suffix}";
    }

    public static string GenerateRandomStreet(string city, Random rand)
    {
        var streetsByCity = new Dictionary<string, string[]>
        {
            ["Zürich"] = new[]
            {
                "Bahnhofstrasse", "Limmatquai", "Niederdorfstrasse", "Langstrasse", "Seefeldstrasse", "Rämistrasse", "Universitätstrasse", "Minervastrasse", "Florastrasse", "Hottingerstrasse",
                "Kreuzstrasse", "Forchstrasse", "Dufourstrasse", "Stockerstrasse", "Talstrasse", "Pelikanstrasse", "Uraniastrasse", "Sihlstrasse", "Badenerstrasse", "Hardstrasse",
                "Josefstrasse", "Militärstrasse", "Pfingstweidstrasse", "Turbinenstrasse", "Hohlstrasse", "Birmensdorferstrasse", "Seestrasse", "Waffenplatzstrasse", "Albisstrasse", "Mythenquai",
                "Bellerivestrasse", "Gloriastrasse", "Winterthurerstrasse", "Schaffhauserstrasse", "Wehntalerstrasse", "Affolternstrasse", "Regensbergstrasse", "Bucheggstrasse", "Kornhausstrasse", "Rötelstrasse",
                "Hofwiesenstrasse", "Thurgauerstrasse", "Hagenholzstrasse", "Leutschenbachstrasse", "Binzmühlestrasse", "Max-Bill-Platz", "Andreasstrasse", "Gubelstrasse", "Schimmelstrasse", "Zweierstrasse",
                "Kanonengasse", "Ankerstrasse", "Bäckerstrasse", "Brauerstrasse", "Dienerstrasse", "Engelstrasse", "Feldstrasse", "Geroldstrasse", "Heinrichstrasse", "Imfeldstrasse",
                "Kalkbreitestrasse", "Letzigraben", "Magnusstrasse", "Neugasse", "Ottostrasse", "Quellenstrasse", "Rotachstrasse", "Stauffacherstrasse", "Tellstrasse", "Uetlibergstrasse",
                "Volkshaus-Strasse", "Weststrasse", "Xeniastrasse", "Ypsilonstrasse", "Zelgstrasse", "Aemtlerstrasse", "Bederstrasse", "Clausiusstrasse", "Dolderstrasse", "Englischviertelstrasse",
                "Freiestrasse", "Gladbachstrasse", "Hadlaubstrasse", "Ilgenstrasse", "Jupiterstrasse", "Kantonsschulstrasse", "Leonhardstrasse", "Moussonstrasse", "Neptunstrasse", "Oberstrass",
                "Plattenstrasse", "Quisanaplatz", "Restelbergstrasse", "Sonneggstrasse", "Toblerstrasse", "Universitätstrasse", "Voltastrasse", "Weinbergstrasse", "Zeltweg", "Zollikerstrasse"
            },
            ["Bern"] = new[]
            {
                "Kramgasse", "Marktgasse", "Spitalgasse", "Bundesgasse", "Schauplatzgasse", "Amthausgasse", "Zeughausgasse", "Aarbergergasse", "Neuengasse", "Gerechtigkeitsgasse",
                "Junkerngasse", "Postgasse", "Rathausgasse", "Münstergasse", "Herrengasse", "Nägeligasse", "Brunngasse", "Kesslergasse", "Metzgergasse", "Münzgraben",
                "Monbijoustrasse", "Sulgeneckstrasse", "Effingerstrasse", "Laupenstrasse", "Schwarztorstrasse", "Hodlerstrasse", "Alpeneggstrasse", "Muristrasse", "Thunstrasse", "Kirchenfeldstrasse",
                "Helvetiastrasse", "Bernastrasse", "Dufourstrasse", "Weltpoststrasse", "Viktoriastrasse", "Marienstrasse", "Luisenstrasse", "Gotthelfstrasse", "Breitenrainstrasse", "Moserstrasse",
                "Spitalackerstrasse", "Nordring", "Wankdorffeldstrasse", "Stauffacherstrasse", "Wylerringstrasse", "Militärstrasse", "Kasparstrasse", "Winkelriedstrasse", "Tellstrasse", "Rodtmattstrasse",
                "Gesellschaftsstrasse", "Scheibenstrasse", "Steinerstrasse", "Lorrainestrasse", "Jurastrasse", "Länggassstrasse", "Bremgartenstrasse", "Mittelstrasse", "Falkenplatz", "Sidlerstrasse",
                "Hochschulstrasse", "Schanzenstrasse", "Schanzeneckstrasse", "Bollwerk", "Speichergasse", "Genfergasse", "Bundesrain", "Kochergasse", "Christoffelgasse", "Bubenbergplatz",
                "Hirschengraben", "Casinoplatz", "Theaterplatz", "Kornhausplatz", "Bärenplatz", "Waisenhausplatz", "Bundesplatz", "Bahnhofplatz", "Schützenmattstrasse", "Eigerstrasse",
                "Marzilistrasse", "Aargauerstalden", "Altenbergstrasse", "Schosshaldenstrasse", "Obstbergweg", "Bantigerstrasse", "Könizstrasse", "Seftigenstrasse", "Freiburgstrasse", "Murtenstrasse"
            },
            ["Basel"] = new[]
            {
                "Freie Strasse", "Gerbergasse", "Spalenberg", "Steinenberg", "Aeschenvorstadt", "Aeschengraben", "Centralbahnstrasse", "Elisabethenstrasse", "Henric Petri-Strasse", "Nauenstrasse",
                "St. Alban-Vorstadt", "St. Alban-Graben", "Rittergasse", "Rheinsprung", "Martinsgasse", "Schneidergasse", "Sattelgasse", "Petersgraben", "Spalenvorstadt", "Spalenring",
                "Missionsstrasse", "Burgfelderstrasse", "Feldbergstrasse", "Klybeckstrasse", "Rheingasse", "Clarastrasse", "Greifengasse", "Utengasse", "Rebgasse", "Hammerstrasse",
                "Hochstrasse", "Münchensteinerstrasse", "Güterstrasse", "Reinacherstrasse", "Bruderholzstrasse", "Dornacherstrasse", "Leimenstrasse", "Picassoplatz", "Heuwaage", "Steinentorstrasse",
                "Kohlenberg", "Barfüsserplatz", "Theaterstrasse", "Klosterberg", "Leonhardsgraben", "Spalengraben", "Schützenmattstrasse", "Bundesstrasse", "Solothurnerstrasse", "Bernoullistrasse",
                "Hebelstrasse", "Klingelbergstrasse", "Schanzenstrasse", "Maiengasse", "Nadelberg", "Augustinergasse", "Münsterberg", "Stapfelberg", "Schlüsselberg", "Totengässlein",
                "Blumenrain", "Fischmarkt", "Schifflände", "Marktplatz", "Eisengasse", "Luftgässlein", "Andreasplatz", "Leonhardskirchplatz", "Petersplatz", "Spalentor",
                "Birmannsgasse", "Riehenstrasse", "Grenzacherstrasse", "Schwarzwaldallee", "Wettsteinplatz", "Mittlere Brücke", "Johanniterbrücke", "Voltastrasse", "Elsässerstrasse", "Lothringerstrasse",
                "St. Johanns-Ring", "St. Johanns-Vorstadt", "Spiegelgasse", "Streitgasse", "Weisse Gasse", "Im Lohnhof", "Petersgasse", "Rosshofgasse", "Pfluggässlein", "Imbergässlein"
            },
            ["Genève"] = new[]
            {
                "Rue du Rhône", "Rue du Marché", "Rue de la Corraterie", "Rue de Rive", "Rue de la Croix-d'Or", "Rue du Mont-Blanc", "Rue des Alpes", "Rue de Lausanne", "Rue de Berne", "Rue de Zurich",
                "Quai du Mont-Blanc", "Quai des Bergues", "Quai Gustave-Ador", "Rue de la Confédération", "Boulevard des Philosophes", "Boulevard Carl-Vogt", "Avenue de la Paix", "Route de Ferney", "Rue de Carouge", "Rue des Eaux-Vives",
                "Avenue Pictet-de-Rochemont", "Rue de Chantepoulet", "Rue du Stand", "Rue Verdaine", "Grand-Rue", "Rue Calvin", "Rue de l'Hôtel-de-Ville", "Rue du Puits-Saint-Pierre", "Rue de la Fontaine", "Rue des Granges",
                "Rue Etienne-Dumont", "Rue de la Cité", "Rue de la Madeleine", "Rue du Perron", "Rue de la Terrassière", "Avenue de Frontenex", "Route de Chêne", "Avenue de Champel", "Route de Florissant", "Avenue Krieg",
                "Rue de la Servette", "Rue de Lyon", "Avenue Wendt", "Rue des Délices", "Rue de Vermont", "Avenue de France", "Rue de Montbrillant", "Rue Rothschild", "Rue de Monthoux", "Rue Sismondi",
                "Rue Voltaire", "Rue Rousseau", "Rue du Temple", "Rue Pradier", "Rue Leschot", "Rue de Candolle", "Boulevard des Tranchées", "Boulevard Helvétique", "Avenue du Mail", "Rue du Conseil-Général",
                "Rue Prévost-Martin", "Rue de Saint-Jean", "Rue de la Coulouvrenière", "Pont de la Machine", "Place de la Fusterie", "Place du Molard", "Place Bel-Air", "Place du Bourg-de-Four", "Place Neuve", "Place des Nations",
                "Rue Ancienne", "Rue de l'Athénée", "Rue Bartholoni", "Rue Charles-Bonnet", "Rue du Cendrier", "Rue Dancet", "Rue Emile-Yung", "Rue Ferdinand-Hodler", "Rue Grenus", "Rue Henri-Fazy"
            },
            ["Luzern"] = new[]
            {
                "Kapellgasse", "Hertensteinstrasse", "Weggisgasse", "Kramgasse", "Weinmarkt", "Kornmarkt", "Mühlenplatz", "Rathausquai", "Bahnhofstrasse", "Pilatusstrasse",
                "Hirschengraben", "Sempacherstrasse", "Bundesstrasse", "Obergrundstrasse", "Tribschenstrasse", "Haldenstrasse", "Alpenstrasse", "Seeburgstrasse", "Nationalquai", "Schweizerhofquai",
                "Löwenstrasse", "Zentralstrasse", "Frankenstrasse", "Morgartenstrasse", "Winkelriedstrasse", "Seidenhofstrasse", "Theaterstrasse", "Baselstrasse", "Zürichstrasse", "Brünigstrasse",
                "Fluhmattstrasse", "Moosstrasse", "Neuweg", "Kellerstrasse", "Furrengasse", "Sternenplatz", "Pfistergasse", "Grabenstrasse", "Burgerstrasse", "St. Leodegarstrasse",
                "Dreilindenstrasse", "Adligenswilerstrasse", "Unterlöchlistrasse", "Oberlöchlistrasse", "Schädrütistrasse", "Berglistrasse", "Spannortstrasse", "Titlisstrasse", "Reussportstrasse", "Bahnhofplatz",
                "Hirschmattstrasse", "Waldstätterstrasse", "Felsbergstrasse", "Eisenbahnstrasse", "Industriestrasse", "Bürgenstrasse", "Arsenalstrasse", "Steinhofstrasse", "Allmendstrasse", "Langensandstrasse",
                "Voltastrasse", "Maihofstrasse", "Zihlmattweg", "Staffelnhofstrasse", "Wesemlinstrasse", "Habsburgerstrasse", "Ruopigenstrasse", "Kreuzbuchstrasse", "Würzenbachstrasse", "Sonnenbergstrasse",
                "Gütschstrasse", "Museggstrasse", "Brambergstrasse", "Falkengasse", "Reusssteg", "Spreuerbrücke", "Geissmattbrücke", "Seebrücke", "Rathaussteg", "Jesuitenplatz"
            },
            ["Lausanne"] = new[]
            {
                "Rue de Bourg", "Rue Saint-François", "Rue du Grand-Pont", "Rue Centrale", "Avenue de la Gare", "Avenue Benjamin-Constant", "Avenue de Rumine", "Avenue du Théâtre", "Rue de la Louve", "Rue Pépinet",
                "Rue du Petit-Chêne", "Rue de l'Ale", "Rue Marterey", "Rue du Midi", "Avenue de Cour", "Avenue d'Ouchy", "Chemin de Mornex", "Rue du Maupas", "Avenue de Montchoisi", "Rue de Genève",
                "Avenue de Tivoli", "Rue de la Barre", "Avenue de Beaulieu", "Avenue des Figuiers", "Rue du Simplon", "Avenue de Provence", "Chemin de Montelly", "Avenue de Morges", "Place de la Palud", "Place Saint-François",
                "Rue Pichard", "Rue du Grand-Chêne", "Rue Caroline", "Rue du Valentin", "Avenue de Bethusy", "Chemin de Primerose", "Avenue de la Harpe", "Rue du Bugnon", "Avenue Pierre-Decker", "Rue du Dr-César-Roux",
                "Avenue de Chailly", "Chemin de Rovéréaz", "Avenue de Beaumont", "Chemin de Boissonnet", "Avenue du Servan", "Rue de la Pontaise", "Avenue de Vinet", "Rue Pré-du-Marché", "Rue Haldimand", "Rue Enning",
                "Escaliers du Marché", "Rue Mercerie", "Rue Madeleine", "Rue Saint-Laurent", "Rue du Pont", "Rue des Terreaux", "Avenue de Rhodanie", "Avenue de l'Elysée", "Avenue du Léman", "Quai de Belgique",
                "Place de la Navigation", "Avenue de Montbenon", "Rue du Tunnel", "Avenue de Savoie", "Rue de Sébeillon", "Avenue de Sévelin", "Rue de la Vigie", "Chemin de Renens", "Avenue du Chablais", "Rue de Lausanne",
                "Boulevard de Grancy", "Avenue du Rond-Point", "Avenue Dapples", "Rue du Lac", "Avenue Juste-Olivier", "Rue Voltaire", "Avenue Ruchonnet", "Rue du Flon", "Voie du Chariot", "Rue de la Tour"
            }
        };

        var genericStreets = new[]
        {
            "Hauptstrasse", "Bahnhofstrasse", "Dorfstrasse", "Kirchstrasse", "Schulstrasse", "Gartenstrasse", "Industriestrasse", "Gewerbestrasse", "Rosenweg", "Sonnenweg",
            "Lindenstrasse", "Buchenweg", "Tannenweg", "Birkenweg", "Ahornweg", "Feldweg", "Wiesenstrasse", "Mühlestrasse", "Poststrasse", "Marktstrasse",
            "Ringstrasse", "Seestrasse", "Bergstrasse", "Waldstrasse", "Parkstrasse", "Schlossstrasse", "Kirchweg", "Schulweg", "Bachstrasse", "Brunnenstrasse",
            "Ausserdorfstrasse", "Innerdorfstrasse", "Oberdorfstrasse", "Unterdorfstrasse", "Neudorfstrasse", "Altdorfstrasse", "Nordstrasse", "Südstrasse", "Oststrasse", "Weststrasse",
            "Sonnhaldestrasse", "Rebhaldenstrasse", "Weinbergstrasse", "Hofstrasse", "Zelgstrasse", "Mattenstrasse", "Riedstrasse", "Moosstrasse", "Steinweg", "Sandweg",
            "Blumenstrasse", "Tulpenweg", "Nelkenstrasse", "Veilchenweg", "Dahlienstrasse", "Asternweg", "Primelweg", "Fliederstrasse", "Jasminweg", "Lavendelstrasse",
            "Eichenstrasse", "Ulmenweg", "Eschenstrasse", "Kastanienweg", "Pappelstrasse", "Weidenweg", "Erlenstrasse", "Fichtenweg", "Föhrenstrasse", "Lärchenweg",
            "Amselweg", "Drosselstrasse", "Finkenweg", "Meisenstrasse", "Spatzenweg", "Lerchenstrasse", "Schwalbenweg", "Storchenstrasse", "Reiherweg", "Falkenstrasse",
            "Adlerstrasse", "Habichtweg", "Bussardstrasse", "Eulenweg", "Kuckuckstrasse", "Spechtweg", "Elsterstrasse", "Rabenweg", "Kranichstrasse", "Taubenweg"
        };

        string[] streets;
        if (streetsByCity.TryGetValue(city, out var cityStreets))
        {
            streets = cityStreets;
        }
        else
        {
            streets = genericStreets;
        }

        var street = streets[rand.Next(streets.Length)];
        var houseNumber = rand.Next(1, 150);
        var escapedStreet = street.Replace("'", "''");

        return $"{escapedStreet} {houseNumber}";
    }
}
