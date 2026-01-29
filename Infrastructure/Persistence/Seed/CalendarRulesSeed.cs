using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class CalendarRulesSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
-- =====================================================
-- NATIONALE FEIERTAGE (CH CH) - gelten für alle Kantone
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('613c22be-e39f-4a40-be5a-e1202d21678f', '01/01', '', true, true, 'CH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('682409ab-0b61-450e-84c3-309a72765de9', '08/01', '', true, true, 'CH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Bundesfeiertag"",""en"":""Swiss National Day"",""fr"":""Fête nationale"",""it"":""Festa nazionale""}'),
('f54d9dd4-5e9c-49d6-81a0-c04d9715d519', 'EASTER+39', '', true, true, 'CH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Auffahrt"",""en"":""Ascension Day"",""fr"":""Ascension"",""it"":""Ascensione""}'),
('99fb49c4-8fea-4266-8836-e2b216345470', '12/25', '', true, true, 'CH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weihnachten"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}'),
('611f688c-8f58-4e8c-a3a6-a6c54f40f874', '09/01+14+SU', '', false, false, 'CH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Eidg. Dank-, Buss- und Bettag"",""en"":""Federal Day of Thanksgiving"",""fr"":""Jeûne fédéral"",""it"":""Digiuno federale""}');

-- =====================================================
-- BERCHTOLDSTAG (01/02)
-- Kantone: AG, BE, FR, GL, JU, LU, NE, OW, SH, SO, TG, VD, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('b0102001-0001-0001-0001-000000000001', '01/02', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000002', '01/02', '', true, true, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000003', '01/02', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000004', '01/02', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000005', '01/02', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000006', '01/02', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000007', '01/02', '', true, true, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000008', '01/02', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000009', '01/02', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000010', '01/02', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000011', '01/02', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000012', '01/02', '', true, true, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}'),
('b0102001-0001-0001-0001-000000000013', '01/02', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Berchtoldstag"",""en"":""St. Berchtold''s Day"",""fr"":""Saint-Berchtold"",""it"":""San Bertoldo""}');

