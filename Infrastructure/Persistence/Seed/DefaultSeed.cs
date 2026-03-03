// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    /// <summary>
    /// User Default, Communications Type, Settings, Countries, States.
    /// </summary>
    public static class DefaultSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO ""AspNetRoles"" (id, concurrency_stamp, name, normalized_name) VALUES
                ('9c05bb10-5855-4201-a755-1d92ed9df000', 'd94790da-0103-4ade-b715-29526b2b1fc7', 'Authorised', 'AUTHORISED'),
                ('e32d7319-6861-4c9a-b096-08a77088cadd', '402b8312-92a7-43f4-be73-b3400ccc2a7b', 'Admin', 'ADMIN')
                ON CONFLICT (id) DO NOTHING;"
            );

            migrationBuilder.Sql(
                @"INSERT INTO ""AspNetUsers"" (id, access_failed_count, concurrency_stamp, discriminator, email, email_confirmed, first_name, last_name, lockout_enabled, lockout_end, normalized_email, normalized_user_name, password_hash, phone_number, phone_number_confirmed, security_stamp, two_factor_enabled, user_name) VALUES
                ('672f77e8-e479-4422-8781-84d218377fb3', 0, '217b0216-5440-4e51-a6e4-ea79d0da9155', 'AppUser', 'admin@test.com', true, 'admin', 'admin', false, null, 'ADMIN@TEST.COM', 'ADMIN', 'AQAAAAEAACcQAAAAEM4rFqzwCkNDdqC7P5XDITL1ub4TLm1MPZMru7BlKyFLNSRfaamO4BUl/fAV4aNNlA==', '123456789', false, 'a04e4667-082e-43df-b82a-3ff914fc7db7', false, 'admin')
                ON CONFLICT (id) DO NOTHING;"
            );

            migrationBuilder.Sql(
                @"INSERT INTO communication_type (id, category, default_index, name, type) VALUES
                (1, 0, 0, 'Festnetz P', 0),
                (2, 0, 1, 'Mobil P', 1),
                (3, 0, 0, 'Festnetz G', 2),
                (4, 0, 0, 'Mobil G', 3),
                (5, 0, 0, 'NotfallNo', 7),
                (6, 1, 2, 'Email P', 4),
                (7, 1, 0, 'Email G', 5),
                (8, 0, 0, 'Anderes', 6)
                ON CONFLICT (id) DO NOTHING;"
            );

            migrationBuilder.InsertData(
                table: "settings",
                columns: new[] { "id", "type", "value" },
                values: new object[,]
                {
                    { new Guid("0f807cbb-e54a-4f5b-9383-5b03ccffc55d"), "authenticationType", "LOGIN" },
                    { new Guid("3be9b255-4b0b-49fd-8585-556375187dac"), "outgoingserver", "mail.gmx.net" },
                    { new Guid("789530bc-18a3-48b1-946f-a5da6d66d357"), "enabledSSL", "true" },
                    { new Guid("8d8b2ae3-7d7b-4f31-9778-0e348deb1fca"), "dispositionNotification" , "false"},
                    { new Guid("91f43fe3-0db7-4554-aa4d-8dac0151f118"), "replyTo" , "hgasparoli@gmx.ch"},
                    { new Guid("d5bbf185-b799-4aa4-86ca-c3fe879654f2"), "klacks-net", "mark" },
                    { new Guid("db3ee771-cbd6-420c-bdf7-8b1036bb82b9"), "outgoingserverPort" , "587"},
                    { new Guid("e16842eb-24ff-47c2-ad1b-5a3d6a2d20cd"), "outgoingserverTimeout", "100" },
                    { new Guid("e3e61605-c1e9-48b9-b5c7-9e66c41889fe"), "readReceipt" , "false"},
                    { new Guid("d3f33e97-c4c4-4c05-93d9-68ff87b02c5e"), "outgoingserverUsername", "hgasparoli@gmx.ch"},
                    { new Guid("e3e61605-c1e9-48b9-b5c7-9e66c41889fe"), "outgoingserverPassword", ""},
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"), "defaultWorkingHours", "8.5" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5e"), "overtimeThreshold", "42" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5f"), "vacationDaysPerYear", "25" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c60"), "probationPeriod", "3" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c61"), "noticePeriod", "30" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c62"), "paymentInterval", "2" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c63"), "guaranteedHours", "170" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c64"), "maximumHours", "200" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c65"), "minimumHours", "160" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c66"), "fullTime", "180" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c67"), "nightRate", "0.1" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c6a"), "holidayRate", "0.1" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c68"), "saRate", "0.1" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c69"), "soRate", "0.1" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c70"), "dayVisibleBeforeMonth", "10" },
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c71"), "dayVisibleAfterMonth", "10" }
                });

            migrationBuilder.Sql(
                @"INSERT INTO ""AspNetUserRoles"" (role_id, user_id) VALUES 
                ('e32d7319-6861-4c9a-b096-08a77088cadd', '672f77e8-e479-4422-8781-84d218377fb3')
                ON CONFLICT (role_id, user_id) DO NOTHING;"
            );

            migrationBuilder.Sql(
                @"INSERT INTO public.countries (id, abbreviation, name, prefix, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                ('c6cdb912-022d-47bf-baf0-49a80d24948c', 'CH', '{""de"":""Schweiz"",""en"":""Switzerland"",""fr"":""Suisse"",""it"":""Svizzera""}'::jsonb, '+41', '2023-06-10 11:57:38.449+00', 'admin', NULL, '', NULL, false, ''),
                ('1aa91576-f08e-4498-b47b-60d09aa2b614', 'DE', '{""de"":""Deutschland"",""en"":""Germany"",""fr"":""Allemagne"",""it"":""Germania""}'::jsonb, '+49', '2023-06-10 12:00:25.999+00', 'admin', NULL, '', NULL, false, ''),
                ('59c98d2e-0634-489b-8e44-a6f7d5532b6b', 'AT', '{""de"":""Österreich"",""en"":""Austria"",""fr"":""Autriche"",""it"":""Austria""}'::jsonb, '+43', '2023-06-10 13:00:39.491+00', 'admin', NULL, '', NULL, false, ''),
                ('11046a85-dfc0-48c8-878b-53b724b9c0d9', 'LI', '{""de"":""Fürstentum Liechtenstein"",""en"":""Principality of Liechtenstein"",""fr"":""Principauté de Liechtenstein"",""it"":""Principato del Liechtenstein""}'::jsonb, '+423', '2023-06-10 13:26:04.182+00', 'admin', NULL, '', NULL, false, ''),
                ('276e0392-bfa3-4230-b8a7-8e9fdfecad57', 'USA', '{""de"":""Vereinigte Staaten von Amerika"",""en"":""United States of America"",""fr"":""États-Unis d''''Amérique"",""it"":""Stati Uniti d''''America""}'::jsonb, '+1', '2023-07-10 07:17:54.545+00', 'admin', NULL, '', NULL, false, ''),
                ('d8084ef6-df46-46ea-a71e-a708898d1650', 'IT', '{""de"":""Italien"",""en"":""Italy"",""fr"":""Italie"",""it"":""Italia""}'::jsonb, '+39', '2023-07-10 07:14:42.454+00', 'admin', NULL, '', NULL, false, ''),
                ('2d576ac5-7978-4e3c-b92b-e3d67493aecb', 'FR', '{""de"":""Frankreich"",""en"":""France"",""fr"":""France"",""it"":""Francia""}'::jsonb, '+33', '2023-07-10 07:14:42.454+00', 'admin', NULL, '', NULL, false, '')"
            );

            migrationBuilder.Sql(
                @"INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                 ('57447b52-1f3e-4d62-a4b3-93c9c72b7ce6', 'AG', 'CH', '{""de"":""Aargau"",""en"":""Aargau"",""fr"":""Argovie"",""it"":""Argovia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('5c1921ad-8b6c-4374-952d-3e9899d2a663', 'AI', 'CH', '{""de"":""Appenzell Innerrhoden"",""en"":""Appenzell Innerrhoden"",""fr"":""Appenzell Rhodes-Intérieures"",""it"":""Appenzello Interno""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('72a92755-95ff-48d5-963f-7681e26888a0', 'AR', 'CH', '{""de"":""Appenzell Ausserrhoden"",""en"":""Appenzell Ausserrhoden"",""fr"":""Appenzell Rhodes-Extérieures"",""it"":""Appenzello Esterno""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('8d0ea829-b098-4cde-bf78-53f301d692eb', 'BE', 'CH', '{""de"":""Bern"",""en"":""Bern"",""fr"":""Berne"",""it"":""Berna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('db4177d6-09bc-4c1a-89ae-7e0c3b549c32', 'BL', 'CH', '{""de"":""Basel-Landschaft"",""en"":""Basel-Landschaft"",""fr"":""Bâle-Campagne"",""it"":""Basilea Campagna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('c37d6794-682b-47c2-8d9c-59a78823a9a9', 'BS', 'CH', '{""de"":""Basel-Stadt"",""en"":""Basel-Stadt"",""fr"":""Bâle-Ville"",""it"":""Basilea Città""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cb5efcaa-6524-4d84-96ad-b44029315514', 'FR', 'CH', '{""de"":""Freiburg"",""en"":""Fribourg"",""fr"":""Fribourg"",""it"":""Friburgo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('8c5f8f1c-f0f7-4e6b-9f5f-31f869bb53a2', 'GE', 'CH', '{""de"":""Genf"",""en"":""Geneva"",""fr"":""Genève"",""it"":""Ginevra""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('6e6d9834-7559-4189-9d63-68d4a692d6cc', 'GL', 'CH', '{""de"":""Glarus"",""en"":""Glarus"",""fr"":""Glaris"",""it"":""Glarona""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('ab1b707b-419e-4bcf-9f36-2cc9e28ea82f', 'GR', 'CH', '{""de"":""Graubünden"",""en"":""Graubünden"",""fr"":""Grisons"",""it"":""Grigioni""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('8238ab69-c7d0-4e97-9763-5c052f748660', 'JU', 'CH', '{""de"":""Jura"",""en"":""Jura"",""fr"":""Jura"",""it"":""Giura""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('ee5508b7-3709-4cfd-91e7-9ee4e532d876', 'LU', 'CH', '{""de"":""Luzern"",""en"":""Lucerne"",""fr"":""Lucerne"",""it"":""Lucerna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('b0b2e204-08f7-44d7-b7ef-5f330ff7a063', 'NE', 'CH', '{""de"":""Neuenburg"",""en"":""Neuchâtel"",""fr"":""Neuchâtel"",""it"":""Neuchâtel""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('f7a01dc0-14c6-43d6-bf23-3a2ed0c22b44', 'NW', 'CH', '{""de"":""Nidwalden"",""en"":""Nidwalden"",""fr"":""Nidwald"",""it"":""Nidvaldo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('c116d12d-0d2d-4416-b0a3-fd6f69933b47', 'OW', 'CH', '{""de"":""Obwalden"",""en"":""Obwalden"",""fr"":""Obwald"",""it"":""Obvaldo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('82661c57-c6f1-4af3-8a8f-18e6e09783c9', 'SG', 'CH', '{""de"":""St. Gallen"",""en"":""St. Gallen"",""fr"":""Saint-Gall"",""it"":""San Gallo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('758ed70b-7d6f-44e9-a1cc-021677af7e72', 'SH', 'CH', '{""de"":""Schaffhausen"",""en"":""Schaffhausen"",""fr"":""Schaffhouse"",""it"":""Sciaffusa""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('8054ad64-7e2c-4963-9ae4-8e3e468bbae1', 'SO', 'CH', '{""de"":""Solothurn"",""en"":""Solothurn"",""fr"":""Soleure"",""it"":""Soletta""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('c6ea12c0-7a7f-43f4-a38b-73e35d3b1e9c', 'SZ', 'CH', '{""de"":""Schwyz"",""en"":""Schwyz"",""fr"":""Schwytz"",""it"":""Svitto""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('28b58797-7f7a-4d78-a4e7-cc1e411ac6f6', 'TG', 'CH', '{""de"":""Thurgau"",""en"":""Thurgau"",""fr"":""Thurgovie"",""it"":""Turgovia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('0ff741f7-4020-4a03-8906-bd0efae81f1b', 'TI', 'CH', '{""de"":""Tessin"",""en"":""Ticino"",""fr"":""Ticino"",""it"":""Ticino""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('65b8bfc6-826b-4a51-98de-34f3506a3f3e', 'UR', 'CH', '{""de"":""Uri"",""en"":""Uri"",""fr"":""Uri"",""it"":""Uri""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('1836b680-3f1c-4310-8368-726fd0f0d15f', 'VD', 'CH', '{""de"":""Waadt"",""en"":""Vaud"",""fr"":""Vaud"",""it"":""Vaud""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('93ab02ce-9a3f-4ab7-b08c-90a6b6181fda', 'VS', 'CH', '{""de"":""Wallis"",""en"":""Valais"",""fr"":""Valais"",""it"":""Vallese""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('853e28a6-1571-416e-b300-90c6a55fe9ee', 'ZG', 'CH', '{""de"":""Zug"",""en"":""Zug"",""fr"":""Zoug"",""it"":""Zugo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('e972c351-ee41-4007-9e9e-b85d8a2f620f', 'ZH', 'CH', '{""de"":""Zürich"",""en"":""Zurich"",""fr"":""Zurich"",""it"":""Zurigo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('7ff80e52-b081-4da9-9d75-0dcb1ba5f1c8', 'BW', 'DE', '{""de"":""Baden-Württemberg"",""en"":""Baden-Württemberg"",""fr"":""Bade-Wurtemberg"",""it"":""Baden-Württemberg""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('7f38ca0d-f31a-4324-9419-1103d24cc2b6', 'BY', 'DE', '{""de"":""Bayern"",""en"":""Bavaria"",""fr"":""Bavière"",""it"":""Baviera""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cfb93e71-fc19-438c-b274-f89ccb9406ca', 'BE', 'DE', '{""de"":""Berlin"",""en"":""Berlin"",""fr"":""Berlin"",""it"":""Berlino""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('5e819994-0b0e-40be-a764-d56ea9b8c5d5', 'BB', 'DE', '{""de"":""Brandenburg"",""en"":""Brandenburg"",""fr"":""Brandebourg"",""it"":""Brandeburgo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('859e0595-9e35-458d-abbf-5c144d51441b', 'HB', 'DE', '{""de"":""Bremen"",""en"":""Bremen"",""fr"":""Brême"",""it"":""Brema""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('2e98f130-837e-431d-b1f9-48e6ec884202', 'HH', 'DE', '{""de"":""Hamburg"",""en"":""Hamburg"",""fr"":""Hambourg"",""it"":""Amburgo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('90946750-fac5-49c5-85d6-3f9eda87e90e', 'HE', 'DE', '{""de"":""Hessen"",""en"":""Hesse"",""fr"":""Hesse"",""it"":""Assia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('bdb79500-e320-4228-ae57-64ae9b68f185', 'MV', 'DE', '{""de"":""Mecklenburg-Vorpommern"",""en"":""Mecklenburg-Vorpommern"",""fr"":""Mecklembourg-Poméranie-Occidentale"",""it"":""Meclemburgo-Pomerania Anteriore""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('1b8e617e-6ad3-4114-8098-b39737352b92', 'NI', 'DE', '{""de"":""Niedersachsen"",""en"":""Lower Saxony"",""fr"":""Basse-Saxe"",""it"":""Bassa Sassonia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('a2e19e83-4533-4fb3-b295-574623ce326e', 'NW', 'DE', '{""de"":""Nordrhein-Westfalen"",""en"":""North Rhine-Westphalia"",""fr"":""Rhénanie-du-Nord-Westphalie"",""it"":""Renania Settentrionale-Vestfalia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('bd78d91c-bed3-4da2-b473-94cfe2fdd2a0', 'RP', 'DE', '{""de"":""Rheinland-Pfalz"",""en"":""Rhineland-Palatinate"",""fr"":""Rhénanie-Palatinat"",""it"":""Renania-Palatinato""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('be66c70d-748d-4547-b420-e46d4418d04f', 'SL', 'DE', '{""de"":""Saarland"",""en"":""Saarland"",""fr"":""Sarre"",""it"":""Saarland""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('91070049-b278-44a4-b90c-4cd6d7a2c5f5', 'SN', 'DE', '{""de"":""Sachsen"",""en"":""Saxony"",""fr"":""Saxe"",""it"":""Sassonia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('82cbf0e0-41ca-47fa-b3dc-115bf10cfba9', 'ST', 'DE', '{""de"":""Sachsen-Anhalt"",""en"":""Saxony-Anhalt"",""fr"":""Saxe-Anhalt"",""it"":""Sassonia-Anhalt""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('675839ca-af48-4a3c-9704-6f161f4b26f3', 'SH', 'DE', '{""de"":""Schleswig-Holstein"",""en"":""Schleswig-Holstein"",""fr"":""Schleswig-Holstein"",""it"":""Schleswig-Holstein""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('9dcdecc4-8e20-41b5-aba1-8bf71e62b024', 'TH', 'DE', '{""de"":""Thüringen"",""en"":""Thuringia"",""fr"":""Thuringe"",""it"":""Turingia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('32b2cb96-e1e1-46dd-b32e-af79329fca93', 'BG', 'AT', '{""de"":""Burgenland"",""en"":""Burgenland"",""fr"":""Burgenland"",""it"":""Burgenland""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('9357259a-b2cc-455a-a3e1-e8fde23c4de5', 'K', 'AT', '{""de"":""Kärnten"",""en"":""Carinthia"",""fr"":""Carinthie"",""it"":""Carinzia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('805cb509-17a3-4eb6-ad50-e1004582f144', 'NÖ', 'AT', '{""de"":""Niederösterreich"",""en"":""Lower Austria"",""fr"":""Basse-Autriche"",""it"":""Bassa Austria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('89a6d230-37b0-40f0-a405-b87528ff0aef', 'OÖ', 'AT', '{""de"":""Oberösterreich"",""en"":""Upper Austria"",""fr"":""Haute-Autriche"",""it"":""Alta Austria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('7eeef546-e9e2-4643-85de-99ff66b81e90', 'S', 'AT', '{""de"":""Salzburg"",""en"":""Salzburg"",""fr"":""Salzbourg"",""it"":""Salisburgo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('55549cf8-9261-4f6e-bbac-ef09ec1022c7', 'ST', 'AT', '{""de"":""Steiermark"",""en"":""Styria"",""fr"":""Styrie"",""it"":""Stiria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('f3a3a47a-cf48-4f88-97e2-7446c7260e83', 'T', 'AT', '{""de"":""Tirol"",""en"":""Tyrol"",""fr"":""Tyrol"",""it"":""Tirolo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('aa1b595c-f4cd-4b86-bf9d-b39a98fad16b', 'V', 'AT', '{""de"":""Vorarlberg"",""en"":""Vorarlberg"",""fr"":""Vorarlberg"",""it"":""Vorarlberg""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL);
INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                 ('88cbf7ed-70fc-4fe2-b76b-00b132e3c068', 'W', 'AT', '{""de"":""Wien"",""en"":""Vienna"",""fr"":""Vienne"",""it"":""Vienna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('63c26057-f6b9-45c8-8019-5fb535ec2934', 'ABR', 'IT', '{""de"":""Abruzzen"",""en"":""Abruzzo"",""fr"":""Abruzzes"",""it"":""Abruzzo""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('813fdcee-ddd8-4f52-9abe-58b2f97bcfbc', 'BAS', 'IT', '{""de"":""Basilikata"",""en"":""Basilicata"",""fr"":""Basilicate"",""it"":""Basilicata""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('7f7237f8-eafa-4295-beca-a476d5b6e20f', 'CAL', 'IT', '{""de"":""Kalabrien"",""en"":""Calabria"",""fr"":""Calabre"",""it"":""Calabria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('ab09134a-a88a-4a4b-995d-b2dd0f435839', 'CAM', 'IT', '{""de"":""Kampanien"",""en"":""Campania"",""fr"":""Campanie"",""it"":""Campania""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('edc1374f-5212-4c2d-a381-b970ca87fc24', 'EMR', 'IT', '{""de"":""Emilia-Romagna"",""en"":""Emilia-Romagna"",""fr"":""Émilie-Romagne"",""it"":""Emilia-Romagna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('2a81ac95-6627-4373-9661-2711cd71da77', 'FVG', 'IT', '{""de"":""Friaul-Julisch Venetien"",""en"":""Friuli-Venezia Giulia"",""fr"":""Frioul-Vénétie julienne"",""it"":""Friuli-Venezia Giulia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('f9ea7684-e5f8-4cfa-a5fd-4bd3bfb808e3', 'LAZ', 'IT', '{""de"":""Latium"",""en"":""Lazio"",""fr"":""Latium"",""it"":""Lazio""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('26d97b9a-5908-4def-9027-9867d635bae6', 'LIG', 'IT', '{""de"":""Ligurien"",""en"":""Liguria"",""fr"":""Ligurie"",""it"":""Liguria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('237fd478-5178-47af-bc0a-057d5fa4f09e', 'LOM', 'IT', '{""de"":""Lombardei"",""en"":""Lombardy"",""fr"":""Lombardie"",""it"":""Lombardia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('c3490327-b4d0-4865-9145-16d0487b765d', 'MAR', 'IT', '{""de"":""Marken"",""en"":""Marche"",""fr"":""Marches"",""it"":""Marche""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('4672cc43-2762-4eb4-9280-93081c7ee822', 'MOL', 'IT', '{""de"":""Molise"",""en"":""Molise"",""fr"":""Molise"",""it"":""Molise""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('1bcb825c-41ce-44c0-b929-c1e44bfa3436', 'PAB', 'IT', '{""de"":""Piemont"",""en"":""Piedmont"",""fr"":""Piemont"",""it"":""Piemonte""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('04dfe7d9-7601-4219-969b-17492cd02397', 'PAT', 'IT', '{""de"":""Apulien"",""en"":""Apulia"",""fr"":""Pouilles"",""it"":""Puglia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('bc15118b-4f09-47e7-a43c-8d701d56ba01', 'SAR', 'IT', '{""de"":""Sardinien"",""en"":""Sardinia"",""fr"":""Sardaigne"",""it"":""Sardegna""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('e8e57d91-8a59-4ca6-870f-cf81a7333d83', 'SIC', 'IT', '{""de"":""Sizilien"",""en"":""Sicily"",""fr"":""Sicile"",""it"":""Sicilia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('ad028558-4d5a-4645-8740-bb57da97cc71', 'TOS', 'IT', '{""de"":""Toskana"",""en"":""Tuscany"",""fr"":""Toscane"",""it"":""Toscana""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('a6864072-82a9-42ba-83b9-d1e5a2cce840', 'TAA', 'IT', '{""de"":""Trentino-Südtirol"",""en"":""Trentino-Alto Adige"",""fr"":""Trentin-Haut-Adige"",""it"":""Trentino-Alto Adige""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('11771795-8cb1-4152-9c09-a40c4ad05fb8', 'UMB', 'IT', '{""de"":""Umbrien"",""en"":""Umbria"",""fr"":""Ombrie"",""it"":""Umbria""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('07d0f694-34fd-4a19-a5f3-da4bad46c4fd', 'VEN', 'IT', '{""de"":""Venetien"",""en"":""Veneto"",""fr"":""Vénétie"",""it"":""Veneto""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000001', 'AL', 'USA', '{""de"":""Alabama"",""en"":""Alabama"",""fr"":""Alabama"",""it"":""Alabama""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000002', 'AK', 'USA', '{""de"":""Alaska"",""en"":""Alaska"",""fr"":""Alaska"",""it"":""Alaska""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000003', 'AZ', 'USA', '{""de"":""Arizona"",""en"":""Arizona"",""fr"":""Arizona"",""it"":""Arizona""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000004', 'AR', 'USA', '{""de"":""Arkansas"",""en"":""Arkansas"",""fr"":""Arkansas"",""it"":""Arkansas""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000005', 'CA', 'USA', '{""de"":""California"",""en"":""California"",""fr"":""Californie"",""it"":""California""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000006', 'CO', 'USA', '{""de"":""Colorado"",""en"":""Colorado"",""fr"":""Colorado"",""it"":""Colorado""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000007', 'CT', 'USA', '{""de"":""Connecticut"",""en"":""Connecticut"",""fr"":""Connecticut"",""it"":""Connecticut""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000008', 'DE', 'USA', '{""de"":""Delaware"",""en"":""Delaware"",""fr"":""Delaware"",""it"":""Delaware""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000009', 'FL', 'USA', '{""de"":""Florida"",""en"":""Florida"",""fr"":""Floride"",""it"":""Florida""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000010', 'GA', 'USA', '{""de"":""Georgia"",""en"":""Georgia"",""fr"":""Géorgie"",""it"":""Georgia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000011', 'HI', 'USA', '{""de"":""Hawaii"",""en"":""Hawaii"",""fr"":""Hawaï"",""it"":""Hawaii""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000012', 'ID', 'USA', '{""de"":""Idaho"",""en"":""Idaho"",""fr"":""Idaho"",""it"":""Idaho""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000013', 'IL', 'USA', '{""de"":""Illinois"",""en"":""Illinois"",""fr"":""Illinois"",""it"":""Illinois""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000014', 'IN', 'USA', '{""de"":""Indiana"",""en"":""Indiana"",""fr"":""Indiana"",""it"":""Indiana""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000015', 'IA', 'USA', '{""de"":""Iowa"",""en"":""Iowa"",""fr"":""Iowa"",""it"":""Iowa""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000016', 'KS', 'USA', '{""de"":""Kansas"",""en"":""Kansas"",""fr"":""Kansas"",""it"":""Kansas""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000017', 'KY', 'USA', '{""de"":""Kentucky"",""en"":""Kentucky"",""fr"":""Kentucky"",""it"":""Kentucky""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000018', 'LA', 'USA', '{""de"":""Louisiana"",""en"":""Louisiana"",""fr"":""Louisiane"",""it"":""Louisiana""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000019', 'ME', 'USA', '{""de"":""Maine"",""en"":""Maine"",""fr"":""Maine"",""it"":""Maine""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000020', 'MD', 'USA', '{""de"":""Maryland"",""en"":""Maryland"",""fr"":""Maryland"",""it"":""Maryland""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000021', 'MA', 'USA', '{""de"":""Massachusetts"",""en"":""Massachusetts"",""fr"":""Massachusetts"",""it"":""Massachusetts""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000022', 'MI', 'USA', '{""de"":""Michigan"",""en"":""Michigan"",""fr"":""Michigan"",""it"":""Michigan""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000023', 'MN', 'USA', '{""de"":""Minnesota"",""en"":""Minnesota"",""fr"":""Minnesota"",""it"":""Minnesota""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000024', 'MS', 'USA', '{""de"":""Mississippi"",""en"":""Mississippi"",""fr"":""Mississippi"",""it"":""Mississippi""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000025', 'MO', 'USA', '{""de"":""Missouri"",""en"":""Missouri"",""fr"":""Missouri"",""it"":""Missouri""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000026', 'MT', 'USA', '{""de"":""Montana"",""en"":""Montana"",""fr"":""Montana"",""it"":""Montana""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000027', 'NE', 'USA', '{""de"":""Nebraska"",""en"":""Nebraska"",""fr"":""Nebraska"",""it"":""Nebraska""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000028', 'NV', 'USA', '{""de"":""Nevada"",""en"":""Nevada"",""fr"":""Nevada"",""it"":""Nevada""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000029', 'NH', 'USA', '{""de"":""New Hampshire"",""en"":""New Hampshire"",""fr"":""New Hampshire"",""it"":""New Hampshire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000030', 'NJ', 'USA', '{""de"":""New Jersey"",""en"":""New Jersey"",""fr"":""New Jersey"",""it"":""New Jersey""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL);
INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                 ('cf9361fb-5792-4d93-9ad5-000000000041', 'NM', 'USA', '{""de"":""New Mexico"",""en"":""New Mexico"",""fr"":""Nouveau-Mexique"",""it"":""New Mexico""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000042', 'NY', 'USA', '{""de"":""New York"",""en"":""New York"",""fr"":""New York"",""it"":""New York""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000043', 'NC', 'USA', '{""de"":""North Carolina"",""en"":""North Carolina"",""fr"":""Caroline du Nord"",""it"":""Carolina del Nord""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000044', 'ND', 'USA', '{""de"":""North Dakota"",""en"":""North Dakota"",""fr"":""Dakota du Nord"",""it"":""Dakota del Nord""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000045', 'OH', 'USA', '{""de"":""Ohio"",""en"":""Ohio"",""fr"":""Ohio"",""it"":""Ohio""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000046', 'OK', 'USA', '{""de"":""Oklahoma"",""en"":""Oklahoma"",""fr"":""Oklahoma"",""it"":""Oklahoma""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000047', 'OR', 'USA', '{""de"":""Oregon"",""en"":""Oregon"",""fr"":""Oregon"",""it"":""Oregon""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000048', 'PA', 'USA', '{""de"":""Pennsylvania"",""en"":""Pennsylvania"",""fr"":""Pennsylvanie"",""it"":""Pennsylvania""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000049', 'RI', 'USA', '{""de"":""Rhode Island"",""en"":""Rhode Island"",""fr"":""Rhode Island"",""it"":""Rhode Island""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000050', 'SC', 'USA', '{""de"":""South Carolina"",""en"":""South Carolina"",""fr"":""Caroline du Sud"",""it"":""Carolina del Sud""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000051', 'SD', 'USA', '{""de"":""South Dakota"",""en"":""South Dakota"",""fr"":""Dakota du Sud"",""it"":""Dakota del Sud""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000052', 'TN', 'USA', '{""de"":""Tennessee"",""en"":""Tennessee"",""fr"":""Tennessee"",""it"":""Tennessee""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000053', 'TX', 'USA', '{""de"":""Texas"",""en"":""Texas"",""fr"":""Texas"",""it"":""Texas""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000054', 'UT', 'USA', '{""de"":""Utah"",""en"":""Utah"",""fr"":""Utah"",""it"":""Utah""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000055', 'VT', 'USA', '{""de"":""Vermont"",""en"":""Vermont"",""fr"":""Vermont"",""it"":""Vermont""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000056', 'VA', 'USA', '{""de"":""Virginia"",""en"":""Virginia"",""fr"":""Virginie"",""it"":""Virginia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000057', 'WA', 'USA', '{""de"":""Washington"",""en"":""Washington"",""fr"":""Washington"",""it"":""Washington""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000058', 'WV', 'USA', '{""de"":""West Virginia"",""en"":""West Virginia"",""fr"":""Virginie-Occidentale"",""it"":""West Virginia""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000059', 'WI', 'USA', '{""de"":""Wisconsin"",""en"":""Wisconsin"",""fr"":""Wisconsin"",""it"":""Wisconsin""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('cf9361fb-5792-4d93-9ad5-000000000060', 'WY', 'USA', '{""de"":""Wyoming"",""en"":""Wyoming"",""fr"":""Wyoming"",""it"":""Wyoming""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000002', '02', 'FR', '{""de"":""Aisne"",""en"":""Aisne"",""fr"":""Aisne"",""it"":""Aisne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000003', '03', 'FR', '{""de"":""Allier"",""en"":""Allier"",""fr"":""Allier"",""it"":""Allier""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000004', '04', 'FR', '{""de"":""Alpes-de-Haute-Provence"",""en"":""Alpes-de-Haute-Provence"",""fr"":""Alpes-de-Haute-Provence"",""it"":""Alpes-de-Haute-Provence""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000005', '05', 'FR', '{""de"":""Hautes-Alpes"",""en"":""Hautes-Alpes"",""fr"":""Hautes-Alpes"",""it"":""Hautes-Alpes""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000006', '06', 'FR', '{""de"":""Alpes-Maritimes"",""en"":""Alpes-Maritimes"",""fr"":""Alpes-Maritimes"",""it"":""Alpes-Maritimes""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000007', '07', 'FR', '{""de"":""Ardèche"",""en"":""Ardèche"",""fr"":""Ardèche"",""it"":""Ardèche""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000008', '08', 'FR', '{""de"":""Ardennes"",""en"":""Ardennes"",""fr"":""Ardennes"",""it"":""Ardennes""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000009', '09', 'FR', '{""de"":""Ariège"",""en"":""Ariège"",""fr"":""Ariège"",""it"":""Ariège""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000010', '10', 'FR', '{""de"":""Aube"",""en"":""Aube"",""fr"":""Aube"",""it"":""Aube""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000011', '11', 'FR', '{""de"":""Aude"",""en"":""Aude"",""fr"":""Aude"",""it"":""Aude""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000012', '12', 'FR', '{""de"":""Aveyron"",""en"":""Aveyron"",""fr"":""Aveyron"",""it"":""Aveyron""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000013', '13', 'FR', '{""de"":""Bouches-du-Rhône"",""en"":""Bouches-du-Rhône"",""fr"":""Bouches-du-Rhône"",""it"":""Bouches-du-Rhône""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000014', '14', 'FR', '{""de"":""Calvados"",""en"":""Calvados"",""fr"":""Calvados"",""it"":""Calvados""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000015', '15', 'FR', '{""de"":""Cantal"",""en"":""Cantal"",""fr"":""Cantal"",""it"":""Cantal""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000016', '16', 'FR', '{""de"":""Charente"",""en"":""Charente"",""fr"":""Charente"",""it"":""Charente""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000017', '17', 'FR', '{""de"":""Charente-Maritime"",""en"":""Charente-Maritime"",""fr"":""Charente-Maritime"",""it"":""Charente-Maritime""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000018', '18', 'FR', '{""de"":""Cher"",""en"":""Cher"",""fr"":""Cher"",""it"":""Cher""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000019', '19', 'FR', '{""de"":""Corrèze"",""en"":""Corrèze"",""fr"":""Corrèze"",""it"":""Corrèze""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000020', '2A', 'FR', '{""de"":""Corse-du-Sud"",""en"":""Corse-du-Sud"",""fr"":""Corse-du-Sud"",""it"":""Corse-du-Sud""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000021', '2B', 'FR', '{""de"":""Haute-Corse"",""en"":""Haute-Corse"",""fr"":""Haute-Corse"",""it"":""Haute-Corse""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000022', '21', 'FR', '{""de"":""Côte-d''Or"",""en"":""Côte-d''Or"",""fr"":""Côte-d''Or"",""it"":""Côte-d''Or""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000023', '22', 'FR', '{""de"":""Côtes-d''Armor"",""en"":""Côtes-d''Armor"",""fr"":""Côtes-d''Armor"",""it"":""Côtes-d''Armor""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000024', '23', 'FR', '{""de"":""Creuse"",""en"":""Creuse"",""fr"":""Creuse"",""it"":""Creuse""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000025', '24', 'FR', '{""de"":""Dordogne"",""en"":""Dordogne"",""fr"":""Dordogne"",""it"":""Dordogne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000026', '25', 'FR', '{""de"":""Doubs"",""en"":""Doubs"",""fr"":""Doubs"",""it"":""Doubs""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000027', '26', 'FR', '{""de"":""Drôme"",""en"":""Drôme"",""fr"":""Drôme"",""it"":""Drôme""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000028', '27', 'FR', '{""de"":""Eure"",""en"":""Eure"",""fr"":""Eure"",""it"":""Eure""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000029', '28', 'FR', '{""de"":""Eure-et-Loir"",""en"":""Eure-et-Loir"",""fr"":""Eure-et-Loir"",""it"":""Eure-et-Loir""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000030', '29', 'FR', '{""de"":""Finistère"",""en"":""Finistère"",""fr"":""Finistère"",""it"":""Finistère""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000031', '30', 'FR', '{""de"":""Gard"",""en"":""Gard"",""fr"":""Gard"",""it"":""Gard""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL);
INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000032', '31', 'FR', '{""de"":""Haute-Garonne"",""en"":""Haute-Garonne"",""fr"":""Haute-Garonne"",""it"":""Haute-Garonne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000033', '32', 'FR', '{""de"":""Gers"",""en"":""Gers"",""fr"":""Gers"",""it"":""Gers""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000034', '33', 'FR', '{""de"":""Gironde"",""en"":""Gironde"",""fr"":""Gironde"",""it"":""Gironde""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000035', '34', 'FR', '{""de"":""Hérault"",""en"":""Hérault"",""fr"":""Hérault"",""it"":""Hérault""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000036', '35', 'FR', '{""de"":""Ille-et-Vilaine"",""en"":""Ille-et-Vilaine"",""fr"":""Ille-et-Vilaine"",""it"":""Ille-et-Vilaine""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000037', '36', 'FR', '{""de"":""Indre"",""en"":""Indre"",""fr"":""Indre"",""it"":""Indre""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000038', '37', 'FR', '{""de"":""Indre-et-Loire"",""en"":""Indre-et-Loire"",""fr"":""Indre-et-Loire"",""it"":""Indre-et-Loire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000039', '38', 'FR', '{""de"":""Isère"",""en"":""Isère"",""fr"":""Isère"",""it"":""Isère""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000040', '39', 'FR', '{""de"":""Jura"",""en"":""Jura"",""fr"":""Jura"",""it"":""Jura""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000041', '40', 'FR', '{""de"":""Landes"",""en"":""Landes"",""fr"":""Landes"",""it"":""Landes""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000042', '41', 'FR', '{""de"":""Loir-et-Cher"",""en"":""Loir-et-Cher"",""fr"":""Loir-et-Cher"",""it"":""Loir-et-Cher""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000043', '42', 'FR', '{""de"":""Loire"",""en"":""Loire"",""fr"":""Loire"",""it"":""Loire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000044', '43', 'FR', '{""de"":""Haute-Loire"",""en"":""Haute-Loire"",""fr"":""Haute-Loire"",""it"":""Haute-Loire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000045', '44', 'FR', '{""de"":""Loire-Atlantique"",""en"":""Loire-Atlantique"",""fr"":""Loire-Atlantique"",""it"":""Loire-Atlantique""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000046', '45', 'FR', '{""de"":""Loiret"",""en"":""Loiret"",""fr"":""Loiret"",""it"":""Loiret""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000047', '46', 'FR', '{""de"":""Lot"",""en"":""Lot"",""fr"":""Lot"",""it"":""Lot""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000048', '47', 'FR', '{""de"":""Lot-et-Garonne"",""en"":""Lot-et-Garonne"",""fr"":""Lot-et-Garonne"",""it"":""Lot-et-Garonne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000049', '48', 'FR', '{""de"":""Lozère"",""en"":""Lozère"",""fr"":""Lozère"",""it"":""Lozère""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000050', '49', 'FR', '{""de"":""Maine-et-Loire"",""en"":""Maine-et-Loire"",""fr"":""Maine-et-Loire"",""it"":""Maine-et-Loire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000051', '50', 'FR', '{""de"":""Manche"",""en"":""Manche"",""fr"":""Manche"",""it"":""Manche""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000052', '51', 'FR', '{""de"":""Marne"",""en"":""Marne"",""fr"":""Marne"",""it"":""Marne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000053', '52', 'FR', '{""de"":""Haute-Marne"",""en"":""Haute-Marne"",""fr"":""Haute-Marne"",""it"":""Haute-Marne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000054', '53', 'FR', '{""de"":""Mayenne"",""en"":""Mayenne"",""fr"":""Mayenne"",""it"":""Mayenne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000055', '54', 'FR', '{""de"":""Meurthe-et-Moselle"",""en"":""Meurthe-et-Moselle"",""fr"":""Meurthe-et-Moselle"",""it"":""Meurthe-et-Moselle""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000056', '55', 'FR', '{""de"":""Meuse"",""en"":""Meuse"",""fr"":""Meuse"",""it"":""Meuse""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000057', '56', 'FR', '{""de"":""Morbihan"",""en"":""Morbihan"",""fr"":""Morbihan"",""it"":""Morbihan""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000058', '57', 'FR', '{""de"":""Moselle"",""en"":""Moselle"",""fr"":""Moselle"",""it"":""Moselle""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000059', '58', 'FR', '{""de"":""Nièvre"",""en"":""Nièvre"",""fr"":""Nièvre"",""it"":""Nièvre""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000060', '59', 'FR', '{""de"":""Nord"",""en"":""Nord"",""fr"":""Nord"",""it"":""Nord""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000061', '60', 'FR', '{""de"":""Oise"",""en"":""Oise"",""fr"":""Oise"",""it"":""Oise""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000062', '61', 'FR', '{""de"":""Orne"",""en"":""Orne"",""fr"":""Orne"",""it"":""Orne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000063', '62', 'FR', '{""de"":""Pas-de-Calais"",""en"":""Pas-de-Calais"",""fr"":""Pas-de-Calais"",""it"":""Pas-de-Calais""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000064', '63', 'FR', '{""de"":""Puy-de-Dôme"",""en"":""Puy-de-Dôme"",""fr"":""Puy-de-Dôme"",""it"":""Puy-de-Dôme""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000065', '64', 'FR', '{""de"":""Pyrénées-Atlantiques"",""en"":""Pyrénées-Atlantiques"",""fr"":""Pyrénées-Atlantiques"",""it"":""Pyrénées-Atlantiques""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000066', '65', 'FR', '{""de"":""Hautes-Pyrénées"",""en"":""Hautes-Pyrénées"",""fr"":""Hautes-Pyrénées"",""it"":""Hautes-Pyrénées""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000067', '66', 'FR', '{""de"":""Pyrénées-Orientales"",""en"":""Pyrénées-Orientales"",""fr"":""Pyrénées-Orientales"",""it"":""Pyrénées-Orientales""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000068', '67', 'FR', '{""de"":""Bas-Rhin"",""en"":""Bas-Rhin"",""fr"":""Bas-Rhin"",""it"":""Bas-Rhin""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000069', '68', 'FR', '{""de"":""Haut-Rhin"",""en"":""Haut-Rhin"",""fr"":""Haut-Rhin"",""it"":""Haut-Rhin""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000070', '69', 'FR', '{""de"":""Rhône"",""en"":""Rhône"",""fr"":""Rhône"",""it"":""Rhône""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000071', '70', 'FR', '{""de"":""Haute-Saône"",""en"":""Haute-Saône"",""fr"":""Haute-Saône"",""it"":""Haute-Saône""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000072', '71', 'FR', '{""de"":""Saône-et-Loire"",""en"":""Saône-et-Loire"",""fr"":""Saône-et-Loire"",""it"":""Saône-et-Loire""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000073', '72', 'FR', '{""de"":""Sarthe"",""en"":""Sarthe"",""fr"":""Sarthe"",""it"":""Sarthe""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000074', '73', 'FR', '{""de"":""Savoie"",""en"":""Savoie"",""fr"":""Savoie"",""it"":""Savoie""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000075', '74', 'FR', '{""de"":""Haute-Savoie"",""en"":""Haute-Savoie"",""fr"":""Haute-Savoie"",""it"":""Haute-Savoie""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000076', '75', 'FR', '{""de"":""Paris"",""en"":""Paris"",""fr"":""Paris"",""it"":""Paris""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000077', '76', 'FR', '{""de"":""Seine-Maritime"",""en"":""Seine-Maritime"",""fr"":""Seine-Maritime"",""it"":""Seine-Maritime""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000078', '77', 'FR', '{""de"":""Seine-et-Marne"",""en"":""Seine-et-Marne"",""fr"":""Seine-et-Marne"",""it"":""Seine-et-Marne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000079', '78', 'FR', '{""de"":""Yvelines"",""en"":""Yvelines"",""fr"":""Yvelines"",""it"":""Yvelines""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000080', '79', 'FR', '{""de"":""Deux-Sèvres"",""en"":""Deux-Sèvres"",""fr"":""Deux-Sèvres"",""it"":""Deux-Sèvres""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000081', '80', 'FR', '{""de"":""Somme"",""en"":""Somme"",""fr"":""Somme"",""it"":""Somme""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL);
INSERT INTO public.state (id, abbreviation, country_prefix, name, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000082', '81', 'FR', '{""de"":""Tarn"",""en"":""Tarn"",""fr"":""Tarn"",""it"":""Tarn""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000083', '82', 'FR', '{""de"":""Tarn-et-Garonne"",""en"":""Tarn-et-Garonne"",""fr"":""Tarn-et-Garonne"",""it"":""Tarn-et-Garonne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000084', '83', 'FR', '{""de"":""Var"",""en"":""Var"",""fr"":""Var"",""it"":""Var""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000085', '84', 'FR', '{""de"":""Vaucluse"",""en"":""Vaucluse"",""fr"":""Vaucluse"",""it"":""Vaucluse""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000086', '85', 'FR', '{""de"":""Vendée"",""en"":""Vendée"",""fr"":""Vendée"",""it"":""Vendée""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000087', '86', 'FR', '{""de"":""Vienne"",""en"":""Vienne"",""fr"":""Vienne"",""it"":""Vienne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000088', '87', 'FR', '{""de"":""Haute-Vienne"",""en"":""Haute-Vienne"",""fr"":""Haute-Vienne"",""it"":""Haute-Vienne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000089', '88', 'FR', '{""de"":""Vosges"",""en"":""Vosges"",""fr"":""Vosges"",""it"":""Vosges""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000090', '89', 'FR', '{""de"":""Yonne"",""en"":""Yonne"",""fr"":""Yonne"",""it"":""Yonne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000091', '90', 'FR', '{""de"":""Territoire de Belfort"",""en"":""Territoire de Belfort"",""fr"":""Territoire de Belfort"",""it"":""Territoire de Belfort""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000092', '91', 'FR', '{""de"":""Essonne"",""en"":""Essonne"",""fr"":""Essonne"",""it"":""Essonne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000093', '92', 'FR', '{""de"":""Hauts-de-Seine"",""en"":""Hauts-de-Seine"",""fr"":""Hauts-de-Seine"",""it"":""Hauts-de-Seine""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000094', '93', 'FR', '{""de"":""Seine-Saint-Denis"",""en"":""Seine-Saint-Denis"",""fr"":""Seine-Saint-Denis"",""it"":""Seine-Saint-Denis""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000095', '94', 'FR', '{""de"":""Val-de-Marne"",""en"":""Val-de-Marne"",""fr"":""Val-de-Marne"",""it"":""Val-de-Marne""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000096', '95', 'FR', '{""de"":""Val-d''Oise"",""en"":""Val-d''Oise"",""fr"":""Val-d''Oise"",""it"":""Val-d''Oise""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000097', '971', 'FR', '{""de"":""Guadeloupe"",""en"":""Guadeloupe"",""fr"":""Guadeloupe"",""it"":""Guadeloupe""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000098', '972', 'FR', '{""de"":""Martinique"",""en"":""Martinique"",""fr"":""Martinique"",""it"":""Martinique""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000099', '973', 'FR', '{""de"":""Guyane"",""en"":""Guyane"",""fr"":""Guyane"",""it"":""Guyane""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000100', '974', 'FR', '{""de"":""La Réunion"",""en"":""La Réunion"",""fr"":""La Réunion"",""it"":""La Réunion""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL),
                 ('d6ae2a47-8e65-4ccd-bbe9-000000000101', '976', 'FR', '{""de"":""Mayotte"",""en"":""Mayotte"",""fr"":""Mayotte"",""it"":""Mayotte""}'::jsonb, NULL, NULL, NULL, NULL, NULL, false, NULL);"

            );
        }
    }
}
