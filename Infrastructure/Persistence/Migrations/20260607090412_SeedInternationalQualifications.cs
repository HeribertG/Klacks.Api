using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInternationalQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO qualification (id, name, emoji, is_time_limited, is_deleted, create_time, current_user_created, current_user_updated, current_user_deleted)
                VALUES

                -- Languages
                ('3f39546f-5573-44a9-8260-4bd28574eadb', '{""de"":""JLPT N3 (Japanisch)"",""en"":""JLPT N3 (Japanese Language Proficiency)"",""fr"":""JLPT N3 (Compétence en langue japonaise)"",""it"":""JLPT N3 (Competenza in lingua giapponese)""}'::jsonb, '🇯🇵', false, false, NOW(), 'seed', '', ''),
                ('e037374a-1a39-4020-8a13-ee1051dd359f', '{""de"":""Arabischkenntnisse"",""en"":""Arabic Language Skills"",""fr"":""Compétences en arabe"",""it"":""Conoscenza della lingua araba""}'::jsonb, '🇸🇦', false, false, NOW(), 'seed', '', ''),
                ('cef4f851-2ce1-4c86-8367-5fb890c98334', '{""de"":""Ivrit-Kenntnisse (Hebräisch)"",""en"":""Hebrew Language Skills"",""fr"":""Compétences en hébreu"",""it"":""Conoscenza della lingua ebraica""}'::jsonb, '🇮🇱', false, false, NOW(), 'seed', '', ''),
                ('178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4', '{""de"":""Mandarin-Kenntnisse (HSK)"",""en"":""Mandarin Language Skills (HSK)"",""fr"":""Compétences en mandarin (HSK)"",""it"":""Conoscenza del mandarino (HSK)""}'::jsonb, '🇨🇳', false, false, NOW(), 'seed', '', ''),
                ('0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e', '{""de"":""Koreanisch (TOPIK)"",""en"":""Korean Language Skills (TOPIK)"",""fr"":""Compétences en coréen (TOPIK)"",""it"":""Conoscenza della lingua coreana (TOPIK)""}'::jsonb, '🇰🇷', false, false, NOW(), 'seed', '', ''),

                -- Spitex / Home Care
                ('dc70fef8-2822-430e-9d25-e6a709984d7e', '{""de"":""EU-Berufsanerkennung Pflege (Richtlinie 2005/36/EG)"",""en"":""EU Professional Recognition – Nursing (Directive 2005/36/EC)"",""fr"":""Reconnaissance professionnelle UE – Soins infirmiers (Directive 2005/36/CE)"",""it"":""Riconoscimento professionale UE – Infermieristica (Direttiva 2005/36/CE)""}'::jsonb, '🇪🇺', false, false, NOW(), 'seed', '', ''),
                ('4df5b030-7650-4f6d-93cf-0d5e29658bac', '{""de"":""介護福祉士 Kaigo Fukushishi (Japan Pflegeexamen)"",""en"":""介護福祉士 Kaigo Fukushishi (Japan Care Worker Exam)"",""fr"":""介護福祉士 Kaigo Fukushishi (Examen de soignant Japon)"",""it"":""介護福祉士 Kaigo Fukushishi (Esame operatore sanitario Giappone)""}'::jsonb, '🇯🇵', false, false, NOW(), 'seed', '', ''),
                ('5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1', '{""de"":""SCFHS-Registrierung (Saudi-Arabien)"",""en"":""SCFHS Registration (Saudi Arabia)"",""fr"":""Inscription SCFHS (Arabie saoudite)"",""it"":""Registrazione SCFHS (Arabia Saudita)""}'::jsonb, '🇸🇦', true, false, NOW(), 'seed', '', ''),
                ('68d28df9-113a-40cf-bf6b-3c9bb18660e3', '{""de"":""DataFlow Credential Verification (Naher Osten)"",""en"":""DataFlow Credential Verification (Middle East)"",""fr"":""Vérification des diplômes DataFlow (Moyen-Orient)"",""it"":""Verifica credenziali DataFlow (Medio Oriente)""}'::jsonb, '📄', true, false, NOW(), 'seed', '', ''),
                ('b9154bfa-5abb-4cd7-9f3b-16a339996add', '{""de"":""MOH-Lizenz Israel (Misrad HaBriut)"",""en"":""MOH Licence Israel (Ministry of Health)"",""fr"":""Licence MOH Israël (Ministère de la Santé)"",""it"":""Licenza MOH Israele (Ministero della Salute)""}'::jsonb, '🇮🇱', true, false, NOW(), 'seed', '', ''),
                ('e3201f77-d5f2-4a8b-af49-8214b3eeda95', '{""de"":""China NHC Pflegeregistrierung (国家护士执业注册)"",""en"":""China NHC Nursing Registration (国家护士执业注册)"",""fr"":""Enregistrement infirmier NHC Chine (国家护士执业注册)"",""it"":""Registrazione infermieristica NHC Cina (国家护士执业注册)""}'::jsonb, '🇨🇳', true, false, NOW(), 'seed', '', ''),
                ('c5ecea26-4bd7-4080-a728-aa26ebd7b226', '{""de"":""Taiwan Nursing License (護理師執照)"",""en"":""Taiwan Nursing Licence (護理師執照)"",""fr"":""Licence infirmière Taïwan (護理師執照)"",""it"":""Licenza infermieristica Taiwan (護理師執照)""}'::jsonb, '🇹🇼', true, false, NOW(), 'seed', '', ''),
                ('40b1bcf6-bb79-40aa-9f59-f60aa381021a', '{""de"":""요양보호사 Yoyangbohosa (Korea Pflegeassistenz)"",""en"":""요양보호사 Yoyangbohosa (Korean Care Worker Licence)"",""fr"":""요양보호사 Yoyangbohosa (Licence aide-soignant Corée)"",""it"":""요양보호사 Yoyangbohosa (Licenza operatore sanitario Corea)""}'::jsonb, '🇰🇷', false, false, NOW(), 'seed', '', ''),

                -- Security
                ('b257acc2-67ea-4b81-9038-f774c863962e', '{""de"":""§34a-Sachkundeprüfung (Deutschland/EU)"",""en"":""§34a Proficiency Test (Germany/EU Security)"",""fr"":""Examen de compétence §34a (Allemagne/UE)"",""it"":""Esame di competenza §34a (Germania/UE)""}'::jsonb, '🇩🇪', true, false, NOW(), 'seed', '', ''),
                ('e8354115-4462-4c0d-8fce-a35369e967cb', '{""de"":""警備員検定 Keibi-in Kentei (Japan Sicherheitszertifikat)"",""en"":""警備員検定 Keibi-in Kentei (Japan Security Guard Certification)"",""fr"":""警備員検定 Keibi-in Kentei (Certification agent de sécurité Japon)"",""it"":""警備員検定 Keibi-in Kentei (Certificazione guardia di sicurezza Giappone)""}'::jsonb, '🇯🇵', true, false, NOW(), 'seed', '', ''),
                ('8ddfb12f-0373-4c24-ab56-1390d2895e39', '{""de"":""Saudi MOI Sicherheitsgenehmigung"",""en"":""Saudi MOI Security Clearance"",""fr"":""Autorisation sécurité MOI Arabie saoudite"",""it"":""Autorizzazione sicurezza MOI Arabia Saudita""}'::jsonb, '🇸🇦', true, false, NOW(), 'seed', '', ''),
                ('f7521782-553d-4cf2-9e81-7ec88c51af60', '{""de"":""Israelischer Sicherheitsbeauftragter-Ausweis"",""en"":""Israeli Security Officer Licence"",""fr"":""Licence agent de sécurité israélien"",""it"":""Licenza agente di sicurezza israeliano""}'::jsonb, '🇮🇱', true, false, NOW(), 'seed', '', ''),
                ('0127ea12-fa4b-4a11-8dd7-b78d93e65779', '{""de"":""China PSB Sicherheitsausweis (保安员证)"",""en"":""China PSB Security Permit (保安员证)"",""fr"":""Permis de sécurité PSB Chine (保安员证)"",""it"":""Permesso di sicurezza PSB Cina (保安员证)""}'::jsonb, '🇨🇳', true, false, NOW(), 'seed', '', ''),
                ('bd8f022d-0f17-4c11-bc97-151015b3b5d2', '{""de"":""Korean Security Guard License (경비원 자격증)"",""en"":""Korean Security Guard Licence (경비원 자격증)"",""fr"":""Licence agent de sécurité coréen (경비원 자격증)"",""it"":""Licenza guardia di sicurezza coreana (경비원 자격증)""}'::jsonb, '🇰🇷', true, false, NOW(), 'seed', '', ''),
                ('917f486e-e48a-4e11-bd68-a531702b20a8', '{""de"":""Taiwan Security Guard License (保全人員執照)"",""en"":""Taiwan Security Guard Licence (保全人員執照)"",""fr"":""Licence agent de sécurité taïwanais (保全人員執照)"",""it"":""Licenza guardia di sicurezza taiwanese (保全人員執照)""}'::jsonb, '🇹🇼', true, false, NOW(), 'seed', '', ''),

                -- Logistics / Spedition
                ('9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c', '{""de"":""Code 95 / EU-Berufskraftfahrerqualifikation (BKF)"",""en"":""Code 95 / EU Certificate of Professional Competence (CPC)"",""fr"":""Code 95 / Qualification professionnelle conducteur UE (CPC)"",""it"":""Codice 95 / Qualifica professionale autista UE (CPC)""}'::jsonb, '🇪🇺', true, false, NOW(), 'seed', '', ''),
                ('17615f16-9d2b-42e7-9e61-b1f0a407bb3b', '{""de"":""Japanischer Führerschein (JAF)"",""en"":""Japanese Driving Licence (JAF)"",""fr"":""Permis de conduire japonais (JAF)"",""it"":""Patente di guida giapponese (JAF)""}'::jsonb, '🇯🇵', false, false, NOW(), 'seed', '', ''),
                ('99dfebb6-0054-4758-83aa-d17439647144', '{""de"":""Halal-Logistik-Zertifizierung"",""en"":""Halal Logistics Certification"",""fr"":""Certification logistique halal"",""it"":""Certificazione logistica halal""}'::jsonb, '🌙', true, false, NOW(), 'seed', '', ''),
                ('0571b080-ec96-41d7-9852-e49ac432ab83', '{""de"":""Cargo Security Protokoll Ben-Gurion (IOSA)"",""en"":""Ben-Gurion Cargo Security Protocol (IOSA)"",""fr"":""Protocole sécurité fret Ben Gourion (IOSA)"",""it"":""Protocollo sicurezza merci Ben Gurion (IOSA)""}'::jsonb, '🇮🇱', true, false, NOW(), 'seed', '', ''),
                ('abaef6e9-0172-4acd-9204-ff5980e976c2', '{""de"":""Chinesischer Führerschein (中国驾照)"",""en"":""Chinese Driving Licence (中国驾照)"",""fr"":""Permis de conduire chinois (中国驾照)"",""it"":""Patente di guida cinese (中国驾照)""}'::jsonb, '🇨🇳', false, false, NOW(), 'seed', '', ''),
                ('1b0e9baa-76b5-4d20-93ae-adcf181892a3', '{""de"":""AEO-Zertifizierung (Zugelassener Wirtschaftsbeteiligter)"",""en"":""AEO Certification (Authorised Economic Operator)"",""fr"":""Certification OEA (Opérateur économique agréé)"",""it"":""Certificazione OEA (Operatore economico autorizzato)""}'::jsonb, '📦', true, false, NOW(), 'seed', '', '')

                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM qualification WHERE id IN (
                    '3f39546f-5573-44a9-8260-4bd28574eadb',
                    'e037374a-1a39-4020-8a13-ee1051dd359f',
                    'cef4f851-2ce1-4c86-8367-5fb890c98334',
                    '178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4',
                    '0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e',
                    'dc70fef8-2822-430e-9d25-e6a709984d7e',
                    '4df5b030-7650-4f6d-93cf-0d5e29658bac',
                    '5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1',
                    '68d28df9-113a-40cf-bf6b-3c9bb18660e3',
                    'b9154bfa-5abb-4cd7-9f3b-16a339996add',
                    'e3201f77-d5f2-4a8b-af49-8214b3eeda95',
                    'c5ecea26-4bd7-4080-a728-aa26ebd7b226',
                    '40b1bcf6-bb79-40aa-9f59-f60aa381021a',
                    'b257acc2-67ea-4b81-9038-f774c863962e',
                    'e8354115-4462-4c0d-8fce-a35369e967cb',
                    '8ddfb12f-0373-4c24-ab56-1390d2895e39',
                    'f7521782-553d-4cf2-9e81-7ec88c51af60',
                    '0127ea12-fa4b-4a11-8dd7-b78d93e65779',
                    'bd8f022d-0f17-4c11-bc97-151015b3b5d2',
                    '917f486e-e48a-4e11-bd68-a531702b20a8',
                    '9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c',
                    '17615f16-9d2b-42e7-9e61-b1f0a407bb3b',
                    '99dfebb6-0054-4758-83aa-d17439647144',
                    '0571b080-ec96-41d7-9852-e49ac432ab83',
                    'abaef6e9-0172-4acd-9204-ff5980e976c2',
                    '1b0e9baa-76b5-4d20-93ae-adcf181892a3'
                );
            ");
        }
    }
}
