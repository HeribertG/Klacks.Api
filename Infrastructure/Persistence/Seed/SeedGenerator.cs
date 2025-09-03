using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Newtonsoft.Json;
using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class SeedGenerator
    {
        public static readonly Guid[] AbsenceIds = new Guid[]
        {
            Guid.Parse("1070d7e6-f314-4d20-bc18-98c5357a4f89"), // Krankheit/Unfall
            Guid.Parse("1d5a1964-7fad-4da9-945c-3ad00c0edaa8"), // Krankheit/Unfall 50%
            Guid.Parse("dca0367a-530e-4eae-be34-b19d2262587e"), // Schulung
            Guid.Parse("15ee57e6-31d1-492e-bf83-d3c386ef7472"), // Ferien
            Guid.Parse("53851d0a-ff7f-460a-82a0-481aa3547d7e"), // 1/2 Ferien
            Guid.Parse("a04f8e87-8966-47c0-b293-931ea4f949ae"), // Militär
            Guid.Parse("72c43150-bd93-4a41-be99-9d7a603ff596")  // Gesperrter Tag
        };

        private static readonly string user = "Anonymus";

        #region Data Generation Methods

        public static (List<Client> Clients, List<Address> Addresses, List<Membership> Memberships, List<Communication> Communications, List<Annotation> Annotations, List<Break> Breaks) GenerateClientsData(int count, int year, bool withIncludes)
        {
            var clients = new List<Client>();
            var addresses = new List<Address>();
            var memberships = new List<Membership>();
            var communications = new List<Communication>();
            var annotations = new List<Annotation>();
            var breaks = new List<Break>();
            var rand = new Random();

            var postcodes = GenerateComprehensiveSwissPostcodes();

            for (int i = 0; i < count; i++)
            {
                var isLegalEntity = false;
                var isCompany = rand.Next(100) < 20; // 20% chance for company
                var clientType = EntityTypeEnum.Employee; // Default

                if (isCompany)
                {
                    isLegalEntity = rand.Next(100) < 20; // 20% chance for legal entity
                    clientType = EntityTypeEnum.Customer; // LegalEntity ist immer Customer
                }
                else
                {
                    clientType = rand.Next(100) < 20 ? EntityTypeEnum.ExternEmp : EntityTypeEnum.Employee;
                }

                var gender = isLegalEntity ? GenderEnum.LegalEntity : GenerateRandomGender();
                
                var lastName = SeedNamesAndDescriptions.LastNames[rand.Next(SeedNamesAndDescriptions.LastNames.Count)];

                var client = new Client
                {
                    Id = Guid.NewGuid(),
                    Name = lastName,
                    FirstName = GetRandomFirstName(gender, i),
                    Birthdate = GenerateRandomBirthday(),
                    Gender = isLegalEntity ? GenderEnum.LegalEntity : GenerateRandomGender(),
                    LegalEntity = isLegalEntity,
                    Type = clientType,
                    Company = isCompany ? GenerateCompanyName(rand) : string.Empty,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    CurrentUserCreated = user
                };

                clients.Add(client);

                if (withIncludes)
                {
                    var clientAddresses = GenerateAddresses(client.Id);
                    addresses.AddRange(clientAddresses);

                    var membership = GenerateMembershipForClient(client, year);
                    memberships.Add(membership);
                    client.MembershipId = membership.Id; // Set the MembershipId on the client

                    var clientCommunications = GenerateCommunications(client.Id);
                    communications.AddRange(clientCommunications);

                    var clientAnnotations = GenerateAnnotations(client.Id);
                    annotations.AddRange(clientAnnotations);

                    var clientBreaks = GenerateBreaks(client.Id, year, membership);
                    breaks.AddRange(clientBreaks);
                }
                else
                {
                    // Create a default membership even when withIncludes is false
                    var membership = GenerateMembershipForClient(client, year);
                    memberships.Add(membership);
                    client.MembershipId = membership.Id;
                }
            }

            return (clients, addresses, memberships, communications, annotations, breaks);
        }

        public static string GenerateCompanyName(Random rand)
        {
            var prefixes = new[] { "Swiss", "Alpine", "Helvetic", "Geneva", "Zurich", "Basel", "Bern" };
            var suffixes = SeedNamesAndDescriptions.CompanySuffixes;
            var industries = SeedNamesAndDescriptions.CompanyNames;

            var prefix = prefixes[rand.Next(prefixes.Length)];
            var industry = industries[rand.Next(industries.Count)];
            var suffix = suffixes[rand.Next(suffixes.Count)];

            return $"{prefix} {industry} {suffix}";
        }
               
        private static ICollection<Address> GenerateAddresses(Guid clientId)
        {
            Random rand = new Random();
            DateTime start = DateTime.Now.AddYears(-1).AddDays(rand.Next(365));

            var swissPostcodes = GenerateComprehensiveSwissPostcodes();
            PostcodeCH randomPostcode = swissPostcodes[rand.Next(swissPostcodes.Count)];

            return new List<Address>
            {
                new Address
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
                    Type = AddressTypeEnum.Employee,
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    CurrentUserCreated = user
                },
            };
        }

        public static ICollection<Annotation> GenerateAnnotations(Guid clientId)
        {
            var annotations = new List<Annotation>();
            var annotationCount = Random.Shared.Next(0, 4); // 0-3 annotations per client

            var annotationTemplates = new[]
            {
                "Client prefers morning appointments",
                "Special dietary requirements noted",
                "Requires wheelchair access",
                "Prefers communication via email",
                "Has medical restrictions",
                "VIP client - high priority",
                "Payment delays in the past",
                "Excellent cooperation"
            };

            for (int i = 0; i < annotationCount; i++)
            {
                annotations.Add(new Annotation
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    Note = annotationTemplates[Random.Shared.Next(annotationTemplates.Length)],
                    IsDeleted = false,
                    CreateTime = DateTime.Now,
                    CurrentUserCreated = user
                });
            }

            return annotations;
        }

        private static ICollection<Break> GenerateBreaks(Guid clientId, int year, Membership membership)
        {
            var breaks = new List<Break>();
            Random rand = new Random();
            var maxBreaks = int.TryParse(FakeSettings.MaxBreaksPerClientPerYear, out int max) ? max : 30;
            var sum = rand.Next(1, maxBreaks + 1); // Generate between 1 and maxBreaks inclusive

            // Determine the valid range for breaks based on membership
            DateTime membershipStart = membership.ValidFrom;
            DateTime membershipEnd = membership.ValidUntil ?? DateTime.Now.AddYears(1);

            // Generate breaks for previous year, current year, and next year
            // MaxBreaksPerClient applies per year, not total
            var yearsToGenerate = new[] { year - 1, year, year + 1 };

            foreach (var targetYear in yearsToGenerate)
            {
                DateTime yearStart = new DateTime(targetYear, 1, 1);
                DateTime yearEnd = new DateTime(targetYear, 12, 31);

                // Constrain to membership period
                DateTime breakPeriodStart = membershipStart > yearStart ? membershipStart : yearStart;
                DateTime breakPeriodEnd = membershipEnd < yearEnd ? membershipEnd : yearEnd;

                // Skip this year if no valid period exists
                if (breakPeriodStart > breakPeriodEnd)
                {
                    continue;
                }

                // Calculate days available for breaks in this year
                int totalDays = (int)(breakPeriodEnd - breakPeriodStart).TotalDays + 1;
                if (totalDays <= 0)
                {
                    continue;
                }

                // Generate breaks for this specific year
                for (int i = 0; i < sum; i++)
                {
                    var duration = Random.Shared.Next(1, 15); // 1-14 days for each break
                    var absenceId = AbsenceIds[Random.Shared.Next(AbsenceIds.Length)]; // Use random fixed Absence GUID

                    // Ensure we don't go beyond the valid period for this year
                    if (duration > totalDays)
                    {
                        continue;
                    }

                    // Random start date within the valid period for this year
                    int maxStartDay = Math.Max(0, totalDays - duration);
                    DateTime start = breakPeriodStart.AddDays(rand.Next(maxStartDay + 1));
                    DateTime end = start.AddDays(duration);

                    // Double-check the end date is within bounds for this year
                    if (end > breakPeriodEnd)
                    {
                        end = breakPeriodEnd;
                    }

                    var item = new Break()
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        AbsenceId = absenceId,
                        From = start,
                        Until = end,
                        Information = "",
                        IsDeleted = false,
                        CreateTime = DateTime.Now,
                        CurrentUserCreated = user
                    };

                    breaks.Add(item);
                }
            }

            return breaks;
        }

        public static ICollection<Communication> GenerateCommunications(Guid clientId)
        {
            var communications = new List<Communication>();
            var commCount = Random.Shared.Next(1, 4); // 1-3 communications per client

            var subjects = SeedNamesAndDescriptions.CommunicationDescriptions;

            for (int i = 0; i < commCount; i++)
            {
                communications.Add(
                    new Communication
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Type = (CommunicationTypeEnum)Random.Shared.Next(0, 3),
                        Value = GenerateRandomNumber(),
                        Prefix = "+41",
                        Description = "SomeDescription",
                        IsDeleted = false,
                        CreateTime = DateTime.Now,
                        CurrentUserCreated = user
                    }
                 );

                communications.Add(
                    new Communication
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        Type = (CommunicationTypeEnum)Random.Shared.Next(4, 5),
                        Value = GenerateRandomEmail(),
                        Description = "SomeDescription",
                        IsDeleted = false,
                        CreateTime = DateTime.Now,
                        CurrentUserCreated = user
                    }
                 );
            }

            return communications;
        }

        public static Membership GenerateMembershipForClient(Client client, int year)
        {
            Random rand = new Random();

            DateTime startOfYear = new DateTime(year, 1, 1);
            DateTime startDate = startOfYear.AddYears(-2).AddDays(rand.Next(365));
            DateTime? endDate = (rand.Next(2) == 0) ? null : startDate.AddYears(3);

            return new Membership
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                ValidFrom = startDate,
                ValidUntil = endDate,
                Type = rand.Next(5),
                IsDeleted = false,
                CreateTime = DateTime.Now,
                CurrentUserCreated = user
            };
        }

        public static string GenerateMockName(int index)
        {
            var firstNames = SeedNamesAndDescriptions.FemaleFirstNames;
            var lastNames = SeedNamesAndDescriptions.LastNames;

            var firstName = firstNames[index % firstNames.Count];
            var lastName = lastNames[index % lastNames.Count];

            return $"{firstName} {lastName}";
        }

        private static List<PostcodeCH> GenerateComprehensiveSwissPostcodes()
        {
            // Representative postcodes for all 26 Swiss cantons plus major cities
            var postcodes = new List<PostcodeCH>
            {
                // AG - Aargau
                new PostcodeCH { Zip = 5000, City = "Aarau", State = "AG" },
                new PostcodeCH { Zip = 5400, City = "Baden", State = "AG" },
                new PostcodeCH { Zip = 8957, City = "Spreitenbach", State = "AG" },
                
                // AI - Appenzell Innerrhoden
                new PostcodeCH { Zip = 9050, City = "Appenzell", State = "AI" },
                new PostcodeCH { Zip = 9063, City = "Stein AR", State = "AI" },
                
                // AR - Appenzell Ausserrhoden  
                new PostcodeCH { Zip = 9100, City = "Herisau", State = "AR" },
                new PostcodeCH { Zip = 9200, City = "Gossau SG", State = "AR" },
                
                // BE - Bern
                new PostcodeCH { Zip = 3000, City = "Bern", State = "BE" },
                new PostcodeCH { Zip = 3600, City = "Thun", State = "BE" },
                new PostcodeCH { Zip = 2500, City = "Biel/Bienne", State = "BE" },
                new PostcodeCH { Zip = 3048, City = "Worblaufen", State = "BE" },
                new PostcodeCH { Zip = 3027, City = "Köniz", State = "BE" },
                
                // BL - Basel Land
                new PostcodeCH { Zip = 4410, City = "Liestal", State = "BL" },
                new PostcodeCH { Zip = 4123, City = "Allschwil", State = "BL" },
                new PostcodeCH { Zip = 4153, City = "Reinach BL", State = "BL" },
                new PostcodeCH { Zip = 4132, City = "Muttenz", State = "BL" },
                
                // BS - Basel Stadt
                new PostcodeCH { Zip = 4000, City = "Basel", State = "BS" },
                new PostcodeCH { Zip = 4001, City = "Basel", State = "BS" },
                new PostcodeCH { Zip = 4056, City = "Basel", State = "BS" },
                
                // FR - Freiburg
                new PostcodeCH { Zip = 1700, City = "Fribourg", State = "FR" },
                new PostcodeCH { Zip = 1630, City = "Bulle", State = "FR" },
                new PostcodeCH { Zip = 1580, City = "Avenches", State = "FR" },
                
                // GE - Genf
                new PostcodeCH { Zip = 1200, City = "Genève", State = "GE" },
                new PostcodeCH { Zip = 1201, City = "Genève", State = "GE" },
                new PostcodeCH { Zip = 1202, City = "Genève", State = "GE" },
                new PostcodeCH { Zip = 1220, City = "Les Avanchets", State = "GE" },
                
                // GL - Glarus
                new PostcodeCH { Zip = 8750, City = "Glarus", State = "GL" },
                new PostcodeCH { Zip = 8783, City = "Linthal", State = "GL" },
                
                // GR - Graubünden
                new PostcodeCH { Zip = 7000, City = "Chur", State = "GR" },
                new PostcodeCH { Zip = 7500, City = "St. Moritz", State = "GR" },
                new PostcodeCH { Zip = 7260, City = "Davos Dorf", State = "GR" },
                
                // JU - Jura
                new PostcodeCH { Zip = 2800, City = "Delémont", State = "JU" },
                new PostcodeCH { Zip = 2900, City = "Porrentruy", State = "JU" },
                
                // LU - Luzern
                new PostcodeCH { Zip = 6000, City = "Luzern", State = "LU" },
                new PostcodeCH { Zip = 6020, City = "Emmenbrücke", State = "LU" },
                new PostcodeCH { Zip = 6030, City = "Ebikon", State = "LU" },
                new PostcodeCH { Zip = 6032, City = "Emmen", State = "LU" },
                new PostcodeCH { Zip = 6010, City = "Kriens", State = "LU" },
                
                // NE - Neuenburg
                new PostcodeCH { Zip = 2000, City = "Neuchâtel", State = "NE" },
                new PostcodeCH { Zip = 2300, City = "La Chaux-de-Fonds", State = "NE" },
                
                // NW - Nidwalden
                new PostcodeCH { Zip = 6370, City = "Stans", State = "NW" },
                new PostcodeCH { Zip = 6390, City = "Engelberg", State = "NW" },
                
                // OW - Obwalden
                new PostcodeCH { Zip = 6060, City = "Sarnen", State = "OW" },
                new PostcodeCH { Zip = 6078, City = "Lungern", State = "OW" },
                
                // SG - St. Gallen
                new PostcodeCH { Zip = 9000, City = "St. Gallen", State = "SG" },
                new PostcodeCH { Zip = 8640, City = "Rapperswil-Jona", State = "SG" },
                new PostcodeCH { Zip = 9200, City = "Gossau SG", State = "SG" },
                
                // SH - Schaffhausen
                new PostcodeCH { Zip = 8200, City = "Schaffhausen", State = "SH" },
                new PostcodeCH { Zip = 8260, City = "Stein am Rhein", State = "SH" },
                
                // SO - Solothurn
                new PostcodeCH { Zip = 4500, City = "Solothurn", State = "SO" },
                new PostcodeCH { Zip = 4600, City = "Olten", State = "SO" },
                
                // SZ - Schwyz
                new PostcodeCH { Zip = 6430, City = "Schwyz", State = "SZ" },
                new PostcodeCH { Zip = 6440, City = "Brunnen", State = "SZ" },
                
                // TG - Thurgau
                new PostcodeCH { Zip = 8500, City = "Frauenfeld", State = "TG" },
                new PostcodeCH { Zip = 8280, City = "Kreuzlingen", State = "TG" },
                
                // TI - Tessin
                new PostcodeCH { Zip = 6900, City = "Lugano", State = "TI" },
                new PostcodeCH { Zip = 6500, City = "Bellinzona", State = "TI" },
                new PostcodeCH { Zip = 6600, City = "Locarno", State = "TI" },
                
                // UR - Uri
                new PostcodeCH { Zip = 6460, City = "Altdorf", State = "UR" },
                new PostcodeCH { Zip = 6490, City = "Andermatt", State = "UR" },
                
                // VD - Waadt
                new PostcodeCH { Zip = 1000, City = "Lausanne", State = "VD" },
                new PostcodeCH { Zip = 1800, City = "Vevey", State = "VD" },
                new PostcodeCH { Zip = 1400, City = "Yverdon-les-Bains", State = "VD" },
                new PostcodeCH { Zip = 1110, City = "Morges", State = "VD" },
                
                // VS - Wallis
                new PostcodeCH { Zip = 1950, City = "Sion", State = "VS" },
                new PostcodeCH { Zip = 3900, City = "Brig", State = "VS" },
                new PostcodeCH { Zip = 1870, City = "Monthey", State = "VS" },
                
                // ZG - Zug
                new PostcodeCH { Zip = 6300, City = "Zug", State = "ZG" },
                new PostcodeCH { Zip = 6330, City = "Cham", State = "ZG" },
                
                // ZH - Zürich
                new PostcodeCH { Zip = 8000, City = "Zürich", State = "ZH" },
                new PostcodeCH { Zip = 8400, City = "Winterthur", State = "ZH" },
                new PostcodeCH { Zip = 8610, City = "Uster", State = "ZH" },
                new PostcodeCH { Zip = 8001, City = "Zürich", State = "ZH" },
                new PostcodeCH { Zip = 8050, City = "Zürich", State = "ZH" }
            };

            return postcodes;
        }

        #endregion

        #region Random Generation Helper Methods

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

        private static string GenerateRandomNumber()
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
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // chars string bleibt .Length
        }

        #endregion

        #region SQL Script Generation Methods

        public static string GenerateInsertScriptForAddresses(List<Address> addresses)
        {
            StringBuilder script = new StringBuilder();
            foreach (var address in addresses)
            {
                script.AppendLine($@"INSERT INTO public.address (id, client_id, address_line1, address_line2, street, street2, street3, zip, city, state, country, valid_from, type, is_deleted, create_time, current_user_created) 
                    VALUES ('{address.Id}', '{address.ClientId}', '{address.AddressLine1}', '{address.AddressLine2}', '{address.Street}', '{address.Street2}', '{address.Street3}', '{address.Zip}', '{address.City}', '{address.State}', '{address.Country}', '{address.ValidFrom:yyyy-MM-dd}', {(int)address.Type}, {address.IsDeleted.ToString().ToLower()}, '{address.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{address.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForAnnotations(List<Annotation> annotations)
        {
            StringBuilder script = new StringBuilder();
            foreach (var annotation in annotations)
            {
                script.AppendLine($@"INSERT INTO public.annotation (id, client_id, note, is_deleted, create_time, current_user_created) 
                    VALUES ('{annotation.Id}', '{annotation.ClientId}', '{annotation.Note}', {annotation.IsDeleted.ToString().ToLower()}, '{annotation.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{annotation.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForBreaks(List<Break> breaks)
        {
            StringBuilder script = new StringBuilder();
            foreach (var breakItem in breaks)
            {
                script.AppendLine($@"INSERT INTO public.break (id, client_id, absence_id, ""from"", ""until"", information, break_reason_id, is_deleted, create_time, current_user_created) 
                    VALUES ('{breakItem.Id}', '{breakItem.ClientId}', '{breakItem.AbsenceId}', '{breakItem.From:yyyy-MM-dd HH:mm:ss.ffffff}', '{breakItem.Until:yyyy-MM-dd HH:mm:ss.ffffff}', '{breakItem.Information}', {(breakItem.BreakReasonId.HasValue ? $"'{breakItem.BreakReasonId}'" : "NULL")}, {breakItem.IsDeleted.ToString().ToLower()}, '{breakItem.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{breakItem.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForClients(List<Client> clients)
        {
            StringBuilder script = new StringBuilder();
            foreach (var client in clients)
            {
                script.AppendLine($@"INSERT INTO public.client (id, name, first_name, second_name, birthdate, gender, legal_entity, company, membership_id, type, is_deleted, create_time, current_user_created) 
                    VALUES ('{client.Id}', '{client.Name}', '{client.FirstName}', '{client.SecondName}', '{client.Birthdate:yyyy-MM-dd}', {(int)client.Gender}, {client.LegalEntity.ToString().ToLower()}, '{client.Company}', '{client.MembershipId}', {(int)client.Type}, {client.IsDeleted.ToString().ToLower()}, '{client.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{client.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForCommunications(List<Communication> communications)
        {
            StringBuilder script = new StringBuilder();
            foreach (var comm in communications)
            {
                script.AppendLine($@"INSERT INTO public.communication (id, client_id, prefix, type, value, description, is_deleted, create_time, current_user_created) 
                    VALUES ('{comm.Id}', '{comm.ClientId}', '{comm.Prefix}', {(int)comm.Type}, '{comm.Value}', '{comm.Description}', {comm.IsDeleted.ToString().ToLower()}, '{comm.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{comm.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForMemberships(List<Membership> memberships)
        {
            StringBuilder script = new StringBuilder();
            foreach (var membership in memberships)
            {
                script.AppendLine($@"INSERT INTO public.membership (id, client_id, type, valid_from, valid_until, contract_id, is_deleted, create_time, current_user_created) 
                    VALUES ('{membership.Id}', '{membership.ClientId}', {membership.Type}, '{membership.ValidFrom:yyyy-MM-dd}', {(membership.ValidUntil.HasValue ? $"'{membership.ValidUntil.Value:yyyy-MM-dd}'" : "NULL")}, {(membership.ContractId.HasValue ? $"'{membership.ContractId}'" : "NULL")}, {membership.IsDeleted.ToString().ToLower()}, '{membership.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{membership.CurrentUserCreated}');");
            }

            return script.ToString();
        }

        public static string GenerateInsertScriptForSettings()
        {
            StringBuilder script = new StringBuilder();

            var settings = new[]
            {
                // Email Settings
                ("0f807cbb-e54a-4f5b-9383-5b03ccffc55d", "authenticationType", "LOGIN"),
                ("3be9b255-4b0b-49fd-8585-556375187dac", "outgoingserver", "mail.gmx.net"),
                ("789530bc-18a3-48b1-946f-a5da6d66d357", "enabledSSL", "true"),
                ("8d8b2ae3-7d7b-4f31-9778-0e348deb1fca", "dispositionNotification", "false"),
                ("91f43fe3-0db7-4554-aa4d-8dac0151f118", "replyTo", "hgasparoli@gmx.ch"),
                ("db3ee771-cbd6-420c-bdf7-8b1036bb82b9", "outgoingserverPort", "587"),
                ("e16842eb-24ff-47c2-ad1b-5a3d6a2d20cd", "outgoingserverTimeout", "100"),
                ("e3e61605-c1e9-48b9-b5c7-9e66c41889fe", "readReceipt", "false"),
                ("a1b2c3d4-e5f6-7890-abcd-ef1234567890", "outgoingserverUsername", "hgasparoli@gmx.ch"),
                ("a1b2c3d4-e5f6-7890-abcd-ef1234567891", "outgoingserverPassword", ""),
                ("a1b2c3d4-e5f6-7890-abcd-ef1234567892", "mark", ""),
                
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

        public static string GenerateInsertScriptForGroupItems(List<Client> clients, List<Address> addresses)
        {
            StringBuilder script = new StringBuilder();
            var currentTime = DateTime.Now;
            var userId = user;
            var random = Random.Shared;

            var cantonGroupNames = new[] { "ZH", "BE", "LU", "SG", "AG", "BS", "BL", "GE", "VD", "NE", "JU", "FR" };

            script.AppendLine("\n-- GroupItem entries for Client-Group assignments based on postal codes");

            foreach (var client in clients)
            {
                var clientAddresses = addresses.Where(a => a.ClientId == client.Id).ToList();

                foreach (var address in clientAddresses)
                {
                    string cantonAbbr = address.Zip switch
                    {
                        var pc when pc.StartsWith("8") => "ZH", // Zürich
                        var pc when pc.StartsWith("3") => "BE", // Bern
                        var pc when pc.StartsWith("6") => "LU", // Luzern
                        var pc when pc.StartsWith("9") => "SG", // St. Gallen
                        var pc when pc.StartsWith("5") => "AG", // Aargau
                        var pc when pc.StartsWith("4") => "BS", // Basel
                        var pc when pc.StartsWith("1") && address.City.Contains("Genève") => "GE", // Genf
                        var pc when pc.StartsWith("1") => "VD", // Waadt
                        var pc when pc.StartsWith("2") && address.City.Contains("Neuchâtel") => "NE", // Neuenburg
                        var pc when pc.StartsWith("2") && address.City.Contains("Delémont") => "JU", // Jura
                        var pc when pc.StartsWith("2") => "BE", // Bern (Biel region)
                        var pc when pc.StartsWith("7") => "GR", // Graubünden - not in our list, use random
                        _ => cantonGroupNames[random.Next(cantonGroupNames.Length)] // Array bleibt .Length
                    };

                    if (!cantonGroupNames.Contains(cantonAbbr))
                    {
                        cantonAbbr = cantonGroupNames[random.Next(cantonGroupNames.Length)]; // Array bleibt .Length
                    }

                    var groupItemId = Guid.NewGuid();

                    script.AppendLine($@"INSERT INTO public.group_item (id, client_id, group_id, shift_id, create_time, current_user_created, is_deleted) 
                        SELECT '{groupItemId}', '{client.Id}', g.id, NULL, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', false 
                        FROM public.""group"" g 
                        WHERE g.name = '{cantonAbbr}' AND g.is_deleted = false 
                        LIMIT 1;");
                }
            }

            return script.ToString();
        }

        #endregion

        #region Shift Generation Methods (from ShiftSeed)

        public static (string script, List<Guid> shiftIds) GenerateInsertScriptForShifts()
        {
            StringBuilder script = new StringBuilder();
            var shiftIds = new List<Guid>();
            var usedNames = new HashSet<string>();
            var usedAbbreviations = new HashSet<string>();

            var baseDate = new DateOnly(2025, 1, 1);
            var currentTime = DateTime.Now;
            var userId = user;
            var random = Random.Shared;

            // Helper function to generate unique names and abbreviations
            string GetUniqueName(string baseName, int counter)
            {
                var name = counter == 1 ? baseName : $"{baseName} {counter}";
                while (usedNames.Contains(name))
                {
                    counter++;
                    name = $"{baseName} {counter}";
                }

                usedNames.Add(name);
                return name;
            }

            string GetUniqueAbbreviation(string baseAbbr, int counter)
            {
                var abbr = counter == 1 ? baseAbbr : $"{baseAbbr}{counter}";
                while (usedAbbreviations.Contains(abbr))
                {
                    counter++;
                    abbr = $"{baseAbbr}{counter}";
                }

                usedAbbreviations.Add(abbr);
                return abbr;
            }

            // Base simple shifts definitions
            var simpleShifts = new[]
            {
                new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, CuttingAfterMidnight = false },
                new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = true },
                new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, CuttingAfterMidnight = false },
                new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "24:00:00", WorkTime = 24, Employees = 1, CuttingAfterMidnight = false }
            };

            script.AppendLine("-- Shift Seed Data");

            // Create simple shifts
            foreach (var shift in simpleShifts)
            {
                var simpleShiftId = Guid.NewGuid();
                var uniqueName = GetUniqueName(shift.Name, 1);
                var uniqueAbbr = GetUniqueAbbreviation(shift.Abbr, 1);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{simpleShiftId}', {(shift.CuttingAfterMidnight ? "true" : "false")}, '{shift.Name} mit {shift.Employees} Mitarbeiter(n)', '00000000-0000-0000-0000-000000000000', '{uniqueName}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '{shift.End}', '{baseDate:yyyy-MM-dd}', '{shift.Start}', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    {shift.WorkTime}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbr}', '00:00:00', NULL,
                    '00:00:00', {shift.Employees}, 0, NULL, NULL
                );");

                shiftIds.Add(simpleShiftId);
            }

            // 1. 10x 24-Stunden Schichten (07:00 - 07:00) mit 3 Teilungen bei 15:00 und 23:00
            for (int i = 1; i <= 10; i++)
            {
                var shift24hRootId = Guid.NewGuid();
                var originalId = Guid.NewGuid();

                // Hauptschicht 24h - meist 1 MA, nur bei kritischen Schichten 2 MA
                var employeesPerShift = (i <= 2) ? 2 : 1; // Nur die ersten 2 Schichten haben 2 MA, Rest 1 MA
                var uniqueName24h = GetUniqueName("24h-Schichtdienst", i);
                var uniqueAbbr24h = GetUniqueAbbreviation("24H", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{shift24hRootId}', true, '24-Stunden Schichtdienst - {employeesPerShift} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueName24h}', '{shift24hRootId}', '{shift24hRootId}', 3,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
                    true, true, true, true, true, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    24, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(5):yyyy-MM-dd HH:mm:ss.ffffff}', '{originalId}', '{uniqueAbbr24h}', '00:00:00', NULL,
                    '00:00:00', {employeesPerShift}, 0, 1, 8
                );");

                // Add shift IDs for later group assignment
                shiftIds.Add(shift24hRootId);

                // Teilschicht 1: 07:00-15:00 (8h) - gleiche Mitarbeiteranzahl wie Hauptschicht
                var part1Id = Guid.NewGuid();
                var uniqueNameFrüh = GetUniqueName("Frühschicht-Teil", i);
                var uniqueAbbrFrüh = GetUniqueAbbreviation("F", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{part1Id}', false, 'Frühschicht - {employeesPerShift} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameFrüh}', '{shift24hRootId}', '{shift24hRootId}', 3,
                    '00:00:00', '00:00:00', '15:00:00', '{baseDate:yyyy-MM-dd}', '07:00:00', NULL,
                    true, true, true, true, true, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, NULL,
                    NULL, false, NULL, '{originalId}', '{uniqueAbbrFrüh}', '00:00:00', NULL,
                    '00:00:00', {employeesPerShift}, 0, 2, 3
                );");

                shiftIds.Add(part1Id);

                // Teilschicht 2: 15:00-23:00 (8h) - gleiche Mitarbeiteranzahl wie Hauptschicht
                var part2Id = Guid.NewGuid();
                var uniqueNameSpät = GetUniqueName("Spätschicht-Teil", i);
                var uniqueAbbrSpät = GetUniqueAbbreviation("S", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{part2Id}', false, 'Spätschicht - {employeesPerShift} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameSpät}', '{shift24hRootId}', '{shift24hRootId}', 3,
                    '00:00:00', '00:00:00', '23:00:00', '{baseDate:yyyy-MM-dd}', '15:00:00', NULL,
                    true, true, true, true, true, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, NULL,
                    NULL, false, NULL, '{originalId}', '{uniqueAbbrSpät}', '00:00:00', NULL,
                    '00:00:00', {employeesPerShift}, 0, 4, 5
                );");

                shiftIds.Add(part2Id);

                // Teilschicht 3: 23:00-07:00 (8h) - Nachtschicht - gleiche Mitarbeiteranzahl wie Hauptschicht
                var part3Id = Guid.NewGuid();
                var uniqueNameNacht = GetUniqueName("Nachtschicht-Teil", i);
                var uniqueAbbrNacht = GetUniqueAbbreviation("N", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{part3Id}', true, 'Nachtschicht - {employeesPerShift} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameNacht}', '{shift24hRootId}', '{shift24hRootId}', 3,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, true, true, true, true, true, true, true,
                    false, true, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, NULL,
                    NULL, false, NULL, '{originalId}', '{uniqueAbbrNacht}', '00:00:00', NULL,
                    '00:00:00', {employeesPerShift}, 0, 6, 7
                );");

                shiftIds.Add(part3Id);
            }

            // 2. 20 Morgenschichten (6 Stunden) - meist 1 MA, nur wenige mit 2 MA
            for (int i = 1; i <= 20; i++)
            {
                var morningShiftId = Guid.NewGuid();
                var startHour = random.Next(5, 8); // 05:00 bis 07:00
                var endHour = startHour + 6;
                var employees = (i <= 3) ? 2 : 1; // Nur die ersten 3 haben 2 MA
                var uniqueNameMorning = GetUniqueName("Morgenschicht", i);
                var uniqueAbbrMorning = GetUniqueAbbreviation("MOR", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{morningShiftId}', false, '6-Stunden Morgenschicht - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameMorning}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '{endHour:D2}:00:00', '{baseDate:yyyy-MM-dd}', '{startHour:D2}:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    6, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(10):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrMorning}', '00:00:00', NULL,
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(morningShiftId);
            }

            // 3. 30 Tagschichten Mo-Fr 08:00-17:00 - meist 1 MA, nur wenige mit mehr MA
            for (int i = 1; i <= 30; i++)
            {
                var dayShiftId = Guid.NewGuid();
                var employees = (i <= 5) ? 2 : 1; // Nur die ersten 5 haben 2 MA
                var uniqueNameDay = GetUniqueName("Tagschicht", i);
                var uniqueAbbrDay = GetUniqueAbbreviation("TAG", i + 100); // +100 to avoid conflict with simple "TAG"

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{dayShiftId}', false, 'Tagschicht Mo-Fr mit 1h Mittagspause - {employees} Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameDay}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '17:00:00', '{baseDate:yyyy-MM-dd}', '08:00:00', NULL,
                    true, false, true, false, false, true, true, true,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(15):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrDay}', '00:00:00', NULL,
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(dayShiftId);
            }

            // 4. 20 Nachtdienste Mo-Fr 23:00-07:00 - 1 Mitarbeiter pro Schicht
            for (int i = 1; i <= 20; i++)
            {
                var nightShiftId = Guid.NewGuid();
                var uniqueNameNightMF = GetUniqueName("Nachtdienst Mo-Fr", i);
                var uniqueAbbrNightMF = GetUniqueAbbreviation("NMF", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{nightShiftId}', true, 'Nachtdienst Mo-Fr - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightMF}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    true, false, true, false, false, true, true, false,
                    true, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(20):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightMF}', '00:00:00', NULL,
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(nightShiftId);
            }

            // 5. 20 Nachtdienste Sa-So 23:00-07:00 - 1 Mitarbeiter pro Schicht
            for (int i = 1; i <= 20; i++)
            {
                var weekendNightId = Guid.NewGuid();
                var uniqueNameNightSS = GetUniqueName("Nachtdienst Sa-So", i);
                var uniqueAbbrNightSS = GetUniqueAbbreviation("NSS", i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{weekendNightId}', true, 'Nachtdienst Sa-So - 1 Mitarbeiter pro Schicht', '00000000-0000-0000-0000-000000000000', '{uniqueNameNightSS}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '07:00:00', '{baseDate:yyyy-MM-dd}', '23:00:00', NULL,
                    false, false, false, true, true, false, false, false,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    8, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(25):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrNightSS}', '00:00:00', NULL,
                    '00:00:00', 1, 0, NULL, NULL
                );");

                shiftIds.Add(weekendNightId);
            }

            // 6. 6x Original-Only Shifts (keine Teilschichten, nur Hauptschicht)
            for (int i = 1; i <= 6; i++)
            {
                var originalOnlyId = Guid.NewGuid();
                var employees = (i == 1 || i == 2) ? 2 : 1; // Erste 2 haben 2 MA, Rest 1 MA

                // Verschiedene Schichttypen für Abwechslung
                var (startTime, endTime, hours, name, baseAbbr) = i switch
                {
                    1 => ("06:00:00", "14:00:00", 8, "Frühdienst Spezial", "FRÜSP"),
                    2 => ("14:00:00", "22:00:00", 8, "Spätdienst Spezial", "SPÄTSP"),
                    3 => ("22:00:00", "06:00:00", 8, "Nachtdienst Spezial", "NACHSP"),
                    4 => ("09:00:00", "17:00:00", 8, "Tagdienst Büro", "TAGBÜ"),
                    5 => ("12:00:00", "20:00:00", 8, "Nachmittag-Abend", "NMAB"),
                    6 => ("20:00:00", "04:00:00", 8, "Abend-Nacht", "ABNA"),
                    _ => ("08:00:00", "16:00:00", 8, "Standard", "STD")
                };

                var isCuttingAfterMidnight = startTime.CompareTo(endTime) > 0; // Über Mitternacht?
                var uniqueNameOriginal = GetUniqueName(name, 1);
                var uniqueAbbrOriginal = GetUniqueAbbreviation(baseAbbr, i);

                script.AppendLine($@"INSERT INTO public.shift (
                    id, cutting_after_midnight, description, macro_id, name, parent_id, root_id, status,
                    after_shift, before_shift, end_shift, from_date, start_shift, until_date,
                    is_friday, is_holiday, is_monday, is_saturday, is_sunday, is_thursday, is_tuesday, is_wednesday,
                    is_weekday_or_holiday, is_sporadic, is_time_range, quantity, travel_time_after, travel_time_before,
                    work_time, shift_type, create_time, current_user_created, current_user_deleted, current_user_updated,
                    deleted_time, is_deleted, update_time, original_id, abbreviation, briefing_time, client_id,
                    debriefing_time, sum_employees, sporadic_scope, lft, rgt
                ) VALUES (
                    '{originalOnlyId}', {(isCuttingAfterMidnight ? "true" : "false")}, 'Original-Only Schicht ohne Teilungen - {employees} Mitarbeiter', '00000000-0000-0000-0000-000000000000', '{uniqueNameOriginal}', NULL, NULL, 1,
                    '00:00:00', '00:00:00', '{endTime}', '{baseDate:yyyy-MM-dd}', '{startTime}', NULL,
                    true, false, true, true, true, true, true, true,
                    false, false, false, 1, '00:00:00', '00:00:00',
                    {hours}, 0, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', NULL, '{userId}',
                    NULL, false, '{currentTime.AddMinutes(30):yyyy-MM-dd HH:mm:ss.ffffff}', NULL, '{uniqueAbbrOriginal}', '00:00:00', NULL,
                    '00:00:00', {employees}, 0, NULL, NULL
                );");

                shiftIds.Add(originalOnlyId);
            }

            return (script.ToString(), shiftIds);
        }

        public static string GenerateInsertScriptForShiftGroupItems(List<Guid> shiftIds)
        {
            StringBuilder script = new StringBuilder();
            var currentTime = DateTime.Now;
            var userId = user;
            var random = Random.Shared;

            // Use the actual Canton Group IDs that were generated in FakeDataGroups
            var cantonGroupNames = new[] { "ZH", "BE", "LU", "SG", "AG", "BS", "BL", "GE", "VD", "NE", "JU", "FR" };

            script.AppendLine("\n-- GroupItem entries for Shift-Group assignments");
            script.AppendLine("-- Note: These use Canton group names from FakeDataGroups");

            foreach (var shiftId in shiftIds)
            {
                // Assign each shift to 1-2 random canton groups
                var numGroups = random.Next(1, 3);
                var selectedCantons = cantonGroupNames.OrderBy(x => random.Next()).Take(numGroups);

                foreach (var cantonName in selectedCantons)
                {
                    var groupItemId = Guid.NewGuid();

                    // We need to get the actual group ID at runtime using the canton name
                    script.AppendLine($@"INSERT INTO public.group_item (id, client_id, group_id, shift_id, create_time, current_user_created, is_deleted) 
                        SELECT '{groupItemId}', NULL, g.id, '{shiftId}', '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', false 
                        FROM public.""group"" g 
                        WHERE g.name = '{cantonName}' AND g.is_deleted = false 
                        LIMIT 1;");
                }
            }

            return script.ToString();
        }

        #endregion

        private static string GetRandomFirstName(GenderEnum gender,  int i)
        {
            Random rand = new Random();
            switch (gender)
            {
                case GenderEnum.Female:
                    return SeedNamesAndDescriptions.FemaleFirstNames[rand.Next(SeedNamesAndDescriptions.FemaleFirstNames.Count)];

                case GenderEnum.Male:
                    return SeedNamesAndDescriptions.MaleFirstNames[rand.Next(SeedNamesAndDescriptions.MaleFirstNames.Count)];

                default:
                    return GenerateMockName(i + 10);
            }
        }
    }
}