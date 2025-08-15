using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class FakeDatas
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            if (!string.IsNullOrEmpty(FakeSettings.WithFake))
            {
                var number = int.Parse(FakeSettings.ClientsNumber);
                var results = GenerateClientsData(number, DateTime.Now.Year, true);

                var scriptForClients = GenerateInsertScriptForClients(results.Clients);
                var scriptForAddresses = GenerateInsertScriptForAddresses(results.Addresses);
                var scriptForMemberships = GenerateInsertScriptForMemberships(results.Memberships);
                var scriptForCommunications = GenerateInsertScriptForCommunications(results.Communications);
                var scriptForAnnotations = GenerateInsertScriptForAnnotations(results.Annotations);
                var scriptForBreaks = GenerateInsertScriptForBreaks(results.Breaks);
                var scriptForSettings = GenerateInsertScriptForSettings();
                var scriptForGroups = FakeDataGroups.GenerateInsertScriptForGroups();

                migrationBuilder.Sql(scriptForClients);
                migrationBuilder.Sql(scriptForAddresses);
                migrationBuilder.Sql(scriptForMemberships);
                migrationBuilder.Sql(scriptForCommunications);
                migrationBuilder.Sql(scriptForAnnotations);
                migrationBuilder.Sql(scriptForBreaks);
                migrationBuilder.Sql(scriptForSettings);
                migrationBuilder.Sql(scriptForGroups);
            }
        }

        private static List<string> CreateFemaleFirstNames()
        {
            return new List<string>
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
        "Faustine", "Thaïs", "Amélie", "Aliénor", "Marion", "Maya", "Louisa", "Inaya",
      };
        }

        private static List<string> CreateMaleFirstNames()
        {
            return new List<string>
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
        "Sofiane", "Kylian", "Evan", "Gilles", "Lucien", "Loris", "Eliott", "Karim",
      };
        }

        private static List<string> CreateNames()
        {
            return new List<string>
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
            "Oswald", "Moser", "Seifert", "Kruse", "Uhl", "Schweizer",
            "Reiter", "Lemke", "Böhm", "Frank", "Muth", "Böhringer",
            "Bolz", "Schenk", "Römer", "Friedrich", "Krieger", "Weber",
            "Förster", "Schroeder", "Weiss", "Berger", "Koch", "Schuster",
            "Ross", "Schwarz", "Ebert", "Hirsch", "Neubauer", "Günther",
            "Bernhardt", "Scholl", "Zwick", "Eberle", "Witt", "Lutz",
            "Heil", "Brinkmann", "Nagel", "Brunner", "Schick", "Schiefer",
            "Heise", "Esser", "Schad", "Enders", "Maas", "Matthes",
            "Kempf", "Heß", "Gebhardt", "Siefert", "Seitz", "Kretzschmar",
            "Hagen", "Mahler", "Fink", "Lindemann", "Holz", "Weis",
            "Martin", "Bernard", "Dubois", "Thomas", "Robert", "Richard",
            "Petit", "Durand", "Leroy", "Moreau", "Simon", "Laurent",
            "Lefebvre", "Michel", "Garcia", "David", "Bertrand", "Roux",
            "Vincent", "Fournier", "Morel", "Girard", "André", "Lefèvre",
            "Mercier", "Dupont", "Lambert", "Bonnet", "Boucher", "Lévêque",
            "Boyer", "Gallet", "Vidal", "Chevalier", "Perrin", "Rodriguez",
            "Da Silva", "Maillard", "Marchand", "Dumont", "Marie", "Barbier",
            "Fontaine", "Brun", "Roy", "Blanchard", "Barre", "Moulin",
            "Evans", "Millet", "Grondin", "Guillaume", "Masson", "Denis",
            "Benoit", "Le Gall", "Guyot", "Maillot", "Le Roux", "Caron",
            "Charles", "Renard", "Collet", "Gérard", "Noël", "Mathieu",
            "Bernier", "Pelletier", "Perrier", "Boulanger", "Blanc", "Berger",
            "Gros", "Lacroix", "Roche", "Rey", "Guerin", "Marchal",
            "Picard", "Renault", "Rodier", "Marty", "Laporte", "Barnier",
            "Lemoine", "Bertin", "Coulon", "Baudry", "Combes", "Pascal",
            "Remy", "Leclercq", "Collin", "Gaudin", "Baudin", "Rolland",
            "Rousset", "Dufour", "Blaise", "Hebert", "Paul", "Dupuy",
            "Rousseau", "Brisson", "Leclerc", "Perrot", "Delmas", "Huguet",
            "Gomes", "Dupré", "Huet", "Vasseur", "Pierre", "Brunet",
            "Morin", "Besset", "Gillet", "Pons", "Charpentier", "Delorme",
            "Cormier", "Bonnin", "Mallet", "Besson", "Savary", "Prevost",
            "Poulain", "Marin", "Normand", "Costa", "Fernandes", "Arnaud",
            "Vivier", "Prevot", "Hamel", "Regnier", "Laine", "Hervé",
            "Villeneuve", "Picot", "Lagarde", "Lopes", "Jacquet", "Aubert",
            "Guillot", "Aubry", "Clerc", "Lemaire", "Valentin", "Blin",
            "Carriere", "Bourgeois", "Bouvet", "Maurice", "Lebrun", "Adam",
            "Godard", "Thibault", "Germain", "Prat", "Ollivier", "Joly",
        };
        }

        private static ICollection<AddressDb> GenerateAddresses(Guid clientId, List<PostcodeCH> postcodes)
        {
            Random rand = new Random();
            DateTime start = DateTime.Now.AddYears(-1).AddDays(rand.Next(365));
            PostcodeCH randomPostcode = postcodes[rand.Next(postcodes.Count)];
            return new List<AddressDb>
        {
          new AddressDb
          {
            ValidFrom = start,
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Street = "SomeStreet",
            Street2 = FakeSettings.ClientsNumber,
            Zip = randomPostcode.Zip.ToString(),
            City = randomPostcode.City,
            State = randomPostcode.State,
            Country = "CH",
          },
        };
        }

        private static ICollection<Annotation> GenerateAnnotations(Guid clientId)
        {
            var annotations = new List<Annotation>();
            Random rand = new Random();
            var sum = rand.Next(10);

            for (int i = 0; i < sum; i++)
            {
                var item = new Annotation
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    Note = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.",
                };
                annotations.Add(item);
            }

            return annotations;
        }

        private static ICollection<BreakDb> GenerateBreaks(Guid clientId, int year)
        {
            var breaks = new List<BreakDb>();
            Random rand = new Random();
            var maxBreaks = int.TryParse(FakeSettings.MaxBreaksPerClient, out int max) ? max : 30;
            var sum = rand.Next(maxBreaks);
            var absencesId = new Guid[]
            {
        Guid.Parse("1070d7e6-f314-4d20-bc18-98c5357a4f89"),
        Guid.Parse("1d5a1964-7fad-4da9-945c-3ad00c0edaa8"),
        Guid.Parse("dca0367a-530e-4eae-be34-b19d2262587e"),
        Guid.Parse("15ee57e6-31d1-492e-bf83-d3c386ef7472"),
        Guid.Parse("53851d0a-ff7f-460a-82a0-481aa3547d7e"),
        Guid.Parse("a04f8e87-8966-47c0-b293-931ea4f949ae"),
        Guid.Parse("72c43150-bd93-4a41-be99-9d7a603ff596"),
            };

            var absenceMinLength = new int[]
            {
        1,
        1,
        1,
        5,
        5,
        1,
        1,
            };

            DateTime startOfYear = new DateTime(year, 1, 1);

            for (int i = 0; i < sum; i++)
            {
                var index = rand.Next(absencesId.Length);
                var absenceId = absencesId[index];
                var absenceMin = absenceMinLength[index];
                if (rand.Next(2) != 0)
                {
                    absenceMin *= 2;
                }

                DateTime start = startOfYear.AddDays(rand.Next(365 - absenceMin));
                DateTime end = start.AddDays(absenceMin);

                var item = new BreakDb()
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    AbsenceId = absenceId,
                    From = start,
                    Until = end,
                };

                breaks.Add(item);
            }

            return breaks;
        }

        private static (List<ClientDb> Clients, List<AddressDb> Addresses, List<MembershipDb> Memberships, List<Communication> Communications, List<Annotation> Annotations, List<BreakDb> Breaks) GenerateClientsData(int count, int year, bool withIncludes)
        {
            var clients = new List<ClientDb>();
            var addresses = new List<AddressDb>();
            var memberships = new List<MembershipDb>();
            var communications = new List<Communication>();
            var annotations = new List<Annotation>();
            var breaks = new List<BreakDb>();
            var names = CreateNames();
            var maleFirstNames = CreateMaleFirstNames();
            var femaleFirstNames = CreateFemaleFirstNames();
            var postcodeCH = GeneratePostCodeCH();

            for (int i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                var gender = GenerateRandomGender();
                var client = new ClientDb
                {
                    Id = id,
                    IdNumber = i + 1,
                    Birthdate = GenerateRandomBirthday(),
                    Gender = gender,
                    Name = GetRandomName(names),
                    FirstName = GetRandomFirstName(gender, maleFirstNames, femaleFirstNames, i),
                };

                if (withIncludes == true)
                {
                    var address = GenerateAddresses(id, postcodeCH);
                    addresses.AddRange(address);
                    var communication = GenerateCommunications(id);
                    communications.AddRange(communication);
                    var annotation = GenerateAnnotations(id);
                    annotations.AddRange(annotation);
                    var _break = GenerateBreaks(id, year);
                    breaks.AddRange(_break);
                }

                var membership = GenerateMembershipForClient(client, year);
                memberships.Add(membership);
                client.MembershipId = membership.Id;

                clients.Add(client);
            }

            return (clients, addresses, memberships, communications, annotations, breaks);
        }

        private static ICollection<Communication> GenerateCommunications(Guid clientId)
        {
            return new List<Communication>
        {
            new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Type = CommunicationTypeEnum.PrivateCellPhone,
                Value = GenerateRandomNumber(),
                Prefix = "+41",
                Description = "SomeDescription",
            },

            new Communication
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                Type = CommunicationTypeEnum.PrivateMail,
                Value = GenerateRandomEmail(),
                Description = "SomeDescription",
            },
        };
        }

        private static string GenerateInsertScriptForAddresses(List<AddressDb> addresses)
        {
            if (addresses == null || !addresses.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var address in addresses)
            {
                script.Append($"INSERT INTO public.address (id, client_id, valid_from, type, address_line1, address_line2, street, street2, street3, zip, city, state, country, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time) VALUES ('{address.Id}', '{address.ClientId}', '{address.ValidFromString}', {(int)address.Type}, '{address.AddressLine1}', '{address.AddressLine2}', '{address.Street}', '{address.Street2}', '{address.Street3}', '{address.Zip}', '{address.City}', '{address.State}', '{address.Country}', '{address.CreateTimeString}', 'Annonymus', NULL, NULL, NULL, FALSE, NULL);\n");
            }

            return script.ToString();
        }

        private static string GenerateInsertScriptForAnnotations(List<Annotation> annotations)
        {
            if (annotations == null || !annotations.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var annotation in annotations)
            {
                script.AppendLine($"INSERT INTO public.annotation (id, create_time, current_user_created, current_user_deleted,current_user_updated, deleted_time, is_deleted, update_time, client_id, note) VALUES ('{annotation.Id}', NULL, 'Annonymus', NULL, NULL, NULL, FALSE, NULL, '{annotation.ClientId}', '{annotation.Note}');\n");
            }

            return script.ToString();
        }

        private static string GenerateInsertScriptForBreaks(List<BreakDb> breaks)
        {
            if (breaks == null || !breaks.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var breakItem in breaks)
            {
                script.AppendLine($"INSERT INTO public.break (id, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, absence_id, client_id, \"from\", \"until\", information) VALUES ('{breakItem.Id}', '{breakItem.CreateTimeString}', 'Annonymus', NULL, NULL, NULL, FALSE, NULL, '{breakItem.AbsenceId}', '{breakItem.ClientId}', '{breakItem.FromString}', '{breakItem.UntilString}', '{breakItem.Information}');\n");
            }

            return script.ToString();
        }

        private static string GenerateInsertScriptForClients(List<ClientDb> clients)
        {
            if (clients == null || !clients.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var client in clients)
            {
                script.Append($"INSERT INTO public.client (id, birthdate, company, first_name, gender, id_number, legal_entity, maiden_name, membership_id, name, passwort_reset_token, second_name, title,type,create_time,current_user_created,current_user_deleted, current_user_updated,deleted_time,is_deleted,update_time) VALUES ('{client.Id}','{client.BirthdateString}', '{client.Company}', '{client.FirstName}', {(int)client.Gender}, {client.IdNumber}, '{client.LegalEntity}', '{client.MaidenName}', '{client.MembershipId}', '{client.Name}', '{client.PasswortResetToken}', '{client.SecondName}', '{client.Title}', {client.IdNumber}, '{client.CreateTimeString}', '{client.CurrentUserCreated}', NULL, NULL, NULL, FALSE, NULL);\n");
            }

            return script.ToString();
        }

        private static string GenerateInsertScriptForCommunications(List<Communication> communications)
        {
            if (communications == null || !communications.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var communication in communications)
            {
                script.Append($"INSERT INTO public.communication (id, client_id, type, value, prefix, description, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time) VALUES ('{communication.Id}', '{communication.ClientId}', {(int)communication.Type}, '{communication.Value}', '{communication.Prefix}', '{communication.Description}', NULL, 'Annonymus', NULL, NULL, NULL, FALSE, NULL);\n");
            }

            return script.ToString();
        }

        private static string GenerateInsertScriptForSettings()
        {
            StringBuilder script = new StringBuilder();
            
            var settings = new[]
            {
                // Email Settings
                ("0f807cbb-e54a-4f5b-9383-5b03ccffc55d", "authenticationType", "<None>"),
                ("3be9b255-4b0b-49fd-8585-556375187dac", "outgoingserver", "smtp-mail.outlook.com"),
                ("789530bc-18a3-48b1-946f-a5da6d66d357", "enabledSSL", "true"),
                ("8d8b2ae3-7d7b-4f31-9778-0e348deb1fca", "dispositionNotification", "false"),
                ("91f43fe3-0db7-4554-aa4d-8dac0151f118", "replyTo", "doNotReply@klacks-net.com"),
                ("db3ee771-cbd6-420c-bdf7-8b1036bb82b9", "outgoingserverPort", "587"),
                ("e16842eb-24ff-47c2-ad1b-5a3d6a2d20cd", "outgoingserverTimeout", "100"),
                ("e3e61605-c1e9-48b9-b5c7-9e66c41889fe", "readReceipt", "false"),
                
                // Application Settings
                ("d5bbf185-b799-4aa4-86ca-c3fe879654f2", "appName", "Klacks-Net"),
                ("1234567a-1234-1234-1234-123456789001", "defaultLanguage", "de"),
                ("1234567a-1234-1234-1234-123456789002", "timezone", "Europe/Zurich"),
                ("1234567a-1234-1234-1234-123456789003", "dateFormat", "dd.MM.yyyy"),
                ("1234567a-1234-1234-1234-123456789004", "timeFormat", "HH:mm"),
                ("1234567a-1234-1234-1234-123456789005", "currency", "CHF"),
                
                // UI Settings
                ("1234567a-1234-1234-1234-123456789006", "theme", "light"),
                ("1234567a-1234-1234-1234-123456789007", "itemsPerPage", "25"),
                ("1234567a-1234-1234-1234-123456789008", "autoRefresh", "true"),
                ("1234567a-1234-1234-1234-123456789009", "showWelcomeMessage", "true"),
                
                // Security Settings
                ("1234567a-1234-1234-1234-123456789010", "sessionTimeout", "120"),
                ("1234567a-1234-1234-1234-123456789011", "passwordMinLength", "8"),
                ("1234567a-1234-1234-1234-123456789012", "requireStrongPassword", "true"),
                ("1234567a-1234-1234-1234-123456789013", "lockoutDuration", "15"),
                ("1234567a-1234-1234-1234-123456789014", "maxLoginAttempts", "5"),
                
                // Feature Flags
                ("1234567a-1234-1234-1234-123456789015", "enableAdvancedSearch", "true"),
                ("1234567a-1234-1234-1234-123456789016", "enableExport", "true"),
                ("1234567a-1234-1234-1234-123456789017", "enableNotifications", "true"),
                ("1234567a-1234-1234-1234-123456789018", "enableDarkMode", "false"),
                ("1234567a-1234-1234-1234-123456789019", "enableBulkOperations", "true"),
                
                // Business Settings
                ("1234567a-1234-1234-1234-123456789020", "defaultWorkingHours", "8.5"),
                ("1234567a-1234-1234-1234-123456789021", "overtimeThreshold", "42"),
                ("1234567a-1234-1234-1234-123456789022", "vacationDaysPerYear", "25"),
                ("1234567a-1234-1234-1234-123456789023", "probationPeriod", "3"),
                ("1234567a-1234-1234-1234-123456789024", "noticePeriod", "30"),
                
                // System Settings
                ("1234567a-1234-1234-1234-123456789025", "backupFrequency", "daily"),
                ("1234567a-1234-1234-1234-123456789026", "logLevel", "Information"),
                ("1234567a-1234-1234-1234-123456789027", "enableCaching", "true"),
                ("1234567a-1234-1234-1234-123456789028", "cacheExpiration", "3600"),
                ("1234567a-1234-1234-1234-123456789029", "enableCompression", "true"),
                ("1234567a-1234-1234-1234-123456789030", "maxFileUploadSize", "10485760")
            };

            foreach (var (id, type, value) in settings)
            {
                script.AppendLine($"INSERT INTO public.settings (id, type, value) VALUES ('{id}', '{type}', '{value}');");
            }

            return script.ToString();
        }


        private static string GenerateInsertScriptForMemberships(List<MembershipDb> memberships)
        {
            if (memberships == null || !memberships.Any())
            {
                return string.Empty;
            }

            StringBuilder script = new StringBuilder();

            foreach (var membership in memberships)
            {
                script.AppendLine($"INSERT INTO public.membership " +
                               $"(id, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, client_id, valid_from, valid_until, type) " +
                               $"VALUES " +
                               $"('{membership.Id}', '{membership.CreateTimeString}', 'Annonymus', NULL, NULL, NULL, FALSE, NULL, '{membership.ClientId}', '{membership.ValidFromString}', {membership.ValidUntilString}, {membership.Type});");
            }

            return script.ToString();
        }

        private static MembershipDb GenerateMembershipForClient(ClientDb client, int year)
        {
            Random rand = new Random();

            DateTime startOfYear = new DateTime(year, 1, 1);
            DateTime start = startOfYear.AddYears(-2).AddDays(rand.Next(365));
            DateTime? end = (rand.Next(2) == 0) ? null : start.AddYears(3);

            var membership = new MembershipDb
            {
                Id = Guid.NewGuid(),
                Client = client,
                ClientId = client.Id,
                ValidFrom = start,
                ValidUntil = end,
                Type = rand.Next(5),
            };

            return membership;
        }

        private static string GenerateMockName(int index)
        {
            char[] name = new char[6];

            for (int i = 0; i < name.Length; i++)
            {
                int asciiValue = 65 + (index + i) % 26; // 65 ist der ASCII-Wert für "A"
                name[i] = (char)asciiValue;
            }

            return new string(name);
        }

        private static List<PostcodeCH> GeneratePostCodeCH()
        {
            var jsonString = @"

 [
	{
		""id"" : 1,
		""city"" : ""Jouxtens-Mézery"",
		""state"" : ""VD"",
		""zip"" : 1008
	},
	{
		""id"" : 2,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1000
	},
	{
		""id"" : 3,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1003
	},
	{
		""id"" : 4,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1004
	},
	{
		""id"" : 5,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1005
	},
	{
		""id"" : 6,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1006
	},
	{
		""id"" : 7,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1007
	},
	{
		""id"" : 8,
		""city"" : ""Prilly"",
		""state"" : ""VD"",
		""zip"" : 1008
	},
	{
		""id"" : 9,
		""city"" : ""Pully"",
		""state"" : ""VD"",
		""zip"" : 1009
	},
	{
		""id"" : 10,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1010
	},
	{
		""id"" : 11,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1011
	},
	{
		""id"" : 12,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1012
	},
	{
		""id"" : 13,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1015
	},
	{
		""id"" : 14,
		""city"" : ""Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1018
	},
	{
		""id"" : 15,
		""city"" : ""Renens VD"",
		""state"" : ""VD"",
		""zip"" : 1020
	},
	{
		""id"" : 16,
		""city"" : ""Chavannes-près-Renens"",
		""state"" : ""VD"",
		""zip"" : 1022
	},
	{
		""id"" : 17,
		""city"" : ""Crissier"",
		""state"" : ""VD"",
		""zip"" : 1023
	},
	{
		""id"" : 18,
		""city"" : ""Ecublens VD"",
		""state"" : ""VD"",
		""zip"" : 1024
	},
	{
		""id"" : 19,
		""city"" : ""St-Sulpice VD"",
		""state"" : ""VD"",
		""zip"" : 1025
	},
	{
		""id"" : 20,
		""city"" : ""Denges"",
		""state"" : ""VD"",
		""zip"" : 1026
	},
	{
		""id"" : 21,
		""city"" : ""Echandens"",
		""state"" : ""VD"",
		""zip"" : 1026
	},
	{
		""id"" : 22,
		""city"" : ""Lonay"",
		""state"" : ""VD"",
		""zip"" : 1027
	},
	{
		""id"" : 23,
		""city"" : ""Préverenges"",
		""state"" : ""VD"",
		""zip"" : 1028
	},
	{
		""id"" : 24,
		""city"" : ""Villars-Ste-Croix"",
		""state"" : ""VD"",
		""zip"" : 1029
	},
	{
		""id"" : 25,
		""city"" : ""Bussigny-près-Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1030
	},
	{
		""id"" : 26,
		""city"" : ""Mex VD"",
		""state"" : ""VD"",
		""zip"" : 1031
	},
	{
		""id"" : 27,
		""city"" : ""Romanel-sur-Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1032
	},
	{
		""id"" : 28,
		""city"" : ""Cheseaux-sur-Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1033
	},
	{
		""id"" : 29,
		""city"" : ""Boussens"",
		""state"" : ""VD"",
		""zip"" : 1034
	},
	{
		""id"" : 30,
		""city"" : ""Bournens"",
		""state"" : ""VD"",
		""zip"" : 1035
	},
	{
		""id"" : 31,
		""city"" : ""Sullens"",
		""state"" : ""VD"",
		""zip"" : 1036
	},
	{
		""id"" : 32,
		""city"" : ""Etagnières"",
		""state"" : ""VD"",
		""zip"" : 1037
	},
	{
		""id"" : 33,
		""city"" : ""Bercher"",
		""state"" : ""VD"",
		""zip"" : 1038
	},
	{
		""id"" : 34,
		""city"" : ""St-Barthélemy VD"",
		""state"" : ""VD"",
		""zip"" : 1040
	},
	{
		""id"" : 35,
		""city"" : ""Villars-le-Terroir"",
		""state"" : ""VD"",
		""zip"" : 1040
	},
	{
		""id"" : 36,
		""city"" : ""Echallens"",
		""state"" : ""VD"",
		""zip"" : 1040
	},
	{
		""id"" : 37,
		""city"" : ""Dommartin"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 38,
		""city"" : ""Poliez-Pittet"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 39,
		""city"" : ""Bottens"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 40,
		""city"" : ""Naz"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 41,
		""city"" : ""Montaubion-Chardonney"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 42,
		""city"" : ""Poliez-le-Grand"",
		""state"" : ""VD"",
		""zip"" : 1041
	},
	{
		""id"" : 43,
		""city"" : ""Bettens"",
		""state"" : ""VD"",
		""zip"" : 1042
	},
	{
		""id"" : 44,
		""city"" : ""Bioley-Orjulaz"",
		""state"" : ""VD"",
		""zip"" : 1042
	},
	{
		""id"" : 45,
		""city"" : ""Assens"",
		""state"" : ""VD"",
		""zip"" : 1042
	},
	{
		""id"" : 46,
		""city"" : ""Sugnens"",
		""state"" : ""VD"",
		""zip"" : 1043
	},
	{
		""id"" : 47,
		""city"" : ""Fey"",
		""state"" : ""VD"",
		""zip"" : 1044
	},
	{
		""id"" : 48,
		""city"" : ""Ogens"",
		""state"" : ""VD"",
		""zip"" : 1045
	},
	{
		""id"" : 49,
		""city"" : ""Rueyres"",
		""state"" : ""VD"",
		""zip"" : 1046
	},
	{
		""id"" : 50,
		""city"" : ""Oppens"",
		""state"" : ""VD"",
		""zip"" : 1047
	},
	{
		""id"" : 51,
		""city"" : ""Le Mont-sur-Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1052
	},
	{
		""id"" : 52,
		""city"" : ""Bretigny-sur-Morrens"",
		""state"" : ""VD"",
		""zip"" : 1053
	},
	{
		""id"" : 53,
		""city"" : ""Cugy VD"",
		""state"" : ""VD"",
		""zip"" : 1053
	},
	{
		""id"" : 54,
		""city"" : ""Morrens VD"",
		""state"" : ""VD"",
		""zip"" : 1054
	},
	{
		""id"" : 55,
		""city"" : ""Froideville"",
		""state"" : ""VD"",
		""zip"" : 1055
	},
	{
		""id"" : 56,
		""city"" : ""Villars-Tiercelin"",
		""state"" : ""VD"",
		""zip"" : 1058
	},
	{
		""id"" : 57,
		""city"" : ""Peney-le-Jorat"",
		""state"" : ""VD"",
		""zip"" : 1059
	},
	{
		""id"" : 58,
		""city"" : ""Villars-Mendraz"",
		""state"" : ""VD"",
		""zip"" : 1061
	},
	{
		""id"" : 59,
		""city"" : ""Sottens"",
		""state"" : ""VD"",
		""zip"" : 1062
	},
	{
		""id"" : 60,
		""city"" : ""Peyres-Possens"",
		""state"" : ""VD"",
		""zip"" : 1063
	},
	{
		""id"" : 61,
		""city"" : ""Boulens"",
		""state"" : ""VD"",
		""zip"" : 1063
	},
	{
		""id"" : 62,
		""city"" : ""Chapelle-sur-Moudon"",
		""state"" : ""VD"",
		""zip"" : 1063
	},
	{
		""id"" : 63,
		""city"" : ""Martherenges"",
		""state"" : ""VD"",
		""zip"" : 1063
	},
	{
		""id"" : 64,
		""city"" : ""Epalinges"",
		""state"" : ""VD"",
		""zip"" : 1066
	},
	{
		""id"" : 65,
		""city"" : ""Les Monts-de-Pully"",
		""state"" : ""VD"",
		""zip"" : 1068
	},
	{
		""id"" : 66,
		""city"" : ""Puidoux"",
		""state"" : ""VD"",
		""zip"" : 1070
	},
	{
		""id"" : 67,
		""city"" : ""Rivaz"",
		""state"" : ""VD"",
		""zip"" : 1071
	},
	{
		""id"" : 68,
		""city"" : ""St-Saphorin (Lavaux)"",
		""state"" : ""VD"",
		""zip"" : 1071
	},
	{
		""id"" : 69,
		""city"" : ""Chexbres"",
		""state"" : ""VD"",
		""zip"" : 1071
	},
	{
		""id"" : 70,
		""city"" : ""Forel (Lavaux)"",
		""state"" : ""VD"",
		""zip"" : 1072
	},
	{
		""id"" : 71,
		""city"" : ""Mollie-Margot"",
		""state"" : ""VD"",
		""zip"" : 1073
	},
	{
		""id"" : 72,
		""city"" : ""Savigny"",
		""state"" : ""VD"",
		""zip"" : 1073
	},
	{
		""id"" : 73,
		""city"" : ""Ferlens VD"",
		""state"" : ""VD"",
		""zip"" : 1076
	},
	{
		""id"" : 74,
		""city"" : ""Servion"",
		""state"" : ""VD"",
		""zip"" : 1077
	},
	{
		""id"" : 75,
		""city"" : ""Essertes"",
		""state"" : ""VD"",
		""zip"" : 1078
	},
	{
		""id"" : 76,
		""city"" : ""Les Cullayes"",
		""state"" : ""VD"",
		""zip"" : 1080
	},
	{
		""id"" : 77,
		""city"" : ""Montpreveyres"",
		""state"" : ""VD"",
		""zip"" : 1081
	},
	{
		""id"" : 78,
		""city"" : ""Corcelles-le-Jorat"",
		""state"" : ""VD"",
		""zip"" : 1082
	},
	{
		""id"" : 79,
		""city"" : ""Mézières VD"",
		""state"" : ""VD"",
		""zip"" : 1083
	},
	{
		""id"" : 80,
		""city"" : ""Carrouge VD"",
		""state"" : ""VD"",
		""zip"" : 1084
	},
	{
		""id"" : 81,
		""city"" : ""Vulliens"",
		""state"" : ""VD"",
		""zip"" : 1085
	},
	{
		""id"" : 82,
		""city"" : ""Ropraz"",
		""state"" : ""VD"",
		""zip"" : 1088
	},
	{
		""id"" : 83,
		""city"" : ""La Croix (Lutry)"",
		""state"" : ""VD"",
		""zip"" : 1090
	},
	{
		""id"" : 84,
		""city"" : ""Aran"",
		""state"" : ""VD"",
		""zip"" : 1091
	},
	{
		""id"" : 85,
		""city"" : ""Chenaux"",
		""state"" : ""VD"",
		""zip"" : 1091
	},
	{
		""id"" : 86,
		""city"" : ""Grandvaux"",
		""state"" : ""VD"",
		""zip"" : 1091
	},
	{
		""id"" : 87,
		""city"" : ""Belmont-sur-Lausanne"",
		""state"" : ""VD"",
		""zip"" : 1092
	},
	{
		""id"" : 88,
		""city"" : ""La Conversion"",
		""state"" : ""VD"",
		""zip"" : 1093
	},
	{
		""id"" : 89,
		""city"" : ""Paudex"",
		""state"" : ""VD"",
		""zip"" : 1094
	},
	{
		""id"" : 90,
		""city"" : ""Lutry"",
		""state"" : ""VD"",
		""zip"" : 1095
	},
	{
		""id"" : 91,
		""city"" : ""Villette (Lavaux)"",
		""state"" : ""VD"",
		""zip"" : 1096
	},
	{
		""id"" : 92,
		""city"" : ""Cully"",
		""state"" : ""VD"",
		""zip"" : 1096
	},
	{
		""id"" : 93,
		""city"" : ""Riex"",
		""state"" : ""VD"",
		""zip"" : 1097
	},
	{
		""id"" : 94,
		""city"" : ""Epesses"",
		""state"" : ""VD"",
		""zip"" : 1098
	},
	{
		""id"" : 95,
		""city"" : ""Morges"",
		""state"" : ""VD"",
		""zip"" : 1110
	},
	{
		""id"" : 96,
		""city"" : ""Echichens"",
		""state"" : ""VD"",
		""zip"" : 1112
	},
	{
		""id"" : 97,
		""city"" : ""St-Saphorin-sur-Morges"",
		""state"" : ""VD"",
		""zip"" : 1113
	},
	{
		""id"" : 98,
		""city"" : ""Colombier VD"",
		""state"" : ""VD"",
		""zip"" : 1114
	},
	{
		""id"" : 99,
		""city"" : ""Vullierens"",
		""state"" : ""VD"",
		""zip"" : 1115
	},
	{
		""id"" : 100,
		""city"" : ""Cottens VD"",
		""state"" : ""VD"",
		""zip"" : 1116
	},
	{
		""id"" : 101,
		""city"" : ""Grancy"",
		""state"" : ""VD"",
		""zip"" : 1117
	},
	{
		""id"" : 102,
		""city"" : ""Bremblens"",
		""state"" : ""VD"",
		""zip"" : 1121
	},
	{
		""id"" : 103,
		""city"" : ""Romanel-sur-Morges"",
		""state"" : ""VD"",
		""zip"" : 1122
	},
	{
		""id"" : 104,
		""city"" : ""Aclens"",
		""state"" : ""VD"",
		""zip"" : 1123
	},
	{
		""id"" : 105,
		""city"" : ""Gollion"",
		""state"" : ""VD"",
		""zip"" : 1124
	},
	{
		""id"" : 106,
		""city"" : ""Monnaz"",
		""state"" : ""VD"",
		""zip"" : 1125
	},
	{
		""id"" : 107,
		""city"" : ""Vaux-sur-Morges"",
		""state"" : ""VD"",
		""zip"" : 1126
	},
	{
		""id"" : 108,
		""city"" : ""Clarmont"",
		""state"" : ""VD"",
		""zip"" : 1127
	},
	{
		""id"" : 109,
		""city"" : ""Reverolle"",
		""state"" : ""VD"",
		""zip"" : 1128
	},
	{
		""id"" : 110,
		""city"" : ""Tolochenaz"",
		""state"" : ""VD"",
		""zip"" : 1131
	},
	{
		""id"" : 111,
		""city"" : ""Lully VD"",
		""state"" : ""VD"",
		""zip"" : 1132
	},
	{
		""id"" : 112,
		""city"" : ""Vufflens-le-Château"",
		""state"" : ""VD"",
		""zip"" : 1134
	},
	{
		""id"" : 113,
		""city"" : ""Chigny"",
		""state"" : ""VD"",
		""zip"" : 1134
	},
	{
		""id"" : 114,
		""city"" : ""Denens"",
		""state"" : ""VD"",
		""zip"" : 1135
	},
	{
		""id"" : 115,
		""city"" : ""Bussy-Chardonney"",
		""state"" : ""VD"",
		""zip"" : 1136
	},
	{
		""id"" : 116,
		""city"" : ""Sévery"",
		""state"" : ""VD"",
		""zip"" : 1141
	},
	{
		""id"" : 117,
		""city"" : ""Pampigny"",
		""state"" : ""VD"",
		""zip"" : 1142
	},
	{
		""id"" : 118,
		""city"" : ""Apples"",
		""state"" : ""VD"",
		""zip"" : 1143
	},
	{
		""id"" : 119,
		""city"" : ""Ballens"",
		""state"" : ""VD"",
		""zip"" : 1144
	},
	{
		""id"" : 120,
		""city"" : ""Bière"",
		""state"" : ""VD"",
		""zip"" : 1145
	},
	{
		""id"" : 121,
		""city"" : ""Mollens VD"",
		""state"" : ""VD"",
		""zip"" : 1146
	},
	{
		""id"" : 122,
		""city"" : ""Montricher"",
		""state"" : ""VD"",
		""zip"" : 1147
	},
	{
		""id"" : 123,
		""city"" : ""Cuarnens"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 124,
		""city"" : ""Moiry VD"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 125,
		""city"" : ""La Praz"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 126,
		""city"" : ""Mont-la-Ville"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 127,
		""city"" : ""Chavannes-le-Veyron"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 128,
		""city"" : ""Mauraz"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 129,
		""city"" : ""Villars-Bozon"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 130,
		""city"" : ""La Coudre"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 131,
		""city"" : ""L''Isle"",
		""state"" : ""VD"",
		""zip"" : 1148
	},
	{
		""id"" : 132,
		""city"" : ""Berolle"",
		""state"" : ""VD"",
		""zip"" : 1149
	},
	{
		""id"" : 133,
		""city"" : ""St-Prex"",
		""state"" : ""VD"",
		""zip"" : 1162
	},
	{
		""id"" : 134,
		""city"" : ""Etoy"",
		""state"" : ""VD"",
		""zip"" : 1163
	},
	{
		""id"" : 135,
		""city"" : ""Buchillon"",
		""state"" : ""VD"",
		""zip"" : 1164
	},
	{
		""id"" : 136,
		""city"" : ""Allaman"",
		""state"" : ""VD"",
		""zip"" : 1165
	},
	{
		""id"" : 137,
		""city"" : ""Perroy"",
		""state"" : ""VD"",
		""zip"" : 1166
	},
	{
		""id"" : 138,
		""city"" : ""Lussy-sur-Morges"",
		""state"" : ""VD"",
		""zip"" : 1167
	},
	{
		""id"" : 139,
		""city"" : ""Villars-sous-Yens"",
		""state"" : ""VD"",
		""zip"" : 1168
	},
	{
		""id"" : 140,
		""city"" : ""Yens"",
		""state"" : ""VD"",
		""zip"" : 1169
	},
	{
		""id"" : 141,
		""city"" : ""Aubonne"",
		""state"" : ""VD"",
		""zip"" : 1170
	},
	{
		""id"" : 142,
		""city"" : ""Bougy-Villars"",
		""state"" : ""VD"",
		""zip"" : 1172
	},
	{
		""id"" : 143,
		""city"" : ""Féchy"",
		""state"" : ""VD"",
		""zip"" : 1173
	},
	{
		""id"" : 144,
		""city"" : ""Montherod"",
		""state"" : ""VD"",
		""zip"" : 1174
	},
	{
		""id"" : 145,
		""city"" : ""Pizy"",
		""state"" : ""VD"",
		""zip"" : 1174
	},
	{
		""id"" : 146,
		""city"" : ""Lavigny"",
		""state"" : ""VD"",
		""zip"" : 1175
	},
	{
		""id"" : 147,
		""city"" : ""St-Livres"",
		""state"" : ""VD"",
		""zip"" : 1176
	},
	{
		""id"" : 148,
		""city"" : ""Tartegnin"",
		""state"" : ""VD"",
		""zip"" : 1180
	},
	{
		""id"" : 149,
		""city"" : ""Rolle"",
		""state"" : ""VD"",
		""zip"" : 1180
	},
	{
		""id"" : 150,
		""city"" : ""Gilly"",
		""state"" : ""VD"",
		""zip"" : 1182
	},
	{
		""id"" : 151,
		""city"" : ""Bursins"",
		""state"" : ""VD"",
		""zip"" : 1183
	},
	{
		""id"" : 152,
		""city"" : ""Vinzel"",
		""state"" : ""VD"",
		""zip"" : 1184
	},
	{
		""id"" : 153,
		""city"" : ""Luins"",
		""state"" : ""VD"",
		""zip"" : 1184
	},
	{
		""id"" : 154,
		""city"" : ""Mont-sur-Rolle"",
		""state"" : ""VD"",
		""zip"" : 1185
	},
	{
		""id"" : 155,
		""city"" : ""Essertines-sur-Rolle"",
		""state"" : ""VD"",
		""zip"" : 1186
	},
	{
		""id"" : 156,
		""city"" : ""St-Oyens"",
		""state"" : ""VD"",
		""zip"" : 1187
	},
	{
		""id"" : 157,
		""city"" : ""Gimel"",
		""state"" : ""VD"",
		""zip"" : 1188
	},
	{
		""id"" : 158,
		""city"" : ""St-George"",
		""state"" : ""VD"",
		""zip"" : 1188
	},
	{
		""id"" : 159,
		""city"" : ""Saubraz"",
		""state"" : ""VD"",
		""zip"" : 1189
	},
	{
		""id"" : 160,
		""city"" : ""Dully"",
		""state"" : ""VD"",
		""zip"" : 1195
	},
	{
		""id"" : 161,
		""city"" : ""Bursinel"",
		""state"" : ""VD"",
		""zip"" : 1195
	},
	{
		""id"" : 162,
		""city"" : ""Gland"",
		""state"" : ""VD"",
		""zip"" : 1196
	},
	{
		""id"" : 163,
		""city"" : ""Prangins"",
		""state"" : ""VD"",
		""zip"" : 1197
	},
	{
		""id"" : 164,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1200
	},
	{
		""id"" : 165,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1201
	},
	{
		""id"" : 166,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1202
	},
	{
		""id"" : 167,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1203
	},
	{
		""id"" : 168,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1204
	},
	{
		""id"" : 169,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1205
	},
	{
		""id"" : 170,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1206
	},
	{
		""id"" : 171,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1207
	},
	{
		""id"" : 172,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1208
	},
	{
		""id"" : 173,
		""city"" : ""Genève"",
		""state"" : ""GE"",
		""zip"" : 1209
	},
	{
		""id"" : 174,
		""city"" : ""Grand-Lancy"",
		""state"" : ""GE"",
		""zip"" : 1212
	},
	{
		""id"" : 175,
		""city"" : ""Petit-Lancy"",
		""state"" : ""GE"",
		""zip"" : 1213
	},
	{
		""id"" : 176,
		""city"" : ""Onex"",
		""state"" : ""GE"",
		""zip"" : 1213
	},
	{
		""id"" : 177,
		""city"" : ""Vernier"",
		""state"" : ""GE"",
		""zip"" : 1214
	},
	{
		""id"" : 178,
		""city"" : ""Genève 15 Aéroport"",
		""state"" : ""GE"",
		""zip"" : 1215
	},
	{
		""id"" : 179,
		""city"" : ""Cointrin"",
		""state"" : ""GE"",
		""zip"" : 1216
	},
	{
		""id"" : 180,
		""city"" : ""Meyrin"",
		""state"" : ""GE"",
		""zip"" : 1217
	},
	{
		""id"" : 181,
		""city"" : ""Le Grand-Saconnex"",
		""state"" : ""GE"",
		""zip"" : 1218
	},
	{
		""id"" : 182,
		""city"" : ""Le Lignon"",
		""state"" : ""GE"",
		""zip"" : 1219
	},
	{
		""id"" : 183,
		""city"" : ""Aïre"",
		""state"" : ""GE"",
		""zip"" : 1219
	},
	{
		""id"" : 184,
		""city"" : ""Châtelaine"",
		""state"" : ""GE"",
		""zip"" : 1219
	},
	{
		""id"" : 185,
		""city"" : ""Les Avanchets"",
		""state"" : ""GE"",
		""zip"" : 1220
	},
	{
		""id"" : 186,
		""city"" : ""Vésenaz"",
		""state"" : ""GE"",
		""zip"" : 1222
	},
	{
		""id"" : 187,
		""city"" : ""Cologny"",
		""state"" : ""GE"",
		""zip"" : 1223
	},
	{
		""id"" : 188,
		""city"" : ""Chêne-Bougeries"",
		""state"" : ""GE"",
		""zip"" : 1224
	},
	{
		""id"" : 189,
		""city"" : ""Chêne-Bourg"",
		""state"" : ""GE"",
		""zip"" : 1225
	},
	{
		""id"" : 190,
		""city"" : ""Thônex"",
		""state"" : ""GE"",
		""zip"" : 1226
	},
	{
		""id"" : 191,
		""city"" : ""Les Acacias"",
		""state"" : ""GE"",
		""zip"" : 1227
	},
	{
		""id"" : 192,
		""city"" : ""Carouge GE"",
		""state"" : ""GE"",
		""zip"" : 1227
	},
	{
		""id"" : 193,
		""city"" : ""Plan-les-Ouates"",
		""state"" : ""GE"",
		""zip"" : 1228
	},
	{
		""id"" : 194,
		""city"" : ""Conches"",
		""state"" : ""GE"",
		""zip"" : 1231
	},
	{
		""id"" : 195,
		""city"" : ""Confignon"",
		""state"" : ""GE"",
		""zip"" : 1232
	},
	{
		""id"" : 196,
		""city"" : ""Bernex"",
		""state"" : ""GE"",
		""zip"" : 1233
	},
	{
		""id"" : 197,
		""city"" : ""Vessy"",
		""state"" : ""GE"",
		""zip"" : 1234
	},
	{
		""id"" : 198,
		""city"" : ""Cartigny"",
		""state"" : ""GE"",
		""zip"" : 1236
	},
	{
		""id"" : 199,
		""city"" : ""Avully"",
		""state"" : ""GE"",
		""zip"" : 1237
	},
	{
		""id"" : 200,
		""city"" : ""Collex"",
		""state"" : ""GE"",
		""zip"" : 1239
	}
]
";
            return JsonConvert.DeserializeObject<List<PostcodeCH>>(jsonString)!;
        }

        private static DateTime GenerateRandomBirthday()
        {
            int minAge = 20;
            int maxAge = 65;
            var random = new Random();
            var currentDate = DateTime.Now;

            int randomAge = random.Next(minAge, maxAge + 1);
            int randomDay = random.Next(1, 365);

            DateTime birthDate = currentDate.AddYears(-randomAge);

            // Ermittelt das genaue Geburtsdatum, indem zufällig Tage vom Anfang des Jahres abgezogen werden
            return birthDate.AddDays(-randomDay);
        }

        private static string GenerateRandomEmail()
        {
            var localPart = GenerateRandomString(8);
            var domain = GenerateRandomString(5);
            return $"{localPart}@{domain}.com";
        }

        private static GenderEnum GenerateRandomGender()
        {
            var random = new Random();
            Array values = Enum.GetValues(typeof(GenderEnum));
            return (GenderEnum)values.GetValue(random.Next(values.Length));
        }

        private static string GenerateRandomNumber()
        {
            var random = new Random();
            var prefixes = new[] { "076", "077", "078", "079" };
            var prefix = prefixes[random.Next(prefixes.Length)];

            var number = (random.Next(0, 10000000) + 10000000).ToString("D7");

            return $"{prefix}{number}";
        }

        private static string GenerateRandomString(int length)
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string GetRandomFirstName(GenderEnum gender, List<string> maleList, List<string> femaleList, int i)
        {
            Random rand = new Random();
            switch (gender)
            {
                case GenderEnum.Female:
                    return femaleList[rand.Next(femaleList.Count)];

                case GenderEnum.Male:
                    return maleList[rand.Next(maleList.Count)];

                default:
                    return GenerateMockName(i + 10);
            }
        }

        private static string GetRandomName(List<string> names)
        {
            var random = new Random();
            int index = random.Next(names.Count);
            return names[index];
        }

        private class AddressDb : Address
        {
            public string CreateTimeString
            {
                get
                {
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public string ValidFromString
            {
                get
                {
                    return ValidFrom!.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
        }

        private class BreakDb : Break
        {
            public string? CreateTimeString
            {
                get
                {
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public string FromString
            {
                get
                {
                    return From.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public string UntilString
            {
                get
                {
                    return Until.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }
        }

        private class ClientDb : Client
        {
            public string BirthdateString
            {
                get
                {
                    if (Birthdate != null)
                    {
                        return ((DateTime)Birthdate).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    }
                    else
                    {
                        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    }
                }
            }

            public string? CreateTimeString
            {
                get
                {
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public new string CurrentUserCreated
            {
                get
                {
                    return "Annonymus";
                }
            }
        }

        private class MembershipDb : Membership
        {
            public string? CreateTimeString
            {
                get
                {
                    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public string ValidFromString
            {
                get
                {
                    return ((DateTime)ValidFrom).ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
            }

            public string? ValidUntilString
            {
                get
                {
                    if (ValidUntil != null)
                    {
                        return "'" + ((DateTime)ValidUntil).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                    }
                    else
                    {
                        return "NULL";
                    }
                }
            }
        }
    }
}
