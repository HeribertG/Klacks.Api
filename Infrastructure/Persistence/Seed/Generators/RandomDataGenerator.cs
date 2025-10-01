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
}
