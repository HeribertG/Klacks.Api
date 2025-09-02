namespace Klacks.Api.Data.Seed
{
    public static class SeedNamesAndDescriptions
    {
        #region Vornamen

        public static List<string> FemaleFirstNames { get; } = new()
        {
            "Sophia", "Emma", "Hannah", "Mia", "Emilia", "Lena", "Amelie", "Sophie",
            "Marie", "Lina", "Mila", "Lea", "Ella", "Luisa", "Anna", "Leonie",
            "Emily", "Johanna", "Clara", "Mara", "Lara", "Laura", "Charlotte", "Lotta",
            "Sarah", "Lia", "Lisa", "Nina", "Nele", "Jana", "Paula", "Isabell",
            "Helena", "Marlene", "Zoe", "Maja", "Elina", "Lilly", "Elena", "Anni",
            "Melina", "Theresa", "Victoria", "Ida", "Pia", "Jule", "Stella", "Selina",
            "Franziska", "Julia", "Frieda", "Eva", "Isabella", "Katharina", "Annika",
            "Isabelle", "Zoey", "Carla", "Lena", "Marie", "Mira", "Lucy", "Pauline",
            "Romy", "Jasmin", "Sara", "Mina", "Sofie", "Alina", "Carolin", "Amalia",
            "Ronja", "Josephine", "Elsa", "Anastasia", "Antonia", "Miriam", "Fabienne",
            "Leila", "Amira", "Fiona", "Christina", "Diana", "Alexandra", "Sina",
            "Rosalie", "Caroline", "Leticia", "Lotte", "Nora", "Elli", "Judith",
            "Valentina", "Elisa", "Juliana", "Annabell", "Magdalena",
            "Emma", "Léa", "Chloé", "Manon", "Camille", "Louise", "Lucie", "Lola",
            "Zoé", "Jade", "Sarah", "Lilou", "Clara", "Éva", "Inès", "Lina",
            "Maelle", "Léna", "Lisa", "Juliette", "Mila", "Ambre", "Alice", "Maëlys",
            "Louna", "Romane", "Charlotte", "Lou", "Élise", "Jeanne", "Elena", "Clémence",
            "Anaïs", "Mathilde", "Marie", "Lise", "Mia", "Noémie", "Julia", "Léonie",
            "Nina", "Rose", "Yasmine", "Lana", "Elsa", "Julie", "Célia", "Justine",
            "Alicia", "Margaux", "Victoria", "Lya", "Maëva", "Amandine", "Héloïse",
            "Naomi", "Pauline", "Eloïse", "Sofia", "Eléna", "Coline", "Lia", "Iris",
            "Gabrielle", "Laly", "Suzanne", "Estelle", "Lila", "Morgane", "Axelle",
            "Fleur", "Cléa", "Flavie", "Roxane", "Salomé", "Noa", "Anaé", "Mélina",
            "Raphaëlle", "Aurore", "Solène", "Ella", "Anaëlle", "Candice", "Andréa",
            "Olivia", "Laurine", "Océane", "Joy", "Diane", "Éléna", "Elisa", "Agathe",
            "Faustine", "Thaïs", "Amélie", "Aliénor", "Marion", "Maya", "Louisa", "Inaya"
        };

        public static List<string> MaleFirstNames { get; } = new()
        {
            "Max", "Paul", "Lukas", "Ben", "Jonas", "Leon", "Finn", "Noah", "Elias",
            "Tim", "Luca", "Felix", "Moritz", "Niklas", "David", "Jan", "Oscar",
            "Julian", "Tobias", "Philipp", "Jannik", "Tom", "Alexander", "Simon",
            "Jakob", "Matteo", "Anton", "Fabian", "Marvin", "Leonard", "Vincent",
            "Raphael", "Florian", "Johannes", "Emil", "Jonah", "Linus", "Nico",
            "Daniel", "Samuel", "Aaron", "Henrik", "Liam", "Robin", "Lennard",
            "Joshua", "Sebastian", "Michael", "Adrian", "Oskar", "Till", "Benjamin",
            "Marc", "Dominik", "Benedikt", "Maximilian", "Joel", "Nick", "Marius",
            "Martin", "Dennis", "Lars", "Markus", "Jonathan", "Christopher", "Kevin",
            "Mathis", "Floris", "Mats", "Erik", "Valentin", "Paulo", "Robert",
            "Oliver", "Jannis", "Henri", "Hendrik", "Marcel", "Christian", "Yannick",
            "Georg", "Louis", "Peter", "Janis", "Sven", "Karim", "Philip", "Andreas",
            "Justus", "Arne", "Milo", "Thilo", "Thomas", "Konstantin", "Theo",
            "Malte", "Kilian", "Gabriel", "Christoph", "Tyler",
            "Lucas", "Hugo", "Martin", "Jules", "Louis", "Enzo", "Thomas", "Maxime",
            "Clément", "Nathan", "Alexandre", "Pierre", "Romain", "Nicolas", "Antoine",
            "Paul", "Mathis", "Baptiste", "Samuel", "Gabriel", "Théo", "Valentin",
            "Julien", "Maxence", "Yanis", "Sébastien", "Quentin", "Benjamin", "Léo",
            "Victor", "Florian", "Guillaume", "Dylan", "Thibault", "Vincent",
            "Arthur", "Alexis", "Mehdi", "Adrien", "Loïc", "Morgan", "Rémy", "Tristan",
            "Jordan", "Simon", "Cédric", "Arnaud", "Kilian", "Jeremy", "Corentin",
            "David", "Kévin", "Jonathan", "Raphaël", "Aurélien", "Mathieu", "Dorian",
            "Flavien", "Axel", "Xavier", "Rémi", "Anthony", "Tom", "François",
            "Mathéo", "Matthieu", "Mohamed", "Yohan", "Damien", "Fabien", "Rayan",
            "Cyril", "Alan", "Aymeric", "Gaël", "William", "Étienne", "Luc",
            "Gabin", "Ilyes", "Stéphane", "Laurent", "Mickaël", "Ismaël", "Max",
            "Steven", "Grégory", "Oscar", "Christophe", "Amine", "Youssef", "Erwan",
            "Sofiane", "Kylian", "Evan", "Gilles", "Lucien", "Loris", "Eliott", "Karim"
        };

        #endregion

        #region Nachnamen

        public static List<string> LastNames { get; } = new()
        {
            "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer",
            "Wagner", "Schulz", "Becker", "Hoffmann", "Schäfer", "Koch",
            "Bauer", "Richter", "Klein", "Wolf", "Schröder", "Neumann",
            "Schwarz", "Zimmermann", "Braun", "Krüger", "Hofmann", "Hartmann",
            "Lange", "Schmitt", "Werner", "Schmitz", "Krause", "Meier",
            "Lehmann", "Schmid", "Schulze", "Maier", "Köhler", "Herrmann",
            "König", "Mayer", "Walter", "Peters", "Möller", "Huber",
            "Kaiser", "Fuchs", "Lang", "Vogel", "Stein", "Jäger",
            "Otto", "Sommer", "Groß", "Seidel", "Heinrich", "Brandt",
            "Haas", "Schreiber", "Graf", "Schulte", "Dietrich", "Ziegler",
            "Kuhn", "Kühn", "Pohl", "Engel", "Horn", "Busch",
            "Bergmann", "Thomas", "Voigt", "Sauer", "Arnold", "Pfeiffer",
            "Wolff", "Voß", "Franke", "Ilg", "Winkler", "Schröter",
            "Lorenz", "Baumann", "Heinz", "Albrecht", "Kuntz", "Schumacher",
            "Pfaff", "Weiß", "Frey", "Körner", "Hahn", "Eisenmann",
            "Zimmer", "Haase", "Lindner", "Ernst", "Mendel", "Maurer",
            "Behrens", "Schindler", "Kirchner", "Weiss", "Krieger", "Abel",
            "Götz", "Paul", "Roth", "Leonhardt", "Hermes", "Thiel",
            "Gehrig", "Hesse", "Ullmann", "Freud", "Merkel", "Eckert",
            "Zahn", "Gerhardt", "Ackermann", "Baier", "Adam", "Döring",
            "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard",
            "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent",
            "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux",
            "Vincent", "Fournier", "Morel", "Girard", "André", "Lefèvre",
            "Mercier", "Dupont", "Lambert", "Bonnet", "Boucher", "Lévêque",
            "Boyer", "Gallet", "Vidal", "Chevalier", "Perrin", "Rodriguez",
            "Da Silva", "Maillard", "Marchand", "Dumont", "Marie", "Barbier"
        };

        #endregion

        #region Straßennamen

        public static List<string> StreetNames { get; } = new()
        {
            "Bahnhofstrasse", "Hauptstrasse", "Kirchgasse", "Schulhausstrasse", "Dorfstrasse", "Poststrasse",
            "Bergstrasse", "Gartenstrasse", "Rosengarten", "Lindenweg", "Birkenweg", "Ahornstrasse",
            "Buchenstrasse", "Tannenstrasse", "Fichtenweg", "Eichenstrasse", "Kastanienallee", "Wiesenweg",
            "Feldstrasse", "Waldweg", "Hügelstrasse", "Talstrasse", "Seestrasse", "Bachstrasse",
            "Quellenstrasse", "Sonnenbergstrasse", "Rebbergstrasse", "Ringstrasse", "Parkstrasse", "Friedhofweg",
            "Mühlestrasse", "Industriestrasse", "Gewerbestrasse", "Schulstrasse", "Spitalstrasse", "Rathausplatz"
        };

        #endregion

        #region Firmennamen

        public static List<string> CompanyNames { get; } = new()
        {
            "Alpine", "Summit", "Crystal", "Phoenix", "Horizon", "Stellar", "Quantum", "Nexus", "Vertex", "Zenith",
            "Pioneer", "Atlas", "Vanguard", "Meridian", "Pinnacle", "Orion", "Eclipse", "Aurora", "Cascade", "Titan",
            "Synergy", "Apex", "Frontier", "Infinity", "Dynamic", "Global", "Prime", "Elite", "Optimal", "Strategic",
            "Innovation", "Solutions", "Systems", "Technologies", "Consulting", "Services", "Partners", "Ventures",
            "Holdings", "International", "Swiss", "Europe", "Digital", "Smart", "Future", "Advanced", "Professional",
            "Enterprise", "Corporate", "Business", "Management", "Development", "Resources", "Capital", "Investment"
        };

        public static List<string> CompanySuffixes { get; } = new()
        {
            "AG", "GmbH", "SA", "Sàrl", "Ltd", "Inc", "KG", "OHG", "KGaA", "SE", "Co", "Corp", "LLC", "PLC"
        };

        #endregion

        #region Email-Domains

        public static List<string> EmailDomains { get; } = new()
        {
            "gmail.com", "outlook.com", "yahoo.com", "bluewin.ch", "sunrise.ch", "swisscom.ch",
            "hotmail.com", "web.de", "gmx.ch", "hispeed.ch", "vtx.ch", "quickline.ch"
        };

        #endregion

        #region Break-Gründe

        public static List<string> BreakReasons { get; } = new()
        {
            "Ferien", "Krankheit", "Weiterbildung", "Persönlicher Urlaub", "Mutterschaftsurlaub",
            "Vaterschaftsurlaub", "Militärdienst", "Zivildienst", "Unfall", "Arzttermin",
            "Familienangelegenheit", "Umzug", "Hochzeit", "Trauerfall", "Sabbatical"
        };

        #endregion

        #region Kommunikations-Beschreibungen

        public static List<string> CommunicationDescriptions { get; } = new()
        {
            "Terminbestätigung", "Terminverschiebung", "Zahlungserinnerung", "Service-Update",
            "Informationsanfrage", "Beschwerdebearbeitung", "Dankeschön", "Newsletter",
            "Vertragsinformation", "Rechnungsstellung", "Mahnung", "Willkommensgruß",
            "Geburtstagswünsche", "Weihnachtsgruß", "Ostergruß", "Jahresabschluss"
        };

        #endregion

        #region Notiz-Templates

        public static List<string> AnnotationTemplates { get; } = new()
        {
            "Kunde bevorzugt Termine am Morgen",
            "Spezielle Diätanforderungen beachten",
            "Rollstuhlzugang erforderlich",
            "Bevorzugt Kommunikation per E-Mail",
            "Hat medizinische Einschränkungen",
            "VIP-Kunde - hohe Priorität",
            "Zahlungsverzögerungen in der Vergangenheit",
            "Ausgezeichnete Zusammenarbeit",
            "Sehr pünktlich und zuverlässig",
            "Benötigt Erinnerungen für Termine",
            "Sprechen nur Deutsch",
            "Sprechen nur Französisch",
            "Mehrsprachig (DE/FR/EN)",
            "Allergien: Nüsse, Laktose",
            "Bevorzugt nachmittags Kontakt"
        };

        #endregion
    }
}