-- =====================================================
-- DREIKÖNIGSTAG (01/06)
-- Kantone: GR, SZ, TI, UR
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('d0106001-0001-0001-0001-000000000001', '01/06', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Dreikönigstag"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('d0106001-0001-0001-0001-000000000002', '01/06', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Dreikönigstag"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('d0106001-0001-0001-0001-000000000003', '01/06', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Dreikönigstag"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('d0106001-0001-0001-0001-000000000004', '01/06', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Dreikönigstag"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}');

-- =====================================================
-- JOSEFSTAG (03/19)
-- Kantone: GR, LU, NW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('00319001-0001-0001-0001-000000000001', '03/19', '', false, false, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000002', '03/19', '', false, false, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000003', '03/19', '', false, false, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000004', '03/19', '', false, false, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000005', '03/19', '', false, false, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000006', '03/19', '', false, false, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000007', '03/19', '', false, false, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000008', '03/19', '', false, false, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('00319001-0001-0001-0001-000000000009', '03/19', '', false, false, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}');

-- =====================================================
-- KARFREITAG (EASTER-02)
-- Alle Kantone außer TI, VS
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0f000001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000002', 'EASTER-02', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000003', 'EASTER-02', '', true, true, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000004', 'EASTER-02', '', true, true, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000005', 'EASTER-02', '', true, true, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000006', 'EASTER-02', '', true, true, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000007', 'EASTER-02', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000008', 'EASTER-02', '', true, true, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000009', 'EASTER-02', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000010', 'EASTER-02', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000011', 'EASTER-02', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000012', 'EASTER-02', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000013', 'EASTER-02', '', true, true, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000014', 'EASTER-02', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000015', 'EASTER-02', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000016', 'EASTER-02', '', true, true, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000017', 'EASTER-02', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000018', 'EASTER-02', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000019', 'EASTER-02', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000020', 'EASTER-02', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000021', 'EASTER-02', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000022', 'EASTER-02', '', true, true, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000023', 'EASTER-02', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('0f000001-0001-0001-0001-000000000024', 'EASTER-02', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}');

-- =====================================================
-- OSTERMONTAG (EASTER+01)
-- Alle Kantone außer VS
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('00e00001-0001-0001-0001-000000000001', 'EASTER+01', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000002', 'EASTER+01', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000004', 'EASTER+01', '', true, true, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000005', 'EASTER+01', '', true, true, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000006', 'EASTER+01', '', true, true, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000007', 'EASTER+01', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000008', 'EASTER+01', '', true, true, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000009', 'EASTER+01', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000010', 'EASTER+01', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000011', 'EASTER+01', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000012', 'EASTER+01', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000013', 'EASTER+01', '', true, true, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000014', 'EASTER+01', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000015', 'EASTER+01', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000016', 'EASTER+01', '', true, true, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000017', 'EASTER+01', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000018', 'EASTER+01', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000019', 'EASTER+01', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000020', 'EASTER+01', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000021', 'EASTER+01', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000022', 'EASTER+01', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000023', 'EASTER+01', '', true, true, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000024', 'EASTER+01', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('00e00001-0001-0001-0001-000000000025', 'EASTER+01', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}');

-- =====================================================
-- TAG DER ARBEIT (05/01)
-- Kantone: AG, BL, BS, FR, JU, NE, SH, SO, TG, TI, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0da00001-0001-0001-0001-000000000001', '05/01', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000002', '05/01', '', true, true, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000003', '05/01', '', true, true, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000004', '05/01', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000005', '05/01', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000006', '05/01', '', true, true, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000007', '05/01', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000008', '05/01', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000009', '05/01', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000010', '05/01', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('0da00001-0001-0001-0001-000000000011', '05/01', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}');

-- =====================================================
-- PFINGSTMONTAG (EASTER+50)
-- Alle Kantone
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0f500001-0001-0001-0001-000000000001', 'EASTER+50', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000002', 'EASTER+50', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000003', 'EASTER+50', '', true, true, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000004', 'EASTER+50', '', true, true, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000005', 'EASTER+50', '', true, true, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000007', 'EASTER+50', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000008', 'EASTER+50', '', true, true, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000009', 'EASTER+50', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000010', 'EASTER+50', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000011', 'EASTER+50', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000012', 'EASTER+50', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000013', 'EASTER+50', '', true, true, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000014', 'EASTER+50', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000015', 'EASTER+50', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000016', 'EASTER+50', '', true, true, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000017', 'EASTER+50', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000018', 'EASTER+50', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000019', 'EASTER+50', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000020', 'EASTER+50', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000021', 'EASTER+50', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000022', 'EASTER+50', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000023', 'EASTER+50', '', true, true, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000024', 'EASTER+50', '', true, true, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000025', 'EASTER+50', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('0f500001-0001-0001-0001-000000000026', 'EASTER+50', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}');

-- =====================================================
-- FRONLEICHNAM (EASTER+60)
-- Kantone: AG, AI, FR, GR, JU, LU, NW, OW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('f0600001-0001-0001-0001-000000000001', 'EASTER+60', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000002', 'EASTER+60', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000003', 'EASTER+60', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000004', 'EASTER+60', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000005', 'EASTER+60', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000006', 'EASTER+60', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000007', 'EASTER+60', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000008', 'EASTER+60', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000009', 'EASTER+60', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000010', 'EASTER+60', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000011', 'EASTER+60', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000012', 'EASTER+60', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000013', 'EASTER+60', '', true, true, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('f0600001-0001-0001-0001-000000000014', 'EASTER+60', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}');

-- =====================================================
-- PETER UND PAUL (06/29)
-- Kantone: GR, TI
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('00629001-0001-0001-0001-000000000001', '06/29', '', false, false, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Peter und Paul"",""en"":""Saints Peter and Paul"",""fr"":""Saint-Pierre et Saint-Paul"",""it"":""Santi Pietro e Paolo""}'),
('00629001-0001-0001-0001-000000000002', '06/29', '', false, false, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Peter und Paul"",""en"":""Saints Peter and Paul"",""fr"":""Saint-Pierre et Saint-Paul"",""it"":""Santi Pietro e Paolo""}');

-- =====================================================
-- MARIÄ HIMMELFAHRT (08/15)
-- Kantone: AG, AI, FR, GR, JU, LU, NW, OW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('00815001-0001-0001-0001-000000000001', '08/15', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000002', '08/15', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000003', '08/15', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000004', '08/15', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000005', '08/15', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000006', '08/15', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000007', '08/15', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000008', '08/15', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000009', '08/15', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000010', '08/15', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000011', '08/15', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000012', '08/15', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000013', '08/15', '', true, true, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('00815001-0001-0001-0001-000000000014', '08/15', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}');

-- =====================================================
-- GENFER BETTAG (09/01+07+TH) - Donnerstag nach 1. Sonntag im September
-- Kanton: GE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0eb00001-0001-0001-0001-000000000001', '09/01+07+TH', '', true, true, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Genfer Bettag"",""en"":""Geneva Fast"",""fr"":""Jeûne genevois"",""it"":""Digiuno ginevrino""}');

-- =====================================================
-- BETTAGSMONTAG (09/01+15+MO) - Montag nach Eidg. Bettag
-- Kanton: VD
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('b0900001-0001-0001-0001-000000000001', '09/01+15+MO', '', true, true, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Bettagsmontag"",""en"":""Monday after Federal Fast"",""fr"":""Lundi du Jeûne"",""it"":""Lunedì del Digiuno""}');

-- =====================================================
-- BRUDER KLAUS (09/25)
-- Kanton: OW
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('b0925001-0001-0001-0001-000000000001', '09/25', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Bruder Klaus"",""en"":""St. Nicholas of Flüe"",""fr"":""Saint Nicolas de Flüe"",""it"":""San Nicola della Flüe""}');

-- =====================================================
-- ALLERHEILIGEN (11/01)
-- Kantone: AG, AI, FR, GL, JU, LU, NW, OW, SG, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('a1101001-0001-0001-0001-000000000001', '11/01', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000002', '11/01', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000003', '11/01', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000004', '11/01', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000005', '11/01', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000006', '11/01', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000007', '11/01', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000008', '11/01', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000009', '11/01', '', true, true, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000010', '11/01', '', true, true, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000011', '11/01', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000012', '11/01', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000013', '11/01', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000014', '11/01', '', true, true, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a1101001-0001-0001-0001-000000000015', '11/01', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}');

-- =====================================================
-- MARIÄ EMPFÄNGNIS (12/08)
-- Kantone: AG, AI, FR, GR, LU, NW, OW, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0e120801-0001-0001-0001-000000000001', '12/08', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000002', '12/08', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000003', '12/08', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000004', '12/08', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000005', '12/08', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000006', '12/08', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000007', '12/08', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000008', '12/08', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000009', '12/08', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000010', '12/08', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000011', '12/08', '', true, true, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('0e120801-0001-0001-0001-000000000012', '12/08', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}');

-- =====================================================
-- STEPHANSTAG (12/26)
-- Kantone: AG, AI, AR, BE, BL, BS, FR, GL, GR, LU, NW, OW, SG, SH, SZ, TG, TI, UR, ZG, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('01226001-0001-0001-0001-000000000001', '12/26', '', true, true, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000002', '12/26', '', true, true, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000003', '12/26', '', true, true, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000004', '12/26', '', true, true, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000005', '12/26', '', true, true, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000006', '12/26', '', true, true, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000007', '12/26', '', true, true, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000008', '12/26', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000009', '12/26', '', true, true, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000010', '12/26', '', true, true, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000011', '12/26', '', true, true, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000012', '12/26', '', true, true, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000013', '12/26', '', true, true, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000014', '12/26', '', true, true, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000015', '12/26', '', true, true, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000016', '12/26', '', true, true, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000017', '12/26', '', true, true, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000018', '12/26', '', true, true, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000019', '12/26', '', true, true, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01226001-0001-0001-0001-000000000020', '12/26', '', true, true, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}');

-- =====================================================
-- RESTAURATION GENF (12/31)
-- Kanton: GE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0e123101-0001-0001-0001-000000000001', '12/31', '', true, true, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Restauration der Republik"",""en"":""Restoration of the Republic"",""fr"":""Restauration de la République"",""it"":""Restaurazione della Repubblica""}');

-- =====================================================
-- NÄFELSER FAHRT (04/01+00+TH) - 1. Donnerstag im April
-- Kanton: GL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0ff00401-0001-0001-0001-000000000001', '04/01+00+TH', '', true, true, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Näfelser Fahrt"",""en"":""Battle of Näfels Memorial"",""fr"":""Commémoration de Näfels"",""it"":""Commemorazione di Näfels""}');

