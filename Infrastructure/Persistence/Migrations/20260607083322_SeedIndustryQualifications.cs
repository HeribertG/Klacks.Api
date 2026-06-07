using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedIndustryQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO qualification (id, name, emoji, is_time_limited, is_deleted, create_time, current_user_created, current_user_updated, current_user_deleted)
                VALUES
                -- Spitex / Home Care
                ('53d33bd0-cd52-48e9-ac0e-618bc94760c7', '{""de"":""Pflegeassistenz SRK"",""en"":""Nursing Assistant CRS"",""fr"":""Assistant(e) en soins CRS"",""it"":""Assistente di cura CRS""}'::jsonb, '🏥', false, false, NOW(), 'seed', '', ''),
                ('72a86d8b-fc16-49da-b5d9-e6feee81c329', '{""de"":""Fachfrau/-mann Gesundheit (FaGe)"",""en"":""Healthcare Worker (FaGe)"",""fr"":""Aide en soins et santé communautaire (ASSC)"",""it"":""Operatore/operatrice sociosanitario (OSS)""}'::jsonb, '💊', false, false, NOW(), 'seed', '', ''),
                ('eb12579a-9262-44a6-be60-d3c9be2954f6', '{""de"":""HF Pflege"",""en"":""Diploma in Nursing (HF)"",""fr"":""Diplôme en soins infirmiers (ES)"",""it"":""Diploma in cure infermieristiche (SSS)""}'::jsonb, '🎓', false, false, NOW(), 'seed', '', ''),
                ('d75c138d-23f1-4064-a3b7-5c202d9e9363', '{""de"":""BLS-AED (Reanimation)"",""en"":""BLS-AED (Basic Life Support)"",""fr"":""BLS-AED (Support vital de base)"",""it"":""BLS-AED (Supporto vitale di base)""}'::jsonb, '❤️', true, false, NOW(), 'seed', '', ''),
                ('cd74f2e9-add7-42f5-b707-3caf1597a25e', '{""de"":""Wundversorgung"",""en"":""Wound Care"",""fr"":""Soins des plaies"",""it"":""Cura delle ferite""}'::jsonb, '🩹', false, false, NOW(), 'seed', '', ''),
                ('a8bb8129-a47b-4b7b-be2c-4c3b918b19ea', '{""de"":""Injektionen setzen"",""en"":""Administering Injections"",""fr"":""Administration d''injections"",""it"":""Somministrazione di iniezioni""}'::jsonb, '💉', false, false, NOW(), 'seed', '', ''),
                ('ac78925e-5019-40be-8db6-d0d20f444171', '{""de"":""Katheter-Pflege"",""en"":""Catheter Care"",""fr"":""Soins de cathéter"",""it"":""Cura del catetere""}'::jsonb, '🩺', false, false, NOW(), 'seed', '', ''),
                ('75b24b9c-4a48-4818-be68-4f65744463d8', '{""de"":""Stomapflege"",""en"":""Stoma Care"",""fr"":""Soins de stomie"",""it"":""Cura della stomia""}'::jsonb, '🩺', false, false, NOW(), 'seed', '', ''),
                ('d98105e2-b60c-4d37-9659-8d685c514ebc', '{""de"":""Medikamentenabgabe"",""en"":""Medication Administration"",""fr"":""Administration des médicaments"",""it"":""Somministrazione farmaci""}'::jsonb, '💊', false, false, NOW(), 'seed', '', ''),
                ('18c4ae01-7e43-46b4-a4e4-bd12437bd5e3', '{""de"":""Demenzpflege"",""en"":""Dementia Care"",""fr"":""Soins aux personnes atteintes de démence"",""it"":""Cura della demenza""}'::jsonb, '🧠', false, false, NOW(), 'seed', '', ''),
                ('b702a794-c83c-4d94-a3fa-f28b850b79e6', '{""de"":""Palliativpflege"",""en"":""Palliative Care"",""fr"":""Soins palliatifs"",""it"":""Cure palliative""}'::jsonb, '🕊️', false, false, NOW(), 'seed', '', ''),
                ('3fd89ae5-21e6-474a-87de-07fdbbe82c41', '{""de"":""Patientenmobilisierung / Transfers"",""en"":""Patient Mobilisation / Transfers"",""fr"":""Mobilisation des patients / Transferts"",""it"":""Mobilizzazione del paziente / Trasferimenti""}'::jsonb, '🦺', false, false, NOW(), 'seed', '', ''),
                ('1190a2fd-c313-457f-b186-3b189ca59344', '{""de"":""Ernährungssonde (PEG)"",""en"":""Feeding Tube (PEG)"",""fr"":""Sonde d''alimentation (PEG)"",""it"":""Sonda di alimentazione (PEG)""}'::jsonb, '🍽️', false, false, NOW(), 'seed', '', ''),
                ('239c2e84-49d6-4023-b982-3658cfc85b4c', '{""de"":""Nothelferkurs"",""en"":""First Aid Course"",""fr"":""Cours de secourisme"",""it"":""Corso di primo soccorso""}'::jsonb, '🩹', true, false, NOW(), 'seed', '', ''),

                -- Sicherheitsbranche / Security
                ('ea7b299d-96fd-47c2-83f3-e5ba340e4b73', '{""de"":""Berufsausweis Sicherheitsassistent/in (SVSS)"",""en"":""Security Professional Certificate (SVSS)"",""fr"":""Certificat professionnel agent de sécurité"",""it"":""Certificato professionale agente di sicurezza""}'::jsonb, '🪪', true, false, NOW(), 'seed', '', ''),
                ('cb3b1969-9c8b-4b51-b9d0-a534d3073f87', '{""de"":""Waffenberechtigung"",""en"":""Firearms Authorisation"",""fr"":""Autorisation de port d''arme"",""it"":""Autorizzazione all''uso di armi da fuoco""}'::jsonb, '🔫', true, false, NOW(), 'seed', '', ''),
                ('5da22bab-3fa8-40e0-90e7-ddbf5289e9bf', '{""de"":""Personenschutz"",""en"":""Bodyguard / Personal Protection"",""fr"":""Protection rapprochée"",""it"":""Protezione personale""}'::jsonb, '🛡️', false, false, NOW(), 'seed', '', ''),
                ('76688c0e-4a46-43b9-a321-37ce4243e3d3', '{""de"":""Deeskalationstraining"",""en"":""De-escalation Training"",""fr"":""Formation à la désescalade"",""it"":""Formazione alla de-escalation""}'::jsonb, '🤝', true, false, NOW(), 'seed', '', ''),
                ('022a55a6-122e-4b6e-be57-8d9a3a173c75', '{""de"":""Zugangskontrolle"",""en"":""Access Control"",""fr"":""Contrôle d''accès"",""it"":""Controllo accessi""}'::jsonb, '🚦', false, false, NOW(), 'seed', '', ''),
                ('72c25a08-fcde-4183-a657-ad0217d38694', '{""de"":""CCTV / Videoüberwachung"",""en"":""CCTV / Video Surveillance"",""fr"":""CCTV / Vidéosurveillance"",""it"":""CCTV / Videosorveglianza""}'::jsonb, '📹', false, false, NOW(), 'seed', '', ''),
                ('78310b09-3a61-483f-b1bd-e69a761ca207', '{""de"":""Brandschutz"",""en"":""Fire Protection"",""fr"":""Protection incendie"",""it"":""Protezione antincendio""}'::jsonb, '🔥', true, false, NOW(), 'seed', '', ''),
                ('0034f092-0355-4552-9e86-8e3ac3b5b3b2', '{""de"":""Hundeführer (K9)"",""en"":""Dog Handler (K9)"",""fr"":""Maître-chien (K9)"",""it"":""Conduttore cinofilo (K9)""}'::jsonb, '🐕', true, false, NOW(), 'seed', '', ''),
                ('3d4b9396-25b8-47a8-8202-668f8f81030e', '{""de"":""Eventschutz"",""en"":""Event Security"",""fr"":""Sécurité événementielle"",""it"":""Sicurezza eventi""}'::jsonb, '🎪', false, false, NOW(), 'seed', '', ''),

                -- Spedition / Logistics
                ('9a185d56-180a-4450-8aee-ea98ac897f49', '{""de"":""Führerausweis C / CE (LKW / Sattelzug)"",""en"":""Driving Licence C / CE (HGV / Articulated)"",""fr"":""Permis de conduire C / CE (PL / Semi-remorque)"",""it"":""Patente C / CE (Autocarro / Autoarticolato)""}'::jsonb, '🚛', true, false, NOW(), 'seed', '', ''),
                ('88800ec7-b4fb-471c-ac30-2b759743cf67', '{""de"":""ADR-Schein (Gefahrgutstransport)"",""en"":""ADR Certificate (Dangerous Goods Transport)"",""fr"":""Certificat ADR (Transport de matières dangereuses)"",""it"":""Certificato ADR (Trasporto di merci pericolose)""}'::jsonb, '☢️', true, false, NOW(), 'seed', '', ''),
                ('6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd', '{""de"":""Staplerschein"",""en"":""Forklift Licence"",""fr"":""Certificat de cariste"",""it"":""Patente per muletto""}'::jsonb, '🏗️', true, false, NOW(), 'seed', '', ''),
                ('19f57576-eb75-417f-986d-a33cc83b6502', '{""de"":""Kranbedienung"",""en"":""Crane Operation"",""fr"":""Conduite de grue"",""it"":""Conduzione gru""}'::jsonb, '🏗️', true, false, NOW(), 'seed', '', ''),
                ('99391d2c-6dc2-419a-b789-8d1e2032c7b4', '{""de"":""Ladungssicherung"",""en"":""Load Securing"",""fr"":""Arrimage des charges"",""it"":""Sicurezza del carico""}'::jsonb, '🔒', true, false, NOW(), 'seed', '', ''),
                ('3ff965ae-6cde-46e1-8094-f51889fb64eb', '{""de"":""IATA Gefahrgut Luft"",""en"":""IATA Dangerous Goods Air (DGR)"",""fr"":""IATA Marchandises dangereuses aérien"",""it"":""IATA Merci pericolose aereo""}'::jsonb, '✈️', true, false, NOW(), 'seed', '', ''),
                ('1a9a21b3-535d-4bfa-ab88-272c4b9f3810', '{""de"":""IMDG (Gefahrgut See)"",""en"":""IMDG (Dangerous Goods by Sea)"",""fr"":""IMDG (Marchandises dangereuses par mer)"",""it"":""IMDG (Merci pericolose via mare)""}'::jsonb, '🚢', true, false, NOW(), 'seed', '', ''),
                ('ec71642b-facc-4b6c-a0ef-1b3e41030f64', '{""de"":""Digitaler Tachograph"",""en"":""Digital Tachograph"",""fr"":""Tachygraphe numérique"",""it"":""Tachigrafo digitale""}'::jsonb, '📟', false, false, NOW(), 'seed', '', ''),
                ('d119c40e-3390-4aac-bd15-bd869629039d', '{""de"":""Zollabwicklung"",""en"":""Customs Clearance"",""fr"":""Dédouanement"",""it"":""Sdoganamento""}'::jsonb, '📋', false, false, NOW(), 'seed', '', ''),
                ('b9543ec0-6281-4161-b0f8-7f036d48ddae', '{""de"":""HACCP (Lebensmittellogistik)"",""en"":""HACCP (Food Logistics)"",""fr"":""HACCP (Logistique alimentaire)"",""it"":""HACCP (Logistica alimentare)""}'::jsonb, '🌡️', true, false, NOW(), 'seed', '', ''),
                ('0c562163-931d-4982-bfcc-0388bec2ef9f', '{""de"":""Kühlkette"",""en"":""Cold Chain Management"",""fr"":""Gestion de la chaîne du froid"",""it"":""Gestione della catena del freddo""}'::jsonb, '❄️', false, false, NOW(), 'seed', '', ''),
                ('90d38d22-a05e-46cb-a4e2-deea6d3cc9f4', '{""de"":""Schwertransport-Genehmigung"",""en"":""Heavy Transport Permit"",""fr"":""Autorisation de transport exceptionnel"",""it"":""Autorizzazione trasporto eccezionale""}'::jsonb, '⚖️', true, false, NOW(), 'seed', '', '')
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM qualification WHERE id IN (
                    '53d33bd0-cd52-48e9-ac0e-618bc94760c7',
                    '72a86d8b-fc16-49da-b5d9-e6feee81c329',
                    'eb12579a-9262-44a6-be60-d3c9be2954f6',
                    'd75c138d-23f1-4064-a3b7-5c202d9e9363',
                    'cd74f2e9-add7-42f5-b707-3caf1597a25e',
                    'a8bb8129-a47b-4b7b-be2c-4c3b918b19ea',
                    'ac78925e-5019-40be-8db6-d0d20f444171',
                    '75b24b9c-4a48-4818-be68-4f65744463d8',
                    'd98105e2-b60c-4d37-9659-8d685c514ebc',
                    '18c4ae01-7e43-46b4-a4e4-bd12437bd5e3',
                    'b702a794-c83c-4d94-a3fa-f28b850b79e6',
                    '3fd89ae5-21e6-474a-87de-07fdbbe82c41',
                    '1190a2fd-c313-457f-b186-3b189ca59344',
                    '239c2e84-49d6-4023-b982-3658cfc85b4c',
                    'ea7b299d-96fd-47c2-83f3-e5ba340e4b73',
                    'cb3b1969-9c8b-4b51-b9d0-a534d3073f87',
                    '5da22bab-3fa8-40e0-90e7-ddbf5289e9bf',
                    '76688c0e-4a46-43b9-a321-37ce4243e3d3',
                    '022a55a6-122e-4b6e-be57-8d9a3a173c75',
                    '72c25a08-fcde-4183-a657-ad0217d38694',
                    '78310b09-3a61-483f-b1bd-e69a761ca207',
                    '0034f092-0355-4552-9e86-8e3ac3b5b3b2',
                    '3d4b9396-25b8-47a8-8202-668f8f81030e',
                    '9a185d56-180a-4450-8aee-ea98ac897f49',
                    '88800ec7-b4fb-471c-ac30-2b759743cf67',
                    '6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd',
                    '19f57576-eb75-417f-986d-a33cc83b6502',
                    '99391d2c-6dc2-419a-b789-8d1e2032c7b4',
                    '3ff965ae-6cde-46e1-8094-f51889fb64eb',
                    '1a9a21b3-535d-4bfa-ab88-272c4b9f3810',
                    'ec71642b-facc-4b6c-a0ef-1b3e41030f64',
                    'd119c40e-3390-4aac-bd15-bd869629039d',
                    'b9543ec0-6281-4161-b0f8-7f036d48ddae',
                    '0c562163-931d-4982-bfcc-0388bec2ef9f',
                    '90d38d22-a05e-46cb-a4e2-deea6d3cc9f4'
                );
            ");
        }
    }
}
