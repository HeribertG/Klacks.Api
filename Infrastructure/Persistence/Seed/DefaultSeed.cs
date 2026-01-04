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
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c69"), "soRate", "0.1" }
                });

            migrationBuilder.Sql(
                @"INSERT INTO ""AspNetUserRoles"" (role_id, user_id) VALUES 
                ('e32d7319-6861-4c9a-b096-08a77088cadd', '672f77e8-e479-4422-8781-84d218377fb3')
                ON CONFLICT (role_id, user_id) DO NOTHING;"
            );

            migrationBuilder.Sql(
                                  @"INSERT INTO public.countries (id,abbreviation,name_de,name_en,name_fr,name_it,prefix,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	                             ('c6cdb912-022d-47bf-baf0-49a80d24948c','CH','Schweiz','Switzerland','Suisse','Svizzera','+41','2023-06-10 11:57:38.449','admin',NULL,'',NULL,false,''),
	                             ('1aa91576-f08e-4498-b47b-60d09aa2b614','DE','Deutschland','Germany','Allemagne','Germania','+49','2023-06-10 12:00:25.999','admin',NULL,'',NULL,false,''),
	                             ('59c98d2e-0634-489b-8e44-a6f7d5532b6b','AT','Österreich','Austria','Autriche','Austria','+43','2023-06-10 13:00:39.491','admin',NULL,'',NULL,false,''),
	                             ('11046a85-dfc0-48c8-878b-53b724b9c0d9','LI','Fürstentum Liechtenstein','Principality of Liechtenstein','Principauté de Liechtenstein','Principato del Liechtenstein','+423','2023-06-10 13:26:04.182','admin',NULL,'',NULL,false,''),
	                             ('276e0392-bfa3-4230-b8a7-8e9fdfecad57','USA','Vereinigte Staaten von Amerika','United States of America','États-Unis d''Amérique','Stati Uniti d''America','+1','2023-07-10 07:17:54.545','admin',NULL,'',NULL,false,''),
	                             ('d8084ef6-df46-46ea-a71e-a708898d1650','IT','Italien','Italy','Italie','Italia','+39','2023-07-10 07:14:42.454','admin',NULL,'',NULL,false,''),
                                 ('2d576ac5-7978-4e3c-b92b-e3d67493aecb','FR','Fankreich','France','France','Francia','+33','2023-07-10 07:14:42.454','admin',NULL,'',NULL,false,'')"
            );

            migrationBuilder.Sql(
              @"INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('57447b52-1f3e-4d62-a4b3-93c9c72b7ce6','AG','CH','Aargau','Aargau','Argovie','Argovia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('5c1921ad-8b6c-4374-952d-3e9899d2a663','AI','CH','Appenzell Innerrhoden','Appenzell Innerrhoden','Appenzell Rhodes-Intérieures','Appenzello Interno',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('72a92755-95ff-48d5-963f-7681e26888a0','AR','CH','Appenzell Ausserrhoden','Appenzell Ausserrhoden','Appenzell Rhodes-Extérieures','Appenzello Esterno',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('8d0ea829-b098-4cde-bf78-53f301d692eb','BE','CH','Bern','Bern','Berne','Berna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('db4177d6-09bc-4c1a-89ae-7e0c3b549c32','BL','CH','Basel-Landschaft','Basel-Landschaft','Bâle-Campagne','Basilea Campagna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('c37d6794-682b-47c2-8d9c-59a78823a9a9','BS','CH','Basel-Stadt','Basel-Stadt','Bâle-Ville','Basilea Città',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cb5efcaa-6524-4d84-96ad-b44029315514','FR','CH','Freiburg','Fribourg','Fribourg','Friburgo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('8c5f8f1c-f0f7-4e6b-9f5f-31f869bb53a2','GE','CH','Genf','Geneva','Genève','Ginevra',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('6e6d9834-7559-4189-9d63-68d4a692d6cc','GL','CH','Glarus','Glarus','Glaris','Glarona',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('ab1b707b-419e-4bcf-9f36-2cc9e28ea82f','GR','CH','Graubünden','Graubünden','Grisons','Grigioni',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('8238ab69-c7d0-4e97-9763-5c052f748660','JU','CH','Jura','Jura','Jura','Giura',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('ee5508b7-3709-4cfd-91e7-9ee4e532d876','LU','CH','Luzern','Lucerne','Lucerne','Lucerna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('b0b2e204-08f7-44d7-b7ef-5f330ff7a063','NE','CH','Neuenburg','Neuchâtel','Neuchâtel','Neuchâtel',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('f7a01dc0-14c6-43d6-bf23-3a2ed0c22b44','NW','CH','Nidwalden','Nidwalden','Nidwald','Nidvaldo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('c116d12d-0d2d-4416-b0a3-fd6f69933b47','OW','CH','Obwalden','Obwalden','Obwald','Obvaldo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('82661c57-c6f1-4af3-8a8f-18e6e09783c9','SG','CH','St. Gallen','St. Gallen','Saint-Gall','San Gallo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('758ed70b-7d6f-44e9-a1cc-021677af7e72','SH','CH','Schaffhausen','Schaffhausen','Schaffhouse','Sciaffusa',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('8054ad64-7e2c-4963-9ae4-8e3e468bbae1','SO','CH','Solothurn','Solothurn','Soleure','Soletta',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('c6ea12c0-7a7f-43f4-a38b-73e35d3b1e9c','SZ','CH','Schwyz','Schwyz','Schwytz','Svitto',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('28b58797-7f7a-4d78-a4e7-cc1e411ac6f6','TG','CH','Thurgau','Thurgau','Thurgovie','Turgovia',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('0ff741f7-4020-4a03-8906-bd0efae81f1b','TI','CH','Tessin','Ticino','Ticino','Ticino',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('65b8bfc6-826b-4a51-98de-34f3506a3f3e','UR','CH','Uri','Uri','Uri','Uri',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('1836b680-3f1c-4310-8368-726fd0f0d15f','VD','CH','Waadt','Vaud','Vaud','Vaud',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('93ab02ce-9a3f-4ab7-b08c-90a6b6181fda','VS','CH','Wallis','Valais','Valais','Vallese',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('853e28a6-1571-416e-b300-90c6a55fe9ee','ZG','CH','Zug','Zug','Zoug','Zugo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('e972c351-ee41-4007-9e9e-b85d8a2f620f','ZH','CH','Zürich','Zurich','Zurich','Zurigo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('7ff80e52-b081-4da9-9d75-0dcb1ba5f1c8','BW','DE','Baden-Württemberg','Baden-Württemberg','Bade-Wurtemberg','Baden-Württemberg',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('7f38ca0d-f31a-4324-9419-1103d24cc2b6','BY','DE','Bayern','Bavaria','Bavière','Baviera',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cfb93e71-fc19-438c-b274-f89ccb9406ca','BE','DE','Berlin','Berlin','Berlin','Berlino',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('5e819994-0b0e-40be-a764-d56ea9b8c5d5','BB','DE','Brandenburg','Brandenburg','Brandebourg','Brandeburgo',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('859e0595-9e35-458d-abbf-5c144d51441b','HB','DE','Bremen','Bremen','Brême','Brema',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('2e98f130-837e-431d-b1f9-48e6ec884202','HH','DE','Hamburg','Hamburg','Hambourg','Amburgo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('90946750-fac5-49c5-85d6-3f9eda87e90e','HE','DE','Hessen','Hesse','Hesse','Assia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('bdb79500-e320-4228-ae57-64ae9b68f185','MV','DE','Mecklenburg-Vorpommern','Mecklenburg-Vorpommern','Mecklembourg-Poméranie-Occidentale','Meclemburgo-Pomerania Anteriore',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('1b8e617e-6ad3-4114-8098-b39737352b92','NI','DE','Niedersachsen','Lower Saxony','Basse-Saxe','Bassa Sassonia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('a2e19e83-4533-4fb3-b295-574623ce326e','NW','DE','Nordrhein-Westfalen','North Rhine-Westphalia','Rhénanie-du-Nord-Westphalie','Renania Settentrionale-Vestfalia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('bd78d91c-bed3-4da2-b473-94cfe2fdd2a0','RP','DE','Rheinland-Pfalz','Rhineland-Palatinate','Rhénanie-Palatinat','Renania-Palatinato',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('be66c70d-748d-4547-b420-e46d4418d04f','SL','DE','Saarland','Saarland','Sarre','Saarland',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('91070049-b278-44a4-b90c-4cd6d7a2c5f5','SN','DE','Sachsen','Saxony','Saxe','Sassonia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('82cbf0e0-41ca-47fa-b3dc-115bf10cfba9','ST','DE','Sachsen-Anhalt','Saxony-Anhalt','Saxe-Anhalt','Sassonia-Anhalt',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('675839ca-af48-4a3c-9704-6f161f4b26f3','SH','DE','Schleswig-Holstein','Schleswig-Holstein','Schleswig-Holstein','Schleswig-Holstein',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('9dcdecc4-8e20-41b5-aba1-8bf71e62b024','TH','DE','Thüringen','Thuringia','Thuringe','Turingia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('32b2cb96-e1e1-46dd-b32e-af79329fca93','BG','AT','Burgenland','Burgenland','Burgenland','Burgenland',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('9357259a-b2cc-455a-a3e1-e8fde23c4de5','K','AT','Kärnten','Carinthia','Carinthie','Carinzia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('805cb509-17a3-4eb6-ad50-e1004582f144','NÖ','AT','Niederösterreich','Lower Austria','Basse-Autriche','Bassa Austria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('89a6d230-37b0-40f0-a405-b87528ff0aef','OÖ','AT','Oberösterreich','Upper Austria','Haute-Autriche','Alta Austria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('7eeef546-e9e2-4643-85de-99ff66b81e90','S','AT','Salzburg','Salzburg','Salzbourg','Salisburgo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('55549cf8-9261-4f6e-bbac-ef09ec1022c7','ST','AT','Steiermark','Styria','Styrie','Stiria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('f3a3a47a-cf48-4f88-97e2-7446c7260e83','T','AT','Tirol','Tyrol','Tyrol','Tirolo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('aa1b595c-f4cd-4b86-bf9d-b39a98fad16b','V','AT','Vorarlberg','Vorarlberg','Vorarlberg','Vorarlberg',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('88cbf7ed-70fc-4fe2-b76b-00b132e3c068','W','AT','Wien','Vienna','Vienne','Vienna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('63c26057-f6b9-45c8-8019-5fb535ec2934','ABR','IT','Abruzzen','Abruzzo','Abruzzes','Abruzzo',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('813fdcee-ddd8-4f52-9abe-58b2f97bcfbc','BAS','IT','Basilikata','Basilicata','Basilicate','Basilicata',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('7f7237f8-eafa-4295-beca-a476d5b6e20f','CAL','IT','Kalabrien','Calabria','Calabre','Calabria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('ab09134a-a88a-4a4b-995d-b2dd0f435839','CAM','IT','Kampanien','Campania','Campanie','Campania',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('edc1374f-5212-4c2d-a381-b970ca87fc24','EMR','IT','Emilia-Romagna','Emilia-Romagna','Émilie-Romagne','Emilia-Romagna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('2a81ac95-6627-4373-9661-2711cd71da77','FVG','IT','Friaul-Julisch Venetien','Friuli-Venezia Giulia','Frioul-Vénétie julienne','Friuli-Venezia Giulia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('f9ea7684-e5f8-4cfa-a5fd-4bd3bfb808e3','LAZ','IT','Latium','Lazio','Latium','Lazio',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('26d97b9a-5908-4def-9027-9867d635bae6','LIG','IT','Ligurien','Liguria','Ligurie','Liguria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('237fd478-5178-47af-bc0a-057d5fa4f09e','LOM','IT','Lombardei','Lombardy','Lombardie','Lombardia',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('c3490327-b4d0-4865-9145-16d0487b765d','MAR','IT','Marken','Marche','Marches','Marche',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('4672cc43-2762-4eb4-9280-93081c7ee822','MOL','IT','Molise','Molise','Molise','Molise',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('1bcb825c-41ce-44c0-b929-c1e44bfa3436','PAB','IT','Piemont','Piedmont','Piemont','Piemonte',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('04dfe7d9-7601-4219-969b-17492cd02397','PAT','IT','Apulien','Apulia','Pouilles','Puglia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('bc15118b-4f09-47e7-a43c-8d701d56ba01','SAR','IT','Sardinien','Sardinia','Sardaigne','Sardegna',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('e8e57d91-8a59-4ca6-870f-cf81a7333d83','SIC','IT','Sizilien','Sicily','Sicile','Sicilia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('ad028558-4d5a-4645-8740-bb57da97cc71','TOS','IT','Toskana','Tuscany','Toscane','Toscana',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('a6864072-82a9-42ba-83b9-d1e5a2cce840','TAA','IT','Trentino-Südtirol','Trentino-Alto Adige','Trentin-Haut-Adige','Trentino-Alto Adige',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('11771795-8cb1-4152-9c09-a40c4ad05fb8','UMB','IT','Umbrien','Umbria','Ombrie','Umbria',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('07d0f694-34fd-4a19-a5f3-da4bad46c4fd','VEN','IT','Venetien','Veneto','Vénétie','Veneto',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('cf9361fb-5792-4d93-9ad5-000000000001','AL','USA','Alabama','Alabama','Alabama','Alabama',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000002','AK','USA','Alaska','Alaska','Alaska','Alaska',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000003','AZ','USA','Arizona','Arizona','Arizona','Arizona',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000004','AR','USA','Arkansas','Arkansas','Arkansas','Arkansas',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000005','CA','USA','California','California','Californie','California',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000006','CO','USA','Colorado','Colorado','Colorado','Colorado',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000007','CT','USA','Connecticut','Connecticut','Connecticut','Connecticut',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000008','DE','USA','Delaware','Delaware','Delaware','Delaware',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000009','FL','USA','Florida','Florida','Floride','Florida',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000010','GA','USA','Georgia','Georgia','Géorgie','Georgia',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('cf9361fb-5792-4d93-9ad5-000000000011','HI','USA','Hawaii','Hawaii','Hawaï','Hawaii',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000012','ID','USA','Idaho','Idaho','Idaho','Idaho',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000013','IL','USA','Illinois','Illinois','Illinois','Illinois',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000014','IN','USA','Indiana','Indiana','Indiana','Indiana',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000015','IA','USA','Iowa','Iowa','Iowa','Iowa',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000016','KS','USA','Kansas','Kansas','Kansas','Kansas',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000017','KY','USA','Kentucky','Kentucky','Kentucky','Kentucky',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000018','LA','USA','Louisiana','Louisiana','Louisiane','Louisiana',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000019','ME','USA','Maine','Maine','Maine','Maine',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000020','MD','USA','Maryland','Maryland','Maryland','Maryland',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('cf9361fb-5792-4d93-9ad5-000000000021','MA','USA','Massachusetts','Massachusetts','Massachusetts','Massachusetts',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000022','MI','USA','Michigan','Michigan','Michigan','Michigan',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000023','MN','USA','Minnesota','Minnesota','Minnesota','Minnesota',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000024','MS','USA','Mississippi','Mississippi','Mississippi','Mississippi',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000025','MO','USA','Missouri','Missouri','Missouri','Missouri',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000026','MT','USA','Montana','Montana','Montana','Montana',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000027','NE','USA','Nebraska','Nebraska','Nebraska','Nebraska',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000028','NV','USA','Nevada','Nevada','Nevada','Nevada',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000029','NH','USA','New Hampshire','New Hampshire','New Hampshire','New Hampshire',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000030','NJ','USA','New Jersey','New Jersey','New Jersey','New Jersey',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('cf9361fb-5792-4d93-9ad5-000000000041','NM','USA','New Mexico','New Mexico','Nouveau-Mexique','New Mexico',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000042','NY','USA','New York','New York','New York','New York',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000043','NC','USA','North Carolina','North Carolina','Caroline du Nord','Carolina del Nord',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000044','ND','USA','North Dakota','North Dakota','Dakota du Nord','Dakota del Nord',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000045','OH','USA','Ohio','Ohio','Ohio','Ohio',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000046','OK','USA','Oklahoma','Oklahoma','Oklahoma','Oklahoma',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000047','OR','USA','Oregon','Oregon','Oregon','Oregon',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000048','PA','USA','Pennsylvania','Pennsylvania','Pennsylvanie','Pennsylvania',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000049','RI','USA','Rhode Island','Rhode Island','Rhode Island','Rhode Island',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000050','SC','USA','South Carolina','South Carolina','Caroline du Sud','Carolina del Sud',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state (id,abbreviation,country_prefix,name_de,name_en,name_fr,name_it,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
	 ('cf9361fb-5792-4d93-9ad5-000000000051','SD','USA','South Dakota','South Dakota','Dakota du Sud','Dakota del Sud',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000052','TN','USA','Tennessee','Tennessee','Tennessee','Tennessee',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000053','TX','USA','Texas','Texas','Texas','Texas',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000054','UT','USA','Utah','Utah','Utah','Utah',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000055','VT','USA','Vermont','Vermont','Vermont','Vermont',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000056','VA','USA','Virginia','Virginia','Virginie','Virginia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000057','WA','USA','Washington','Washington','Washington','Washington',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000058','WV','USA','West Virginia','West Virginia','Virginie-Occidentale','West Virginia',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000059','WI','USA','Wisconsin','Wisconsin','Wisconsin','Wisconsin',NULL,NULL,NULL,NULL,NULL,false,NULL),
	 ('cf9361fb-5792-4d93-9ad5-000000000060','WY','USA','Wyoming','Wyoming','Wyoming','Wyoming',NULL,NULL,NULL,NULL,NULL,false,NULL);
INSERT INTO public.state(id, abbreviation, country_prefix, name_de, name_en, name_fr, name_it, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted) VALUES
   ('d6ae2a47-8e65-4ccd-bbe9-000000000001','01', 'FR', 'Ain', 'Ain', 'Ain', 'Ain', NULL, NULL, NULL, NULL, NULL,false, NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000002','02', 'FR', 'Aisne', 'Aisne', 'Aisne', 'Aisne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000003','03', 'FR', 'Allier', 'Allier', 'Allier', 'Allier',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000004','04', 'FR', 'Alpes-de-Haute-Provence', 'Alpes-de-Haute-Provence', 'Alpes-de-Haute-Provence', 'Alpes-de-Haute-Provence',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000005','05', 'FR', 'Hautes-Alpes', 'Hautes-Alpes', 'Hautes-Alpes', 'Hautes-Alpes',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000006','06', 'FR', 'Alpes-Maritimes', 'Alpes-Maritimes', 'Alpes-Maritimes', 'Alpes-Maritimes',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000007','07', 'FR', 'Ardèche', 'Ardèche', 'Ardèche', 'Ardèche',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000008','08', 'FR', 'Ardennes', 'Ardennes', 'Ardennes', 'Ardennes',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000009','09', 'FR', 'Ariège', 'Ariège', 'Ariège', 'Ariège',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000010','10', 'FR', 'Aube', 'Aube', 'Aube', 'Aube',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000011','11', 'FR', 'Aude', 'Aude', 'Aude', 'Aude',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000012','12', 'FR', 'Aveyron', 'Aveyron', 'Aveyron', 'Aveyron',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000013','13', 'FR', 'Bouches-du-Rhône', 'Bouches-du-Rhône', 'Bouches-du-Rhône', 'Bouches-du-Rhône',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000014','14', 'FR', 'Calvados', 'Calvados', 'Calvados', 'Calvados',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000015','15', 'FR', 'Cantal', 'Cantal', 'Cantal', 'Cantal',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000016','16', 'FR', 'Charente', 'Charente', 'Charente', 'Charente',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000017','17', 'FR', 'Charente-Maritime', 'Charente-Maritime', 'Charente-Maritime', 'Charente-Maritime',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000018','18', 'FR', 'Cher', 'Cher', 'Cher', 'Cher',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000019','19', 'FR', 'Corrèze', 'Corrèze', 'Corrèze', 'Corrèze',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000020','2A', 'FR', 'Corse-du-Sud', 'Corse-du-Sud', 'Corse-du-Sud', 'Corse-du-Sud',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000021','2B', 'FR', 'Haute-Corse', 'Haute-Corse', 'Haute-Corse', 'Haute-Corse',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000022','21', 'FR', 'Côte-d''Or', 'Côte-d''Or', 'Côte-d''Or', 'Côte-d''Or',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000023','22', 'FR', 'Côtes-d''Armor', 'Côtes-d''Armor', 'Côtes-d''Armor', 'Côtes-d''Armor',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000024','23', 'FR', 'Creuse', 'Creuse', 'Creuse', 'Creuse',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000025','24', 'FR', 'Dordogne', 'Dordogne', 'Dordogne', 'Dordogne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000026','25', 'FR', 'Doubs', 'Doubs', 'Doubs', 'Doubs',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000027','26', 'FR', 'Drôme', 'Drôme', 'Drôme', 'Drôme',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000028','27', 'FR', 'Eure', 'Eure', 'Eure', 'Eure',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000029','28', 'FR', 'Eure-et-Loir', 'Eure-et-Loir', 'Eure-et-Loir', 'Eure-et-Loir',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000030','29', 'FR', 'Finistère', 'Finistère', 'Finistère', 'Finistère',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000031','30', 'FR', 'Gard', 'Gard', 'Gard', 'Gard',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000032','31', 'FR', 'Haute-Garonne', 'Haute-Garonne', 'Haute-Garonne', 'Haute-Garonne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000033','32', 'FR', 'Gers', 'Gers', 'Gers', 'Gers',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000034','33', 'FR', 'Gironde', 'Gironde', 'Gironde', 'Gironde',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000035','34', 'FR', 'Hérault', 'Hérault', 'Hérault', 'Hérault',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000036','35', 'FR', 'Ille-et-Vilaine', 'Ille-et-Vilaine', 'Ille-et-Vilaine', 'Ille-et-Vilaine',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000037','36', 'FR', 'Indre', 'Indre', 'Indre', 'Indre',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000038','37', 'FR', 'Indre-et-Loire', 'Indre-et-Loire', 'Indre-et-Loire', 'Indre-et-Loire',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000039','38', 'FR', 'Isère', 'Isère', 'Isère', 'Isère',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000040','39', 'FR', 'Jura', 'Jura', 'Jura', 'Jura',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000041','40', 'FR', 'Landes', 'Landes', 'Landes', 'Landes',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000042','41', 'FR', 'Loir-et-Cher', 'Loir-et-Cher', 'Loir-et-Cher', 'Loir-et-Cher',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000043','42', 'FR', 'Loire', 'Loire', 'Loire', 'Loire',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000044','43', 'FR', 'Haute-Loire', 'Haute-Loire', 'Haute-Loire', 'Haute-Loire',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000045','44', 'FR', 'Loire-Atlantique', 'Loire-Atlantique', 'Loire-Atlantique', 'Loire-Atlantique',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000046','45', 'FR', 'Loiret', 'Loiret', 'Loiret', 'Loiret',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000047','46', 'FR', 'Lot', 'Lot', 'Lot', 'Lot',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000048','47', 'FR', 'Lot-et-Garonne', 'Lot-et-Garonne', 'Lot-et-Garonne', 'Lot-et-Garonne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000049','48', 'FR', 'Lozère', 'Lozère', 'Lozère', 'Lozère',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000050','49', 'FR', 'Maine-et-Loire', 'Maine-et-Loire', 'Maine-et-Loire', 'Maine-et-Loire',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000051','50', 'FR', 'Manche', 'Manche', 'Manche', 'Manche',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000052','51', 'FR', 'Marne', 'Marne', 'Marne', 'Marne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000053','52', 'FR', 'Haute-Marne', 'Haute-Marne', 'Haute-Marne', 'Haute-Marne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000054','53', 'FR', 'Mayenne', 'Mayenne', 'Mayenne', 'Mayenne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000055','54', 'FR', 'Meurthe-et-Moselle', 'Meurthe-et-Moselle', 'Meurthe-et-Moselle', 'Meurthe-et-Moselle',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000056','55', 'FR', 'Meuse', 'Meuse', 'Meuse', 'Meuse',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000057','56', 'FR', 'Morbihan', 'Morbihan', 'Morbihan', 'Morbihan',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000058','57', 'FR', 'Moselle', 'Moselle', 'Moselle', 'Moselle',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000059','58', 'FR', 'Nièvre', 'Nièvre', 'Nièvre', 'Nièvre',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000060','59', 'FR', 'Nord', 'Nord', 'Nord', 'Nord',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000061','60', 'FR', 'Oise', 'Oise', 'Oise', 'Oise',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000062','61', 'FR', 'Orne', 'Orne', 'Orne', 'Orne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000063','62', 'FR', 'Pas-de-Calais', 'Pas-de-Calais', 'Pas-de-Calais', 'Pas-de-Calais',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000064','63', 'FR', 'Puy-de-Dôme', 'Puy-de-Dôme', 'Puy-de-Dôme', 'Puy-de-Dôme',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000065','64', 'FR', 'Pyrénées-Atlantiques', 'Pyrénées-Atlantiques', 'Pyrénées-Atlantiques', 'Pyrénées-Atlantiques',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000066','65', 'FR', 'Hautes-Pyrénées', 'Hautes-Pyrénées', 'Hautes-Pyrénées', 'Hautes-Pyrénées',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000067','66', 'FR', 'Pyrénées-Orientales', 'Pyrénées-Orientales', 'Pyrénées-Orientales', 'Pyrénées-Orientales',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000068','67', 'FR', 'Bas-Rhin', 'Bas-Rhin', 'Bas-Rhin', 'Bas-Rhin',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000069','68', 'FR', 'Haut-Rhin', 'Haut-Rhin', 'Haut-Rhin', 'Haut-Rhin',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000070','69', 'FR', 'Rhône', 'Rhône', 'Rhône', 'Rhône',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000071','70', 'FR', 'Haute-Saône', 'Haute-Saône', 'Haute-Saône', 'Haute-Saône',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000072','71', 'FR', 'Saône-et-Loire', 'Saône-et-Loire', 'Saône-et-Loire', 'Saône-et-Loire',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000073','72', 'FR', 'Sarthe', 'Sarthe', 'Sarthe', 'Sarthe',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000074','73', 'FR', 'Savoie', 'Savoie', 'Savoie', 'Savoie',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000075','74', 'FR', 'Haute-Savoie', 'Haute-Savoie', 'Haute-Savoie', 'Haute-Savoie',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000076','75', 'FR', 'Paris', 'Paris', 'Paris', 'Paris',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000077','76', 'FR', 'Seine-Maritime', 'Seine-Maritime', 'Seine-Maritime', 'Seine-Maritime',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000078','77', 'FR', 'Seine-et-Marne', 'Seine-et-Marne', 'Seine-et-Marne', 'Seine-et-Marne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000079','78', 'FR', 'Yvelines', 'Yvelines', 'Yvelines', 'Yvelines',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000080','79', 'FR', 'Deux-Sèvres', 'Deux-Sèvres', 'Deux-Sèvres', 'Deux-Sèvres',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000081','80', 'FR', 'Somme', 'Somme', 'Somme', 'Somme',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000082','81', 'FR', 'Tarn', 'Tarn', 'Tarn', 'Tarn',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000083','82', 'FR', 'Tarn-et-Garonne', 'Tarn-et-Garonne', 'Tarn-et-Garonne', 'Tarn-et-Garonne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000084','83', 'FR', 'Var', 'Var', 'Var', 'Var',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000085','84', 'FR', 'Vaucluse', 'Vaucluse', 'Vaucluse', 'Vaucluse',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000086','85', 'FR', 'Vendée', 'Vendée', 'Vendée', 'Vendée',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000087','86', 'FR', 'Vienne', 'Vienne', 'Vienne', 'Vienne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000088','87', 'FR', 'Haute-Vienne', 'Haute-Vienne', 'Haute-Vienne', 'Haute-Vienne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000089','88', 'FR', 'Vosges', 'Vosges', 'Vosges', 'Vosges',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000090','89', 'FR', 'Yonne', 'Yonne', 'Yonne', 'Yonne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000091','90', 'FR', 'Territoire de Belfort', 'Territoire de Belfort', 'Territoire de Belfort', 'Territoire de Belfort',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000092','91', 'FR', 'Essonne', 'Essonne', 'Essonne', 'Essonne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000093','92', 'FR', 'Hauts-de-Seine', 'Hauts-de-Seine', 'Hauts-de-Seine', 'Hauts-de-Seine',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000094','93', 'FR', 'Seine-Saint-Denis', 'Seine-Saint-Denis', 'Seine-Saint-Denis', 'Seine-Saint-Denis',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000095','94', 'FR', 'Val-de-Marne', 'Val-de-Marne', 'Val-de-Marne', 'Val-de-Marne',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000096','95', 'FR', 'Val-d''Oise', 'Val-d''Oise', 'Val-d''Oise', 'Val-d''Oise',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000097','971', 'FR', 'Guadeloupe', 'Guadeloupe', 'Guadeloupe', 'Guadeloupe',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000098','972', 'FR', 'Martinique', 'Martinique', 'Martinique', 'Martinique',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000099','973', 'FR', 'Guyane', 'Guyane', 'Guyane', 'Guyane',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000100','974', 'FR', 'La Réunion', 'La Réunion', 'La Réunion', 'La Réunion',NULL,NULL,NULL,NULL,NULL,false,NULL),
   ('d6ae2a47-8e65-4ccd-bbe9-000000000101','976', 'FR', 'Mayotte', 'Mayotte', 'Mayotte', 'Mayotte',NULL,NULL,NULL,NULL,NULL,false,NULL);"

            );
        }
    }
}
