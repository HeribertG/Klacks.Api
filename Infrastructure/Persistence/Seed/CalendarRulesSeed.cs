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
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('613c22be-e39f-4a40-be5a-e1202d21678f', '01/01', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('682409ab-0b61-450e-84c3-309a72765de9', '08/01', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Bundesfeiertag', 'Swiss National Day', 'Fête nationale', 'Festa nazionale'),
('f54d9dd4-5e9c-49d6-81a0-c04d9715d519', 'EASTER+39', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Auffahrt', 'Ascension Day', 'Ascension', 'Ascensione'),
('99fb49c4-8fea-4266-8836-e2b216345470', '12/25', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Weihnachten', 'Christmas Day', 'Noël', 'Natale'),
('611f688c-8f58-4e8c-a3a6-a6c54f40f874', '09/01+14+SU', '', false, false, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Eidg. Dank-, Buss- und Bettag', 'Federal Day of Thanksgiving', 'Jeûne fédéral', 'Digiuno federale');

-- =====================================================
-- BERCHTOLDSTAG (01/02)
-- Kantone: AG, BE, FR, GL, JU, LU, NE, OW, SH, SO, TG, VD, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('b0102001-0001-0001-0001-000000000001', '01/02', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000002', '01/02', '', true, true, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000003', '01/02', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000004', '01/02', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000005', '01/02', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000006', '01/02', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000007', '01/02', '', true, true, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000008', '01/02', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000009', '01/02', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000010', '01/02', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000011', '01/02', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000012', '01/02', '', true, true, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo'),
('b0102001-0001-0001-0001-000000000013', '01/02', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Berchtoldstag', 'St. Berchtold''s Day', 'Saint-Berchtold', 'San Bertoldo');

-- =====================================================
-- DREIKÖNIGSTAG (01/06)
-- Kantone: GR, SZ, TI, UR
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('d0106001-0001-0001-0001-000000000001', '01/06', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Dreikönigstag', 'Epiphany', 'Épiphanie', 'Epifania'),
('d0106001-0001-0001-0001-000000000002', '01/06', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Dreikönigstag', 'Epiphany', 'Épiphanie', 'Epifania'),
('d0106001-0001-0001-0001-000000000003', '01/06', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Dreikönigstag', 'Epiphany', 'Épiphanie', 'Epifania'),
('d0106001-0001-0001-0001-000000000004', '01/06', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Dreikönigstag', 'Epiphany', 'Épiphanie', 'Epifania');

-- =====================================================
-- JOSEFSTAG (03/19)
-- Kantone: GR, LU, NW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('00319001-0001-0001-0001-000000000001', '03/19', '', false, false, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000002', '03/19', '', false, false, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000003', '03/19', '', false, false, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000004', '03/19', '', false, false, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000005', '03/19', '', false, false, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000006', '03/19', '', false, false, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000007', '03/19', '', false, false, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000008', '03/19', '', false, false, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('00319001-0001-0001-0001-000000000009', '03/19', '', false, false, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe');

-- =====================================================
-- KARFREITAG (EASTER-02)
-- Alle Kantone außer TI, VS
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0f000001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000002', 'EASTER-02', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000003', 'EASTER-02', '', true, true, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000004', 'EASTER-02', '', true, true, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000005', 'EASTER-02', '', true, true, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000006', 'EASTER-02', '', true, true, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000007', 'EASTER-02', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000008', 'EASTER-02', '', true, true, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000009', 'EASTER-02', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000010', 'EASTER-02', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000011', 'EASTER-02', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000012', 'EASTER-02', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000013', 'EASTER-02', '', true, true, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000014', 'EASTER-02', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000015', 'EASTER-02', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000016', 'EASTER-02', '', true, true, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000017', 'EASTER-02', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000018', 'EASTER-02', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000019', 'EASTER-02', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000020', 'EASTER-02', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000021', 'EASTER-02', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000022', 'EASTER-02', '', true, true, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000023', 'EASTER-02', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('0f000001-0001-0001-0001-000000000024', 'EASTER-02', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo');

-- =====================================================
-- OSTERMONTAG (EASTER+01)
-- Alle Kantone außer VS
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('00e00001-0001-0001-0001-000000000001', 'EASTER+01', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000002', 'EASTER+01', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000004', 'EASTER+01', '', true, true, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000005', 'EASTER+01', '', true, true, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000006', 'EASTER+01', '', true, true, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000007', 'EASTER+01', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000008', 'EASTER+01', '', true, true, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000009', 'EASTER+01', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000010', 'EASTER+01', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000011', 'EASTER+01', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000012', 'EASTER+01', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000013', 'EASTER+01', '', true, true, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000014', 'EASTER+01', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000015', 'EASTER+01', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000016', 'EASTER+01', '', true, true, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000017', 'EASTER+01', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000018', 'EASTER+01', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000019', 'EASTER+01', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000020', 'EASTER+01', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000021', 'EASTER+01', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000022', 'EASTER+01', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000023', 'EASTER+01', '', true, true, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000024', 'EASTER+01', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('00e00001-0001-0001-0001-000000000025', 'EASTER+01', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua');

-- =====================================================
-- TAG DER ARBEIT (05/01)
-- Kantone: AG, BL, BS, FR, JU, NE, SH, SO, TG, TI, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0da00001-0001-0001-0001-000000000001', '05/01', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000002', '05/01', '', true, true, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000003', '05/01', '', true, true, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000004', '05/01', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000005', '05/01', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000006', '05/01', '', true, true, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000007', '05/01', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000008', '05/01', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000009', '05/01', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000010', '05/01', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('0da00001-0001-0001-0001-000000000011', '05/01', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro');

-- =====================================================
-- PFINGSTMONTAG (EASTER+50)
-- Alle Kantone
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0f500001-0001-0001-0001-000000000001', 'EASTER+50', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000002', 'EASTER+50', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000003', 'EASTER+50', '', true, true, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000004', 'EASTER+50', '', true, true, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000005', 'EASTER+50', '', true, true, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000007', 'EASTER+50', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000008', 'EASTER+50', '', true, true, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000009', 'EASTER+50', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000010', 'EASTER+50', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000011', 'EASTER+50', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000012', 'EASTER+50', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000013', 'EASTER+50', '', true, true, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000014', 'EASTER+50', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000015', 'EASTER+50', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000016', 'EASTER+50', '', true, true, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000017', 'EASTER+50', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000018', 'EASTER+50', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000019', 'EASTER+50', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000020', 'EASTER+50', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000021', 'EASTER+50', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000022', 'EASTER+50', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000023', 'EASTER+50', '', true, true, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000024', 'EASTER+50', '', true, true, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000025', 'EASTER+50', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('0f500001-0001-0001-0001-000000000026', 'EASTER+50', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste');

-- =====================================================
-- FRONLEICHNAM (EASTER+60)
-- Kantone: AG, AI, FR, GR, JU, LU, NW, OW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('f0600001-0001-0001-0001-000000000001', 'EASTER+60', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000002', 'EASTER+60', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000003', 'EASTER+60', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000004', 'EASTER+60', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000005', 'EASTER+60', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000006', 'EASTER+60', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000007', 'EASTER+60', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000008', 'EASTER+60', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000009', 'EASTER+60', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000010', 'EASTER+60', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000011', 'EASTER+60', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000012', 'EASTER+60', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000013', 'EASTER+60', '', true, true, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('f0600001-0001-0001-0001-000000000014', 'EASTER+60', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini');

-- =====================================================
-- PETER UND PAUL (06/29)
-- Kantone: GR, TI
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('00629001-0001-0001-0001-000000000001', '06/29', '', false, false, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Peter und Paul', 'Saints Peter and Paul', 'Saint-Pierre et Saint-Paul', 'Santi Pietro e Paolo'),
('00629001-0001-0001-0001-000000000002', '06/29', '', false, false, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Peter und Paul', 'Saints Peter and Paul', 'Saint-Pierre et Saint-Paul', 'Santi Pietro e Paolo');

-- =====================================================
-- MARIÄ HIMMELFAHRT (08/15)
-- Kantone: AG, AI, FR, GR, JU, LU, NW, OW, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('00815001-0001-0001-0001-000000000001', '08/15', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000002', '08/15', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000003', '08/15', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000004', '08/15', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000005', '08/15', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000006', '08/15', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000007', '08/15', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000008', '08/15', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000009', '08/15', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000010', '08/15', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000011', '08/15', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000012', '08/15', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000013', '08/15', '', true, true, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('00815001-0001-0001-0001-000000000014', '08/15', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria');

-- =====================================================
-- GENFER BETTAG (09/01+07+TH) - Donnerstag nach 1. Sonntag im September
-- Kanton: GE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0eb00001-0001-0001-0001-000000000001', '09/01+07+TH', '', true, true, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Genfer Bettag', 'Geneva Fast', 'Jeûne genevois', 'Digiuno ginevrino');

-- =====================================================
-- BETTAGSMONTAG (09/01+15+MO) - Montag nach Eidg. Bettag
-- Kanton: VD
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('b0900001-0001-0001-0001-000000000001', '09/01+15+MO', '', true, true, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Bettagsmontag', 'Monday after Federal Fast', 'Lundi du Jeûne', 'Lunedì del Digiuno');

-- =====================================================
-- BRUDER KLAUS (09/25)
-- Kanton: OW
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('b0925001-0001-0001-0001-000000000001', '09/25', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Bruder Klaus', 'St. Nicholas of Flüe', 'Saint Nicolas de Flüe', 'San Nicola della Flüe');

-- =====================================================
-- ALLERHEILIGEN (11/01)
-- Kantone: AG, AI, FR, GL, JU, LU, NW, OW, SG, SO, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('a1101001-0001-0001-0001-000000000001', '11/01', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000002', '11/01', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000003', '11/01', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000004', '11/01', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000005', '11/01', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000006', '11/01', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000007', '11/01', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000008', '11/01', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000009', '11/01', '', true, true, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000010', '11/01', '', true, true, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000011', '11/01', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000012', '11/01', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000013', '11/01', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000014', '11/01', '', true, true, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a1101001-0001-0001-0001-000000000015', '11/01', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti');

-- =====================================================
-- MARIÄ EMPFÄNGNIS (12/08)
-- Kantone: AG, AI, FR, GR, LU, NW, OW, SZ, TI, UR, VS, ZG
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0e120801-0001-0001-0001-000000000001', '12/08', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000002', '12/08', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000003', '12/08', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000004', '12/08', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000005', '12/08', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000006', '12/08', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000007', '12/08', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000008', '12/08', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000009', '12/08', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000010', '12/08', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000011', '12/08', '', true, true, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('0e120801-0001-0001-0001-000000000012', '12/08', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione');

-- =====================================================
-- STEPHANSTAG (12/26)
-- Kantone: AG, AI, AR, BE, BL, BS, FR, GL, GR, LU, NW, OW, SG, SH, SZ, TG, TI, UR, ZG, ZH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('01226001-0001-0001-0001-000000000001', '12/26', '', true, true, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000002', '12/26', '', true, true, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000003', '12/26', '', true, true, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000004', '12/26', '', true, true, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000005', '12/26', '', true, true, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000006', '12/26', '', true, true, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000007', '12/26', '', true, true, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000008', '12/26', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000009', '12/26', '', true, true, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000010', '12/26', '', true, true, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000011', '12/26', '', true, true, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000012', '12/26', '', true, true, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000013', '12/26', '', true, true, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000014', '12/26', '', true, true, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000015', '12/26', '', true, true, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000016', '12/26', '', true, true, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000017', '12/26', '', true, true, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000018', '12/26', '', true, true, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000019', '12/26', '', true, true, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01226001-0001-0001-0001-000000000020', '12/26', '', true, true, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano');

-- =====================================================
-- RESTAURATION GENF (12/31)
-- Kanton: GE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0e123101-0001-0001-0001-000000000001', '12/31', '', true, true, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Restauration der Republik', 'Restoration of the Republic', 'Restauration de la République', 'Restaurazione della Repubblica');

-- =====================================================
-- NÄFELSER FAHRT (04/01+00+TH) - 1. Donnerstag im April
-- Kanton: GL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0ff00401-0001-0001-0001-000000000001', '04/01+00+TH', '', true, true, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Näfelser Fahrt', 'Battle of Näfels Memorial', 'Commémoration de Näfels', 'Commemorazione di Näfels');

-- =====================================================
-- UNABHÄNGIGKEITSTAG JURA (06/23)
-- Kanton: JU
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0a062301-0001-0001-0001-000000000001', '06/23', '', true, true, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Unabhängigkeitstag', 'Independence Day', 'Jour de l''Indépendance', 'Giorno dell''Indipendenza');

-- =====================================================
-- USA FEIERTAGE
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('05a00001-0001-0001-0001-000000000001', '01/01', '', true, true, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('05a00001-0001-0001-0001-000000000002', '01/15+00+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Martin Luther King Day', 'Martin Luther King Jr. Day', 'Jour de Martin Luther King', 'Giorno di Martin Luther King'),
('05a00001-0001-0001-0001-000000000003', '02/01+14+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Präsidententag', 'Presidents'' Day', 'Jour des Présidents', 'Giorno dei Presidenti'),
('05a00001-0001-0001-0001-000000000004', '05/01+27+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Memorial Day', 'Memorial Day', 'Memorial Day', 'Memorial Day'),
('05a00001-0001-0001-0001-000000000005', '07/04', 'SA-1;SO+1', true, true, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Unabhängigkeitstag', 'Independence Day', 'Jour de l''Indépendance', 'Giorno dell''Indipendenza'),
('05a00001-0001-0001-0001-000000000006', '09/01+00+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labor Day', 'Fête du Travail', 'Festa del Lavoro'),
('05a00001-0001-0001-0001-000000000007', '10/01+07+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Columbus Day', 'Columbus Day', 'Jour de Christophe Colomb', 'Giorno di Colombo'),
('05a00001-0001-0001-0001-000000000008', '11/11', 'SA-1;SO+1', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Veteranentag', 'Veterans Day', 'Jour des Vétérans', 'Giorno dei Veterani'),
('05a00001-0001-0001-0001-000000000009', '11/01+21+TH', '', true, true, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Thanksgiving', 'Thanksgiving Day', 'Action de grâce', 'Giorno del Ringraziamento'),
('05a00001-0001-0001-0001-000000000010', '12/25', '', true, true, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Weihnachten', 'Christmas Day', 'Noël', 'Natale');

-- =====================================================
-- HALBE FEIERTAGE (nicht gesetzlich, aber üblich)
-- =====================================================

-- HEILIGABEND (12/24) - halber Tag in vielen Kantonen
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('0ab12401-0001-0001-0001-000000000001', '12/24', '', false, false, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000002', '12/24', '', false, false, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000003', '12/24', '', false, false, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000004', '12/24', '', false, false, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000005', '12/24', '', false, false, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000006', '12/24', '', false, false, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000007', '12/24', '', false, false, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000008', '12/24', '', false, false, 'GE', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000009', '12/24', '', false, false, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000010', '12/24', '', false, false, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000011', '12/24', '', false, false, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000012', '12/24', '', false, false, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000013', '12/24', '', false, false, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000014', '12/24', '', false, false, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000015', '12/24', '', false, false, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000016', '12/24', '', false, false, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000017', '12/24', '', false, false, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000018', '12/24', '', false, false, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000019', '12/24', '', false, false, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000020', '12/24', '', false, false, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000021', '12/24', '', false, false, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000022', '12/24', '', false, false, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000023', '12/24', '', false, false, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000024', '12/24', '', false, false, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000025', '12/24', '', false, false, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('0ab12401-0001-0001-0001-000000000026', '12/24', '', false, false, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale');

-- SILVESTER (12/31) - halber Tag in vielen Kantonen (außer GE, wo es ein voller Feiertag ist)
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('01231001-0001-0001-0001-000000000001', '12/31', '', false, false, 'AG', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000002', '12/31', '', false, false, 'AI', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000003', '12/31', '', false, false, 'AR', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000004', '12/31', '', false, false, 'BE', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000005', '12/31', '', false, false, 'BL', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000006', '12/31', '', false, false, 'BS', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000007', '12/31', '', false, false, 'FR', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000008', '12/31', '', false, false, 'GL', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000009', '12/31', '', false, false, 'GR', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000010', '12/31', '', false, false, 'JU', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000011', '12/31', '', false, false, 'LU', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000012', '12/31', '', false, false, 'NE', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000013', '12/31', '', false, false, 'NW', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000014', '12/31', '', false, false, 'OW', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000015', '12/31', '', false, false, 'SG', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000016', '12/31', '', false, false, 'SH', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000017', '12/31', '', false, false, 'SO', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000018', '12/31', '', false, false, 'SZ', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000019', '12/31', '', false, false, 'TG', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000020', '12/31', '', false, false, 'TI', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000021', '12/31', '', false, false, 'UR', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000022', '12/31', '', false, false, 'VD', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000023', '12/31', '', false, false, 'VS', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000024', '12/31', '', false, false, 'ZG', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro'),
('01231001-0001-0001-0001-000000000025', '12/31', '', false, false, 'ZH', 'CH', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');

-- #####################################################
-- ÖSTERREICH (AT)
-- Bundesländer: W (Wien), NÖ (Niederösterreich), OÖ (Oberösterreich),
-- S (Salzburg), T (Tirol), V (Vorarlberg), K (Kärnten), ST (Steiermark), B (Burgenland)
-- #####################################################

-- =====================================================
-- ÖSTERREICH - NATIONALE FEIERTAGE (AT AT) - gelten für alle Bundesländer
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('a0000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('a0000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania'),
('a0000001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('a0000001-0001-0001-0001-000000000004', '05/01', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Staatsfeiertag', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('a0000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Christi Himmelfahrt', 'Ascension Day', 'Ascension', 'Ascensione'),
('a0000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('a0000001-0001-0001-0001-000000000007', 'EASTER+60', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('a0000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('a0000001-0001-0001-0001-000000000009', '10/26', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Nationalfeiertag', 'National Day', 'Fête nationale', 'Festa nazionale'),
('a0000001-0001-0001-0001-000000000010', '11/01', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('a0000001-0001-0001-0001-000000000011', '12/08', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('a0000001-0001-0001-0001-000000000012', '12/25', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Christtag', 'Christmas Day', 'Noël', 'Natale'),
('a0000001-0001-0001-0001-000000000013', '12/26', '', true, true, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Stefanitag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano');

-- ÖSTERREICH - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('a0000001-0001-0001-0001-000000000014', 'EASTER-02', '', false, false, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('a0000001-0001-0001-0001-000000000015', '12/24', '', false, false, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Heiliger Abend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('a0000001-0001-0001-0001-000000000016', '12/31', '', false, false, 'AT', 'AT', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');

-- #####################################################
-- DEUTSCHLAND (DE)
-- Bundesländer: BW, BY, BE, BB, HB, HH, HE, MV, NI, NW, RP, SL, SN, ST, SH, TH
-- #####################################################

-- =====================================================
-- DEUTSCHLAND - NATIONALE FEIERTAGE (DE DE) - gelten für alle Bundesländer
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('de000001-0001-0001-0001-000000000002', 'EASTER-02', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('de000001-0001-0001-0001-000000000003', 'EASTER+01', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('de000001-0001-0001-0001-000000000004', '05/01', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('de000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Christi Himmelfahrt', 'Ascension Day', 'Ascension', 'Ascensione'),
('de000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('de000001-0001-0001-0001-000000000007', '10/03', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Tag der Deutschen Einheit', 'German Unity Day', 'Jour de l''unité allemande', 'Giorno dell''unità tedesca'),
('de000001-0001-0001-0001-000000000008', '12/25', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, '1. Weihnachtstag', 'Christmas Day', 'Noël', 'Natale'),
('de000001-0001-0001-0001-000000000009', '12/26', '', true, true, 'DE', 'DE', NULL, NULL, NULL, NULL, '2. Weihnachtstag', 'Boxing Day', 'Lendemain de Noël', 'Santo Stefano');

-- =====================================================
-- DEUTSCHLAND - HEILIGE DREI KÖNIGE (01/06)
-- Bundesländer: BW, BY, ST
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de010601-0001-0001-0001-000000000001', '01/06', '', true, true, 'BW', 'DE', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania'),
('de010601-0001-0001-0001-000000000002', '01/06', '', true, true, 'BY', 'DE', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania'),
('de010601-0001-0001-0001-000000000003', '01/06', '', true, true, 'ST', 'DE', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania');

-- =====================================================
-- DEUTSCHLAND - INTERNATIONALER FRAUENTAG (03/08)
-- Bundesländer: BE, MV
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de030801-0001-0001-0001-000000000001', '03/08', '', true, true, 'BE', 'DE', NULL, NULL, NULL, NULL, 'Internationaler Frauentag', 'International Women''s Day', 'Journée internationale des femmes', 'Giornata internazionale della donna'),
('de030801-0001-0001-0001-000000000002', '03/08', '', true, true, 'MV', 'DE', NULL, NULL, NULL, NULL, 'Internationaler Frauentag', 'International Women''s Day', 'Journée internationale des femmes', 'Giornata internazionale della donna');

-- =====================================================
-- DEUTSCHLAND - FRONLEICHNAM (EASTER+60)
-- Bundesländer: BW, BY, HE, NW, RP, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('def00601-0001-0001-0001-000000000001', 'EASTER+60', '', true, true, 'BW', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('def00601-0001-0001-0001-000000000002', 'EASTER+60', '', true, true, 'BY', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('def00601-0001-0001-0001-000000000003', 'EASTER+60', '', true, true, 'HE', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('def00601-0001-0001-0001-000000000004', 'EASTER+60', '', true, true, 'NW', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('def00601-0001-0001-0001-000000000005', 'EASTER+60', '', true, true, 'RP', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('def00601-0001-0001-0001-000000000006', 'EASTER+60', '', true, true, 'SL', 'DE', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini');

-- =====================================================
-- DEUTSCHLAND - MARIÄ HIMMELFAHRT (08/15)
-- Bundesländer: BY, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de081501-0001-0001-0001-000000000001', '08/15', '', true, true, 'BY', 'DE', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('de081501-0001-0001-0001-000000000002', '08/15', '', true, true, 'SL', 'DE', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria');

-- =====================================================
-- DEUTSCHLAND - WELTKINDERTAG (09/20)
-- Bundesländer: TH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de092001-0001-0001-0001-000000000001', '09/20', '', true, true, 'TH', 'DE', NULL, NULL, NULL, NULL, 'Weltkindertag', 'World Children''s Day', 'Journée mondiale de l''enfance', 'Giornata mondiale dell''infanzia');

-- =====================================================
-- DEUTSCHLAND - REFORMATIONSTAG (10/31)
-- Bundesländer: BB, HB, HH, MV, NI, SN, ST, SH, TH
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de103101-0001-0001-0001-000000000001', '10/31', '', true, true, 'BB', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000002', '10/31', '', true, true, 'HB', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000003', '10/31', '', true, true, 'HH', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000004', '10/31', '', true, true, 'MV', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000005', '10/31', '', true, true, 'NI', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000006', '10/31', '', true, true, 'SN', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000007', '10/31', '', true, true, 'ST', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000008', '10/31', '', true, true, 'SH', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma'),
('de103101-0001-0001-0001-000000000009', '10/31', '', true, true, 'TH', 'DE', NULL, NULL, NULL, NULL, 'Reformationstag', 'Reformation Day', 'Jour de la Réforme', 'Giorno della Riforma');

-- =====================================================
-- DEUTSCHLAND - ALLERHEILIGEN (11/01)
-- Bundesländer: BW, BY, NW, RP, SL
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('dea11001-0001-0001-0001-000000000001', '11/01', '', true, true, 'BW', 'DE', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('dea11001-0001-0001-0001-000000000002', '11/01', '', true, true, 'BY', 'DE', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('dea11001-0001-0001-0001-000000000003', '11/01', '', true, true, 'NW', 'DE', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('dea11001-0001-0001-0001-000000000004', '11/01', '', true, true, 'RP', 'DE', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('dea11001-0001-0001-0001-000000000005', '11/01', '', true, true, 'SL', 'DE', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti');

-- =====================================================
-- DEUTSCHLAND - BUSS- UND BETTAG (Mittwoch vor dem 23. November)
-- Bundesländer: SN
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('deb00001-0001-0001-0001-000000000001', '11/16+00+WE', '', true, true, 'SN', 'DE', NULL, NULL, NULL, NULL, 'Buß- und Bettag', 'Day of Repentance and Prayer', 'Jour de pénitence et de prière', 'Giorno di penitenza e preghiera');

-- DEUTSCHLAND - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('de000001-0001-0001-0001-000000000010', '12/24', '', false, false, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('de000001-0001-0001-0001-000000000011', '12/31', '', false, false, 'DE', 'DE', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');

-- #####################################################
-- FÜRSTENTUM LIECHTENSTEIN (LI)
-- Keine Unterteilung in Regionen
-- #####################################################

-- =====================================================
-- LIECHTENSTEIN - NATIONALE FEIERTAGE (LI LI)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('01000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('01000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania'),
('01000001-0001-0001-0001-000000000003', '02/02', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Mariä Lichtmess', 'Candlemas', 'Chandeleur', 'Candelora'),
('01000001-0001-0001-0001-000000000004', '03/19', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Josefstag', 'St. Joseph''s Day', 'Saint-Joseph', 'San Giuseppe'),
('01000001-0001-0001-0001-000000000005', 'EASTER-02', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('01000001-0001-0001-0001-000000000006', 'EASTER+01', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('01000001-0001-0001-0001-000000000007', '05/01', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('01000001-0001-0001-0001-000000000008', 'EASTER+39', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Christi Himmelfahrt', 'Ascension Day', 'Ascension', 'Ascensione'),
('01000001-0001-0001-0001-000000000009', 'EASTER+50', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('01000001-0001-0001-0001-000000000010', 'EASTER+60', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Fronleichnam', 'Corpus Christi', 'Fête-Dieu', 'Corpus Domini'),
('01000001-0001-0001-0001-000000000011', '08/15', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Staatsfeiertag', 'National Day', 'Fête nationale', 'Festa nazionale'),
('01000001-0001-0001-0001-000000000012', '09/08', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Mariä Geburt', 'Nativity of Mary', 'Nativité de Marie', 'Natività di Maria'),
('01000001-0001-0001-0001-000000000013', '11/01', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('01000001-0001-0001-0001-000000000014', '12/08', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('01000001-0001-0001-0001-000000000015', '12/24', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('01000001-0001-0001-0001-000000000016', '12/25', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Weihnachten', 'Christmas Day', 'Noël', 'Natale'),
('01000001-0001-0001-0001-000000000017', '12/26', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('01000001-0001-0001-0001-000000000018', '12/31', '', true, true, 'LI', 'LI', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');

-- #####################################################
-- ITALIEN (IT)
-- Nationale Feiertage gelten landesweit
-- #####################################################

-- =====================================================
-- ITALIEN - NATIONALE FEIERTAGE (IT IT)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('10000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('10000001-0001-0001-0001-000000000002', '01/06', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Heilige Drei Könige', 'Epiphany', 'Épiphanie', 'Epifania'),
('10000001-0001-0001-0001-000000000003', 'EASTER', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Ostersonntag', 'Easter Sunday', 'Pâques', 'Pasqua'),
('10000001-0001-0001-0001-000000000004', 'EASTER+01', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì dell''Angelo'),
('10000001-0001-0001-0001-000000000005', '04/25', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Tag der Befreiung', 'Liberation Day', 'Jour de la Libération', 'Festa della Liberazione'),
('10000001-0001-0001-0001-000000000006', '05/01', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del Lavoro'),
('10000001-0001-0001-0001-000000000007', '06/02', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Tag der Republik', 'Republic Day', 'Fête de la République', 'Festa della Repubblica'),
('10000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Ferragosto'),
('10000001-0001-0001-0001-000000000009', '11/01', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('10000001-0001-0001-0001-000000000010', '12/08', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Mariä Empfängnis', 'Immaculate Conception', 'Immaculée Conception', 'Immacolata Concezione'),
('10000001-0001-0001-0001-000000000011', '12/25', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Weihnachten', 'Christmas Day', 'Noël', 'Natale'),
('10000001-0001-0001-0001-000000000012', '12/26', '', true, true, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano');

-- ITALIEN - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('10000001-0001-0001-0001-000000000013', '12/24', '', false, false, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('10000001-0001-0001-0001-000000000014', '12/31', '', false, false, 'IT', 'IT', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');

-- #####################################################
-- FRANKREICH (FR)
-- Nationale Feiertage gelten landesweit
-- Elsass-Lothringen (67, 68, 57) hat zusätzliche Feiertage
-- #####################################################

-- =====================================================
-- FRANKREICH - NATIONALE FEIERTAGE (FR FR)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('f0000001-0001-0001-0001-000000000001', '01/01', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
('f0000001-0001-0001-0001-000000000002', 'EASTER+01', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Ostermontag', 'Easter Monday', 'Lundi de Pâques', 'Lunedì di Pasqua'),
('f0000001-0001-0001-0001-000000000003', '05/01', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Tag der Arbeit', 'Labour Day', 'Fête du Travail', 'Festa del lavoro'),
('f0000001-0001-0001-0001-000000000004', '05/08', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Tag des Sieges', 'Victory Day', 'Victoire 1945', 'Giorno della Vittoria'),
('f0000001-0001-0001-0001-000000000005', 'EASTER+39', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Christi Himmelfahrt', 'Ascension Day', 'Ascension', 'Ascensione'),
('f0000001-0001-0001-0001-000000000006', 'EASTER+50', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Pfingstmontag', 'Whit Monday', 'Lundi de Pentecôte', 'Lunedì di Pentecoste'),
('f0000001-0001-0001-0001-000000000007', '07/14', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Nationalfeiertag', 'Bastille Day', 'Fête nationale', 'Presa della Bastiglia'),
('f0000001-0001-0001-0001-000000000008', '08/15', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Mariä Himmelfahrt', 'Assumption of Mary', 'Assomption', 'Assunzione di Maria'),
('f0000001-0001-0001-0001-000000000009', '11/01', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Allerheiligen', 'All Saints'' Day', 'Toussaint', 'Ognissanti'),
('f0000001-0001-0001-0001-000000000010', '11/11', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Waffenstillstand', 'Armistice Day', 'Armistice', 'Armistizio'),
('f0000001-0001-0001-0001-000000000011', '12/25', '', true, true, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Weihnachten', 'Christmas Day', 'Noël', 'Natale');

-- =====================================================
-- FRANKREICH - ELSASS-MOSEL ZUSÄTZLICHE FEIERTAGE
-- Départements: 57 (Moselle), 67 (Bas-Rhin), 68 (Haut-Rhin)
-- =====================================================
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('f0570001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '57', 'FR', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('f0570001-0001-0001-0001-000000000002', '12/26', '', true, true, '57', 'FR', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('f0670001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '67', 'FR', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('f0670001-0001-0001-0001-000000000002', '12/26', '', true, true, '67', 'FR', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano'),
('f0680001-0001-0001-0001-000000000001', 'EASTER-02', '', true, true, '68', 'FR', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
('f0680001-0001-0001-0001-000000000002', '12/26', '', true, true, '68', 'FR', NULL, NULL, NULL, NULL, 'Stephanstag', 'St. Stephen''s Day', 'Saint-Étienne', 'Santo Stefano');

-- FRANKREICH - HALBE TAGE
INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
('f0000001-0001-0001-0001-000000000012', '12/24', '', false, false, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Heiligabend', 'Christmas Eve', 'Veille de Noël', 'Vigilia di Natale'),
('f0000001-0001-0001-0001-000000000013', '12/31', '', false, false, 'FR', 'FR', NULL, NULL, NULL, NULL, 'Silvester', 'New Year''s Eve', 'Saint-Sylvestre', 'San Silvestro');
"
            );
        }
    }
}