-- =====================================================
-- UNABHÄNGIGKEITSTAG JURA (06/23)
-- Kanton: JU
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0a062301-0001-0001-0001-000000000001', '06/23', '', true, true, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Unabhängigkeitstag"",""en"":""Independence Day"",""fr"":""Jour de l''Indépendance"",""it"":""Giorno dell''Indipendenza""}');

-- =====================================================
-- USA FEIERTAGE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('05a00001-0001-0001-0001-000000000001', '01/01', '', true, true, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('05a00001-0001-0001-0001-000000000002', '01/15+00+MO', '', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Martin Luther King Day"",""en"":""Martin Luther King Jr. Day"",""fr"":""Jour de Martin Luther King"",""it"":""Giorno di Martin Luther King""}'),
('05a00001-0001-0001-0001-000000000003', '02/01+14+MO', '', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Präsidententag"",""en"":""Presidents'' Day"",""fr"":""Jour des Présidents"",""it"":""Giorno dei Presidenti""}'),
('05a00001-0001-0001-0001-000000000004', '05/01+27+MO', '', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Memorial Day"",""en"":""Memorial Day"",""fr"":""Memorial Day"",""it"":""Memorial Day""}'),
('05a00001-0001-0001-0001-000000000005', '07/04', 'SA-1;SO+1', true, true, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Unabhängigkeitstag"",""en"":""Independence Day"",""fr"":""Jour de l''Indépendance"",""it"":""Giorno dell''Indipendenza""}'),
('05a00001-0001-0001-0001-000000000006', '09/01+00+MO', '', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labor Day"",""fr"":""Fête du Travail"",""it"":""Festa del Lavoro""}'),
('05a00001-0001-0001-0001-000000000007', '10/01+07+MO', '', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Columbus Day"",""en"":""Columbus Day"",""fr"":""Jour de Christophe Colomb"",""it"":""Giorno di Colombo""}'),
('05a00001-0001-0001-0001-000000000008', '11/11', 'SA-1;SO+1', false, false, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Veteranentag"",""en"":""Veterans Day"",""fr"":""Jour des Vétérans"",""it"":""Giorno dei Veterani""}'),
('05a00001-0001-0001-0001-000000000009', '11/01+21+TH', '', true, true, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Thanksgiving"",""en"":""Thanksgiving Day"",""fr"":""Action de grâce"",""it"":""Giorno del Ringraziamento""}'),
('05a00001-0001-0001-0001-000000000010', '12/25', '', true, true, 'USA', 'USA', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weihnachten"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}');

-- =====================================================
-- HALBE FEIERTAGE (nicht gesetzlich, aber üblich)
-- =====================================================

-- HEILIGABEND (12/24) - halber Tag in vielen Kantonen
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('0ab12401-0001-0001-0001-000000000001', '12/24', '', false, false, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000002', '12/24', '', false, false, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000003', '12/24', '', false, false, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000004', '12/24', '', false, false, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000005', '12/24', '', false, false, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000006', '12/24', '', false, false, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000007', '12/24', '', false, false, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000008', '12/24', '', false, false, 'GE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000009', '12/24', '', false, false, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000010', '12/24', '', false, false, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000011', '12/24', '', false, false, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000012', '12/24', '', false, false, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000013', '12/24', '', false, false, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000014', '12/24', '', false, false, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000015', '12/24', '', false, false, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000016', '12/24', '', false, false, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000017', '12/24', '', false, false, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000018', '12/24', '', false, false, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000019', '12/24', '', false, false, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000020', '12/24', '', false, false, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000021', '12/24', '', false, false, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000022', '12/24', '', false, false, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000023', '12/24', '', false, false, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000024', '12/24', '', false, false, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000025', '12/24', '', false, false, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('0ab12401-0001-0001-0001-000000000026', '12/24', '', false, false, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}');

-- SILVESTER (12/31) - halber Tag in vielen Kantonen (außer GE, wo es ein voller Feiertag ist)
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('01231001-0001-0001-0001-000000000001', '12/31', '', false, false, 'AG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000002', '12/31', '', false, false, 'AI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000003', '12/31', '', false, false, 'AR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000004', '12/31', '', false, false, 'BE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000005', '12/31', '', false, false, 'BL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000006', '12/31', '', false, false, 'BS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000007', '12/31', '', false, false, 'FR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000008', '12/31', '', false, false, 'GL', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000009', '12/31', '', false, false, 'GR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000010', '12/31', '', false, false, 'JU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000011', '12/31', '', false, false, 'LU', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000012', '12/31', '', false, false, 'NE', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000013', '12/31', '', false, false, 'NW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000014', '12/31', '', false, false, 'OW', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000015', '12/31', '', false, false, 'SG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000016', '12/31', '', false, false, 'SH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000017', '12/31', '', false, false, 'SO', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000018', '12/31', '', false, false, 'SZ', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000019', '12/31', '', false, false, 'TG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000020', '12/31', '', false, false, 'TI', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000021', '12/31', '', false, false, 'UR', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000022', '12/31', '', false, false, 'VD', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000023', '12/31', '', false, false, 'VS', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000024', '12/31', '', false, false, 'ZG', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}'),
('01231001-0001-0001-0001-000000000025', '12/31', '', false, false, 'ZH', 'CH', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');

-- #####################################################
-- ÖSTERREICH (AT)
-- Bundesländer: W (Wien), NÖ (Niederösterreich), OÖ (Oberösterreich),
-- S (Salzburg), T (Tirol), V (Vorarlberg), K (Kärnten), ST (Steiermark), B (Burgenland)
-- #####################################################

-- =====================================================
-- ÖSTERREICH - NATIONALE FEIERTAGE (AT AT) - gelten für alle Bundesländer
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('a0000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('a0000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('a0000001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('a0000001-0001-0001-0001-000000000004', '05/01', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Staatsfeiertag"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('a0000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Christi Himmelfahrt"",""en"":""Ascension Day"",""fr"":""Ascension"",""it"":""Ascensione""}'),
('a0000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('a0000001-0001-0001-0001-000000000007', 'EASTER+60', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('a0000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('a0000001-0001-0001-0001-000000000009', '10/26', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Nationalfeiertag"",""en"":""National Day"",""fr"":""Fête nationale"",""it"":""Festa nazionale""}'),
('a0000001-0001-0001-0001-000000000010', '11/01', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('a0000001-0001-0001-0001-000000000011', '12/08', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('a0000001-0001-0001-0001-000000000012', '12/25', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Christtag"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}'),
('a0000001-0001-0001-0001-000000000013', '12/26', '', true, true, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stefanitag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}');

-- ÖSTERREICH - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('a0000001-0001-0001-0001-000000000014', 'EASTER-02', '', false, false, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('a0000001-0001-0001-0001-000000000015', '12/24', '', false, false, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiliger Abend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('a0000001-0001-0001-0001-000000000016', '12/31', '', false, false, 'AT', 'AT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');

-- #####################################################
-- DEUTSCHLAND (DE)
-- Bundesländer: BW, BY, BE, BB, HB, HH, HE, MV, NI, NW, RP, SL, SN, ST, SH, TH
-- #####################################################

-- =====================================================
-- DEUTSCHLAND - NATIONALE FEIERTAGE (DE DE) - gelten für alle Bundesländer
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('de000001-0001-0001-0001-000000000002', 'EASTER-02', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('de000001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('de000001-0001-0001-0001-000000000004', '05/01', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('de000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Christi Himmelfahrt"",""en"":""Ascension Day"",""fr"":""Ascension"",""it"":""Ascensione""}'),
('de000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('de000001-0001-0001-0001-000000000007', '10/03', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Deutschen Einheit"",""en"":""German Unity Day"",""fr"":""Jour de l''unité allemande"",""it"":""Giorno dell''unità tedesca""}'),
('de000001-0001-0001-0001-000000000008', '12/25', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""1. Weihnachtstag"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}'),
('de000001-0001-0001-0001-000000000009', '12/26', '', true, true, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""2. Weihnachtstag"",""en"":""Boxing Day"",""fr"":""Lendemain de Noël"",""it"":""Santo Stefano""}');

-- =====================================================
-- DEUTSCHLAND - HEILIGE DREI KÖNIGE (01/06)
-- Bundesländer: BW, BY, ST
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de010601-0001-0001-0001-000000000001', '01/06', '', true, true, 'BW', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('de010601-0001-0001-0001-000000000002', '01/06', '', true, true, 'BY', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('de010601-0001-0001-0001-000000000003', '01/06', '', true, true, 'ST', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}');

-- =====================================================
-- DEUTSCHLAND - INTERNATIONALER FRAUENTAG (03/08)
-- Bundesländer: BE, MV
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de030801-0001-0001-0001-000000000001', '03/08', '', true, true, 'BE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Internationaler Frauentag"",""en"":""International Women''s Day"",""fr"":""Journée internationale des femmes"",""it"":""Giornata internazionale della donna""}'),
('de030801-0001-0001-0001-000000000002', '03/08', '', true, true, 'MV', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Internationaler Frauentag"",""en"":""International Women''s Day"",""fr"":""Journée internationale des femmes"",""it"":""Giornata internazionale della donna""}');

-- =====================================================
-- DEUTSCHLAND - FRONLEICHNAM (EASTER+60)
-- Bundesländer: BW, BY, HE, NW, RP, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('def00601-0001-0001-0001-000000000001', 'EASTER+60', '', true, true, 'BW', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('def00601-0001-0001-0001-000000000002', 'EASTER+60', '', true, true, 'BY', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('def00601-0001-0001-0001-000000000003', 'EASTER+60', '', true, true, 'HE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('def00601-0001-0001-0001-000000000004', 'EASTER+60', '', true, true, 'NW', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('def00601-0001-0001-0001-000000000005', 'EASTER+60', '', true, true, 'RP', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('def00601-0001-0001-0001-000000000006', 'EASTER+60', '', true, true, 'SL', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}');

-- =====================================================
-- DEUTSCHLAND - MARIÄ HIMMELFAHRT (08/15)
-- Bundesländer: BY, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de081501-0001-0001-0001-000000000001', '08/15', '', true, true, 'BY', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('de081501-0001-0001-0001-000000000002', '08/15', '', true, true, 'SL', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}');

-- =====================================================
-- DEUTSCHLAND - WELTKINDERTAG (09/20)
-- Bundesländer: TH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de092001-0001-0001-0001-000000000001', '09/20', '', true, true, 'TH', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weltkindertag"",""en"":""World Children''s Day"",""fr"":""Journée mondiale de l''enfance"",""it"":""Giornata mondiale dell''infanzia""}');

-- =====================================================
-- DEUTSCHLAND - REFORMATIONSTAG (10/31)
-- Bundesländer: BB, HB, HH, MV, NI, SN, ST, SH, TH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de103101-0001-0001-0001-000000000001', '10/31', '', true, true, 'BB', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000002', '10/31', '', true, true, 'HB', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000003', '10/31', '', true, true, 'HH', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000004', '10/31', '', true, true, 'MV', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000005', '10/31', '', true, true, 'NI', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000006', '10/31', '', true, true, 'SN', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000007', '10/31', '', true, true, 'ST', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000008', '10/31', '', true, true, 'SH', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}'),
('de103101-0001-0001-0001-000000000009', '10/31', '', true, true, 'TH', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Reformationstag"",""en"":""Reformation Day"",""fr"":""Jour de la Réforme"",""it"":""Giorno della Riforma""}');

-- =====================================================
-- DEUTSCHLAND - ALLERHEILIGEN (11/01)
-- Bundesländer: BW, BY, NW, RP, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('dea11001-0001-0001-0001-000000000001', '11/01', '', true, true, 'BW', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('dea11001-0001-0001-0001-000000000002', '11/01', '', true, true, 'BY', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('dea11001-0001-0001-0001-000000000003', '11/01', '', true, true, 'NW', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('dea11001-0001-0001-0001-000000000004', '11/01', '', true, true, 'RP', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('dea11001-0001-0001-0001-000000000005', '11/01', '', true, true, 'SL', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}');

-- =====================================================
-- DEUTSCHLAND - BUSS- UND BETTAG (Mittwoch vor dem 23. November)
-- Bundesländer: SN
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('deb00001-0001-0001-0001-000000000001', '11/16+00+WE', '', true, true, 'SN', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Buß- und Bettag"",""en"":""Day of Repentance and Prayer"",""fr"":""Jour de pénitence et de prière"",""it"":""Giorno di penitenza e preghiera""}');

-- DEUTSCHLAND - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('de000001-0001-0001-0001-000000000010', '12/24', '', false, false, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('de000001-0001-0001-0001-000000000011', '12/31', '', false, false, 'DE', 'DE', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');

-- #####################################################
-- FÜRSTENTUM LIECHTENSTEIN (LI)
-- Keine Unterteilung in Regionen
-- #####################################################

-- =====================================================
-- LIECHTENSTEIN - NATIONALE FEIERTAGE (LI LI)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('01000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('01000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('01000001-0001-0001-0001-000000000003', '02/02', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Lichtmess"",""en"":""Candlemas"",""fr"":""Chandeleur"",""it"":""Candelora""}'),
('01000001-0001-0001-0001-000000000004', '03/19', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Josefstag"",""en"":""St. Joseph''s Day"",""fr"":""Saint-Joseph"",""it"":""San Giuseppe""}'),
('01000001-0001-0001-0001-000000000005', 'EASTER-02', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('01000001-0001-0001-0001-000000000006', 'EASTER+01', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('01000001-0001-0001-0001-000000000007', '05/01', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('01000001-0001-0001-0001-000000000008', 'EASTER+39', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Christi Himmelfahrt"",""en"":""Ascension Day"",""fr"":""Ascension"",""it"":""Ascensione""}'),
('01000001-0001-0001-0001-000000000009', 'EASTER+50', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('01000001-0001-0001-0001-000000000010', 'EASTER+60', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Fronleichnam"",""en"":""Corpus Christi"",""fr"":""Fête-Dieu"",""it"":""Corpus Domini""}'),
('01000001-0001-0001-0001-000000000011', '08/15', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Staatsfeiertag"",""en"":""National Day"",""fr"":""Fête nationale"",""it"":""Festa nazionale""}'),
('01000001-0001-0001-0001-000000000012', '09/08', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Geburt"",""en"":""Nativity of Mary"",""fr"":""Nativité de Marie"",""it"":""Natività di Maria""}'),
('01000001-0001-0001-0001-000000000013', '11/01', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('01000001-0001-0001-0001-000000000014', '12/08', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('01000001-0001-0001-0001-000000000015', '12/24', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('01000001-0001-0001-0001-000000000016', '12/25', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weihnachten"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}'),
('01000001-0001-0001-0001-000000000017', '12/26', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('01000001-0001-0001-0001-000000000018', '12/31', '', true, true, 'LI', 'LI', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');

-- #####################################################
-- ITALIEN (IT)
-- Nationale Feiertage gelten landesweit
-- #####################################################

-- =====================================================
-- ITALIEN - NATIONALE FEIERTAGE (IT IT)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('10000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('10000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heilige Drei Könige"",""en"":""Epiphany"",""fr"":""Épiphanie"",""it"":""Epifania""}'),
('10000001-0001-0001-0001-000000000003', 'EASTER', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostersonntag"",""en"":""Easter Sunday"",""fr"":""Pâques"",""it"":""Pasqua""}'),
('10000001-0001-0001-0001-000000000004', 'EASTER+01', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì dell''Angelo""}'),
('10000001-0001-0001-0001-000000000005', '04/25', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Befreiung"",""en"":""Liberation Day"",""fr"":""Jour de la Libération"",""it"":""Festa della Liberazione""}'),
('10000001-0001-0001-0001-000000000006', '05/01', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del Lavoro""}'),
('10000001-0001-0001-0001-000000000007', '06/02', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Republik"",""en"":""Republic Day"",""fr"":""Fête de la République"",""it"":""Festa della Repubblica""}'),
('10000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Ferragosto""}'),
('10000001-0001-0001-0001-000000000009', '11/01', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('10000001-0001-0001-0001-000000000010', '12/08', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Empfängnis"",""en"":""Immaculate Conception"",""fr"":""Immaculée Conception"",""it"":""Immacolata Concezione""}'),
('10000001-0001-0001-0001-000000000011', '12/25', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weihnachten"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}'),
('10000001-0001-0001-0001-000000000012', '12/26', '', true, true, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}');

-- ITALIEN - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('10000001-0001-0001-0001-000000000013', '12/24', '', false, false, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('10000001-0001-0001-0001-000000000014', '12/31', '', false, false, 'IT', 'IT', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');

-- #####################################################
-- FRANKREICH (FR)
-- Nationale Feiertage gelten landesweit
-- Elsass-Lothringen (67, 68, 57) hat zusätzliche Feiertage
-- #####################################################

-- =====================================================
-- FRANKREICH - NATIONALE FEIERTAGE (FR FR)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('f0000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Neujahr"",""en"":""New Year''s Day"",""fr"":""Jour de l''An"",""it"":""Capodanno""}'),
('f0000001-0001-0001-0001-000000000002', 'EASTER+01', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Ostermontag"",""en"":""Easter Monday"",""fr"":""Lundi de Pâques"",""it"":""Lunedì di Pasqua""}'),
('f0000001-0001-0001-0001-000000000003', '05/01', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag der Arbeit"",""en"":""Labour Day"",""fr"":""Fête du Travail"",""it"":""Festa del lavoro""}'),
('f0000001-0001-0001-0001-000000000004', '05/08', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Tag des Sieges"",""en"":""Victory Day"",""fr"":""Victoire 1945"",""it"":""Giorno della Vittoria""}'),
('f0000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Christi Himmelfahrt"",""en"":""Ascension Day"",""fr"":""Ascension"",""it"":""Ascensione""}'),
('f0000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Pfingstmontag"",""en"":""Whit Monday"",""fr"":""Lundi de Pentecôte"",""it"":""Lunedì di Pentecoste""}'),
('f0000001-0001-0001-0001-000000000007', '07/14', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Nationalfeiertag"",""en"":""Bastille Day"",""fr"":""Fête nationale"",""it"":""Presa della Bastiglia""}'),
('f0000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Mariä Himmelfahrt"",""en"":""Assumption of Mary"",""fr"":""Assomption"",""it"":""Assunzione di Maria""}'),
('f0000001-0001-0001-0001-000000000009', '11/01', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Allerheiligen"",""en"":""All Saints'' Day"",""fr"":""Toussaint"",""it"":""Ognissanti""}'),
('f0000001-0001-0001-0001-000000000010', '11/11', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Waffenstillstand"",""en"":""Armistice Day"",""fr"":""Armistice"",""it"":""Armistizio""}'),
('f0000001-0001-0001-0001-000000000011', '12/25', '', true, true, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Weihnachten"",""en"":""Christmas Day"",""fr"":""Noël"",""it"":""Natale""}');

-- =====================================================
-- FRANKREICH - ELSASS-MOSEL ZUSÄTZLICHE FEIERTAGE
-- Départements: 57 (Moselle), 67 (Bas-Rhin), 68 (Haut-Rhin)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('f0570001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '57', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('f0570001-0001-0001-0001-000000000002', '12/26', '', true, true, '57', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('f0670001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '67', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('f0670001-0001-0001-0001-000000000002', '12/26', '', true, true, '67', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}'),
('f0680001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '68', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Karfreitag"",""en"":""Good Friday"",""fr"":""Vendredi saint"",""it"":""Venerdì Santo""}'),
('f0680001-0001-0001-0001-000000000002', '12/26', '', true, true, '68', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Stephanstag"",""en"":""St. Stephen''s Day"",""fr"":""Saint-Étienne"",""it"":""Santo Stefano""}');

-- FRANKREICH - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES
('f0000001-0001-0001-0001-000000000012', '12/24', '', false, false, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Heiligabend"",""en"":""Christmas Eve"",""fr"":""Veille de Noël"",""it"":""Vigilia di Natale""}'),
('f0000001-0001-0001-0001-000000000013', '12/31', '', false, false, 'FR', 'FR', '{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}', '{""de"":""Silvester"",""en"":""New Year''s Eve"",""fr"":""Saint-Sylvestre"",""it"":""San Silvestro""}');
"
            );
        }
    }
}
