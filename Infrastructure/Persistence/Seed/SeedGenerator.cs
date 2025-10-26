using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Data.Seed.Generators;
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

        public static string GenerateCompanyName(Random rand) => RandomDataGenerator.GenerateCompanyName(rand);
               
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

        public static string GenerateMockName(int index) => RandomDataGenerator.GenerateMockName(index);

        private static List<PostcodeCH> GenerateComprehensiveSwissPostcodes()
            => PostcodeGenerator.GenerateComprehensiveSwissPostcodes();

        #endregion

        #region Random Generation Helper Methods

        public static DateTime GenerateRandomBirthday() => RandomDataGenerator.GenerateRandomBirthday();

        public static string GenerateRandomEmail() => RandomDataGenerator.GenerateRandomEmail();

        public static GenderEnum GenerateRandomGender() => RandomDataGenerator.GenerateRandomGender();

        private static string GenerateRandomNumber() => RandomDataGenerator.GenerateRandomPhoneNumber();

        public static string GenerateRandomString(int length) => RandomDataGenerator.GenerateRandomString(length);

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
                script.AppendLine($@"INSERT INTO public.membership (id, client_id, type, valid_from, valid_until, is_deleted, create_time, current_user_created)
                    VALUES ('{membership.Id}', '{membership.ClientId}', {membership.Type}, '{membership.ValidFrom:yyyy-MM-dd}', {(membership.ValidUntil.HasValue ? $"'{membership.ValidUntil.Value:yyyy-MM-dd}'" : "NULL")}, {membership.IsDeleted.ToString().ToLower()}, '{membership.CreateTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{membership.CurrentUserCreated}');");
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
                var validFrom = client.Membership?.ValidFrom ?? currentTime;
                var validFromStr = validFrom.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

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

                    script.AppendLine($@"INSERT INTO public.group_item (id, client_id, group_id, shift_id, valid_from, valid_until, create_time, current_user_created, is_deleted)
                        SELECT '{groupItemId}', '{client.Id}', g.id, NULL, '{validFromStr}', NULL, '{currentTime:yyyy-MM-dd HH:mm:ss.ffffff}', '{userId}', false
                        FROM public.""group"" g
                        WHERE g.name = '{cantonAbbr}' AND g.is_deleted = false
                        LIMIT 1;");
                }
            }

            return script.ToString();
        }

        #endregion

        private static string GetRandomFirstName(GenderEnum gender, int i)
            => RandomDataGenerator.GetRandomFirstName(gender, i);
    }
}