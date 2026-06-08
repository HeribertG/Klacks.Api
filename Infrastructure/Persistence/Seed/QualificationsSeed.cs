// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default qualification seed for all supported industries and regions.
/// Covers home care (Spitex), security, logistics, healthcare, gastronomy, construction, cleaning, and transport.
/// After inserting qualifications, seeds the qualification_country junction table so country-based
/// filters work correctly on a fresh database without relying on migration data steps.
/// </summary>

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class QualificationsSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO qualification (id, name, emoji, is_time_limited, type, category, is_deleted, create_time, current_user_created, current_user_updated, current_user_deleted)
                VALUES

                -- Languages (type = 1, category = 0)
                ('744d6e64-3ddf-41d0-87c8-30b690c451d9', '{""de"":""Deutsch"",""en"":""German"",""fr"":""Allemand"",""it"":""Tedesco""}'::jsonb, '🇩🇪', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', '{""de"":""Englisch"",""en"":""English"",""fr"":""Anglais"",""it"":""Inglese""}'::jsonb, '🇬🇧', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('664351fc-0c5a-4610-9c05-f5faf778ff90', '{""de"":""Französisch"",""en"":""French"",""fr"":""Français"",""it"":""Francese""}'::jsonb, '🇫🇷', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('4a272d5a-d188-4b1e-9631-5870171c8d8f', '{""de"":""Italienisch"",""en"":""Italian"",""fr"":""Italien"",""it"":""Italiano""}'::jsonb, '🇮🇹', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('3f39546f-5573-44a9-8260-4bd28574eadb', '{""de"":""JLPT N3 (Japanisch)"",""en"":""JLPT N3 (Japanese Language Proficiency)"",""fr"":""JLPT N3 (Compétence en langue japonaise)"",""it"":""JLPT N3 (Competenza in lingua giapponese)""}'::jsonb, '🇯🇵', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('e037374a-1a39-4020-8a13-ee1051dd359f', '{""de"":""Arabischkenntnisse"",""en"":""Arabic Language Skills"",""fr"":""Compétences en arabe"",""it"":""Conoscenza della lingua araba""}'::jsonb, '🇸🇦', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('cef4f851-2ce1-4c86-8367-5fb890c98334', '{""de"":""Ivrit-Kenntnisse (Hebräisch)"",""en"":""Hebrew Language Skills"",""fr"":""Compétences en hébreu"",""it"":""Conoscenza della lingua ebraica""}'::jsonb, '🇮🇱', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4', '{""de"":""Mandarin-Kenntnisse (HSK)"",""en"":""Mandarin Language Skills (HSK)"",""fr"":""Compétences en mandarin (HSK)"",""it"":""Conoscenza del mandarino (HSK)""}'::jsonb, '🇨🇳', false, 1, 0, false, NOW(), 'seed', '', ''),
                ('0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e', '{""de"":""Koreanisch (TOPIK)"",""en"":""Korean Language Skills (TOPIK)"",""fr"":""Compétences en coréen (TOPIK)"",""it"":""Conoscenza della lingua coreana (TOPIK)""}'::jsonb, '🇰🇷', false, 1, 0, false, NOW(), 'seed', '', ''),

                -- General (type = 2, category = 0)
                ('fa3e3d7c-8384-4281-b1d8-0cfafdd56e16', '{""de"":""Führerschein"",""en"":""Driving licence"",""fr"":""Permis de conduire"",""it"":""Patente di guida""}'::jsonb, '🚗', false, 2, 0, false, NOW(), 'seed', '', ''),

                -- Spitex / Home Care – CH (type = 2, category = 1)
                ('53d33bd0-cd52-48e9-ac0e-618bc94760c7', '{""de"":""Pflegeassistenz SRK"",""en"":""Nursing Assistant CRS"",""fr"":""Assistant(e) en soins CRS"",""it"":""Assistente di cura CRS""}'::jsonb, '🏥', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('72a86d8b-fc16-49da-b5d9-e6feee81c329', '{""de"":""Fachfrau/-mann Gesundheit (FaGe)"",""en"":""Healthcare Worker (FaGe)"",""fr"":""Aide en soins et santé communautaire (ASSC)"",""it"":""Operatore/operatrice sociosanitario (OSS)""}'::jsonb, '💊', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('eb12579a-9262-44a6-be60-d3c9be2954f6', '{""de"":""HF Pflege"",""en"":""Diploma in Nursing (HF)"",""fr"":""Diplôme en soins infirmiers (ES)"",""it"":""Diploma in cure infermieristiche (SSS)""}'::jsonb, '🎓', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('d75c138d-23f1-4064-a3b7-5c202d9e9363', '{""de"":""BLS-AED (Reanimation)"",""en"":""BLS-AED (Basic Life Support)"",""fr"":""BLS-AED (Support vital de base)"",""it"":""BLS-AED (Supporto vitale di base)""}'::jsonb, '❤️', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('cd74f2e9-add7-42f5-b707-3caf1597a25e', '{""de"":""Wundversorgung"",""en"":""Wound Care"",""fr"":""Soins des plaies"",""it"":""Cura delle ferite""}'::jsonb, '🩹', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('a8bb8129-a47b-4b7b-be2c-4c3b918b19ea', '{""de"":""Injektionen setzen"",""en"":""Administering Injections"",""fr"":""Administration d''''injections"",""it"":""Somministrazione di iniezioni""}'::jsonb, '💉', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('ac78925e-5019-40be-8db6-d0d20f444171', '{""de"":""Katheter-Pflege"",""en"":""Catheter Care"",""fr"":""Soins de cathéter"",""it"":""Cura del catetere""}'::jsonb, '🩺', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('75b24b9c-4a48-4818-be68-4f65744463d8', '{""de"":""Stomapflege"",""en"":""Stoma Care"",""fr"":""Soins de stomie"",""it"":""Cura della stomia""}'::jsonb, '🩺', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('d98105e2-b60c-4d37-9659-8d685c514ebc', '{""de"":""Medikamentenabgabe"",""en"":""Medication Administration"",""fr"":""Administration des médicaments"",""it"":""Somministrazione farmaci""}'::jsonb, '💊', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('18c4ae01-7e43-46b4-a4e4-bd12437bd5e3', '{""de"":""Demenzpflege"",""en"":""Dementia Care"",""fr"":""Soins aux personnes atteintes de démence"",""it"":""Cura della demenza""}'::jsonb, '🧠', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('b702a794-c83c-4d94-a3fa-f28b850b79e6', '{""de"":""Palliativpflege"",""en"":""Palliative Care"",""fr"":""Soins palliatifs"",""it"":""Cure palliative""}'::jsonb, '🕊️', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('3fd89ae5-21e6-474a-87de-07fdbbe82c41', '{""de"":""Patientenmobilisierung / Transfers"",""en"":""Patient Mobilisation / Transfers"",""fr"":""Mobilisation des patients / Transferts"",""it"":""Mobilizzazione del paziente / Trasferimenti""}'::jsonb, '🦺', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('1190a2fd-c313-457f-b186-3b189ca59344', '{""de"":""Ernährungssonde (PEG)"",""en"":""Feeding Tube (PEG)"",""fr"":""Sonde d''''alimentation (PEG)"",""it"":""Sonda di alimentazione (PEG)""}'::jsonb, '🍽️', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('239c2e84-49d6-4023-b982-3658cfc85b4c', '{""de"":""Nothelferkurs"",""en"":""First Aid Course"",""fr"":""Cours de secourisme"",""it"":""Corso di primo soccorso""}'::jsonb, '🩹', true, 2, 1, false, NOW(), 'seed', '', ''),

                -- Spitex / Home Care – International (type = 2, category = 1)
                ('dc70fef8-2822-430e-9d25-e6a709984d7e', '{""de"":""EU-Berufsanerkennung Pflege (Richtlinie 2005/36/EG)"",""en"":""EU Professional Recognition – Nursing (Directive 2005/36/EC)"",""fr"":""Reconnaissance professionnelle UE – Soins infirmiers (Directive 2005/36/CE)"",""it"":""Riconoscimento professionale UE – Infermieristica (Direttiva 2005/36/CE)""}'::jsonb, '🇪🇺', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('4df5b030-7650-4f6d-93cf-0d5e29658bac', '{""de"":""介護福祉士 Kaigo Fukushishi (Japan Pflegeexamen)"",""en"":""介護福祉士 Kaigo Fukushishi (Japan Care Worker Exam)"",""fr"":""介護福祉士 Kaigo Fukushishi (Examen de soignant Japon)"",""it"":""介護福祉士 Kaigo Fukushishi (Esame operatore sanitario Giappone)""}'::jsonb, '🇯🇵', false, 2, 1, false, NOW(), 'seed', '', ''),
                ('5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1', '{""de"":""SCFHS-Registrierung (Saudi-Arabien)"",""en"":""SCFHS Registration (Saudi Arabia)"",""fr"":""Inscription SCFHS (Arabie saoudite)"",""it"":""Registrazione SCFHS (Arabia Saudita)""}'::jsonb, '🇸🇦', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('68d28df9-113a-40cf-bf6b-3c9bb18660e3', '{""de"":""DataFlow Credential Verification (Naher Osten)"",""en"":""DataFlow Credential Verification (Middle East)"",""fr"":""Vérification des diplômes DataFlow (Moyen-Orient)"",""it"":""Verifica credenziali DataFlow (Medio Oriente)""}'::jsonb, '📄', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('b9154bfa-5abb-4cd7-9f3b-16a339996add', '{""de"":""MOH-Lizenz Israel (Misrad HaBriut)"",""en"":""MOH Licence Israel (Ministry of Health)"",""fr"":""Licence MOH Israël (Ministère de la Santé)"",""it"":""Licenza MOH Israele (Ministero della Salute)""}'::jsonb, '🇮🇱', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('e3201f77-d5f2-4a8b-af49-8214b3eeda95', '{""de"":""China NHC Pflegeregistrierung (国家护士执业注册)"",""en"":""China NHC Nursing Registration (国家护士执业注册)"",""fr"":""Enregistrement infirmier NHC Chine (国家护士执业注册)"",""it"":""Registrazione infermieristica NHC Cina (国家护士执业注册)""}'::jsonb, '🇨🇳', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('c5ecea26-4bd7-4080-a728-aa26ebd7b226', '{""de"":""Taiwan Nursing License (護理師執照)"",""en"":""Taiwan Nursing Licence (護理師執照)"",""fr"":""Licence infirmière Taïwan (護理師執照)"",""it"":""Licenza infermieristica Taiwan (護理師執照)""}'::jsonb, '🇹🇼', true, 2, 1, false, NOW(), 'seed', '', ''),
                ('40b1bcf6-bb79-40aa-9f59-f60aa381021a', '{""de"":""요양보호사 Yoyangbohosa (Korea Pflegeassistenz)"",""en"":""요양보호사 Yoyangbohosa (Korean Care Worker Licence)"",""fr"":""요양보호사 Yoyangbohosa (Licence aide-soignant Corée)"",""it"":""요양보호사 Yoyangbohosa (Licenza operatore sanitario Corea)""}'::jsonb, '🇰🇷', false, 2, 1, false, NOW(), 'seed', '', ''),

                -- Security – CH (type = 2, category = 2)
                ('ea7b299d-96fd-47c2-83f3-e5ba340e4b73', '{""de"":""Berufsausweis Sicherheitsassistent/in (SVSS)"",""en"":""Security Professional Certificate (SVSS)"",""fr"":""Certificat professionnel agent de sécurité"",""it"":""Certificato professionale agente di sicurezza""}'::jsonb, '🪪', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('cb3b1969-9c8b-4b51-b9d0-a534d3073f87', '{""de"":""Waffenberechtigung"",""en"":""Firearms Authorisation"",""fr"":""Autorisation de port d''''arme"",""it"":""Autorizzazione all''''uso di armi da fuoco""}'::jsonb, '🔫', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('5da22bab-3fa8-40e0-90e7-ddbf5289e9bf', '{""de"":""Personenschutz"",""en"":""Bodyguard / Personal Protection"",""fr"":""Protection rapprochée"",""it"":""Protezione personale""}'::jsonb, '🛡️', false, 2, 2, false, NOW(), 'seed', '', ''),
                ('76688c0e-4a46-43b9-a321-37ce4243e3d3', '{""de"":""Deeskalationstraining"",""en"":""De-escalation Training"",""fr"":""Formation à la désescalade"",""it"":""Formazione alla de-escalation""}'::jsonb, '🤝', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('022a55a6-122e-4b6e-be57-8d9a3a173c75', '{""de"":""Zugangskontrolle"",""en"":""Access Control"",""fr"":""Contrôle d''''accès"",""it"":""Controllo accessi""}'::jsonb, '🚦', false, 2, 2, false, NOW(), 'seed', '', ''),
                ('72c25a08-fcde-4183-a657-ad0217d38694', '{""de"":""CCTV / Videoüberwachung"",""en"":""CCTV / Video Surveillance"",""fr"":""CCTV / Vidéosurveillance"",""it"":""CCTV / Videosorveglianza""}'::jsonb, '📹', false, 2, 2, false, NOW(), 'seed', '', ''),
                ('78310b09-3a61-483f-b1bd-e69a761ca207', '{""de"":""Brandschutz"",""en"":""Fire Protection"",""fr"":""Protection incendie"",""it"":""Protezione antincendio""}'::jsonb, '🔥', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('0034f092-0355-4552-9e86-8e3ac3b5b3b2', '{""de"":""Hundeführer (K9)"",""en"":""Dog Handler (K9)"",""fr"":""Maître-chien (K9)"",""it"":""Conduttore cinofilo (K9)""}'::jsonb, '🐕', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('3d4b9396-25b8-47a8-8202-668f8f81030e', '{""de"":""Eventschutz"",""en"":""Event Security"",""fr"":""Sécurité événementielle"",""it"":""Sicurezza eventi""}'::jsonb, '🎪', false, 2, 2, false, NOW(), 'seed', '', ''),

                -- Security – International (type = 2, category = 2)
                ('b257acc2-67ea-4b81-9038-f774c863962e', '{""de"":""§34a-Sachkundeprüfung (Deutschland/EU)"",""en"":""§34a Proficiency Test (Germany/EU Security)"",""fr"":""Examen de compétence §34a (Allemagne/UE)"",""it"":""Esame di competenza §34a (Germania/UE)""}'::jsonb, '🇩🇪', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('e8354115-4462-4c0d-8fce-a35369e967cb', '{""de"":""警備員検定 Keibi-in Kentei (Japan Sicherheitszertifikat)"",""en"":""警備員検定 Keibi-in Kentei (Japan Security Guard Certification)"",""fr"":""警備員検定 Keibi-in Kentei (Certification agent de sécurité Japon)"",""it"":""警備員検定 Keibi-in Kentei (Certificazione guardia di sicurezza Giappone)""}'::jsonb, '🇯🇵', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('8ddfb12f-0373-4c24-ab56-1390d2895e39', '{""de"":""Saudi MOI Sicherheitsgenehmigung"",""en"":""Saudi MOI Security Clearance"",""fr"":""Autorisation sécurité MOI Arabie saoudite"",""it"":""Autorizzazione sicurezza MOI Arabia Saudita""}'::jsonb, '🇸🇦', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('f7521782-553d-4cf2-9e81-7ec88c51af60', '{""de"":""Israelischer Sicherheitsbeauftragter-Ausweis"",""en"":""Israeli Security Officer Licence"",""fr"":""Licence agent de sécurité israélien"",""it"":""Licenza agente di sicurezza israeliano""}'::jsonb, '🇮🇱', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('0127ea12-fa4b-4a11-8dd7-b78d93e65779', '{""de"":""China PSB Sicherheitsausweis (保安员证)"",""en"":""China PSB Security Permit (保安员证)"",""fr"":""Permis de sécurité PSB Chine (保安员证)"",""it"":""Permesso di sicurezza PSB Cina (保安员证)""}'::jsonb, '🇨🇳', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('bd8f022d-0f17-4c11-bc97-151015b3b5d2', '{""de"":""Korean Security Guard License (경비원 자격증)"",""en"":""Korean Security Guard Licence (경비원 자격증)"",""fr"":""Licence agent de sécurité coréen (경비원 자격증)"",""it"":""Licenza guardia di sicurezza coreana (경비원 자격증)""}'::jsonb, '🇰🇷', true, 2, 2, false, NOW(), 'seed', '', ''),
                ('917f486e-e48a-4e11-bd68-a531702b20a8', '{""de"":""Taiwan Security Guard License (保全人員執照)"",""en"":""Taiwan Security Guard Licence (保全人員執照)"",""fr"":""Licence agent de sécurité taïwanais (保全人員執照)"",""it"":""Licenza guardia di sicurezza taiwanese (保全人員執照)""}'::jsonb, '🇹🇼', true, 2, 2, false, NOW(), 'seed', '', ''),

                -- Logistics / Spedition – CH (type = 2, category = 3)
                ('9a185d56-180a-4450-8aee-ea98ac897f49', '{""de"":""Führerausweis C / CE (LKW / Sattelzug)"",""en"":""Driving Licence C / CE (HGV / Articulated)"",""fr"":""Permis de conduire C / CE (PL / Semi-remorque)"",""it"":""Patente C / CE (Autocarro / Autoarticolato)""}'::jsonb, '🚛', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('88800ec7-b4fb-471c-ac30-2b759743cf67', '{""de"":""ADR-Schein (Gefahrgutstransport)"",""en"":""ADR Certificate (Dangerous Goods Transport)"",""fr"":""Certificat ADR (Transport de matières dangereuses)"",""it"":""Certificato ADR (Trasporto di merci pericolose)""}'::jsonb, '☢️', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd', '{""de"":""Staplerschein"",""en"":""Forklift Licence"",""fr"":""Certificat de cariste"",""it"":""Patente per muletto""}'::jsonb, '🏗️', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('19f57576-eb75-417f-986d-a33cc83b6502', '{""de"":""Kranbedienung"",""en"":""Crane Operation"",""fr"":""Conduite de grue"",""it"":""Conduzione gru""}'::jsonb, '🏗️', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('99391d2c-6dc2-419a-b789-8d1e2032c7b4', '{""de"":""Ladungssicherung"",""en"":""Load Securing"",""fr"":""Arrimage des charges"",""it"":""Sicurezza del carico""}'::jsonb, '🔒', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('3ff965ae-6cde-46e1-8094-f51889fb64eb', '{""de"":""IATA Gefahrgut Luft"",""en"":""IATA Dangerous Goods Air (DGR)"",""fr"":""IATA Marchandises dangereuses aérien"",""it"":""IATA Merci pericolose aereo""}'::jsonb, '✈️', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('1a9a21b3-535d-4bfa-ab88-272c4b9f3810', '{""de"":""IMDG (Gefahrgut See)"",""en"":""IMDG (Dangerous Goods by Sea)"",""fr"":""IMDG (Marchandises dangereuses par mer)"",""it"":""IMDG (Merci pericolose via mare)""}'::jsonb, '🚢', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('ec71642b-facc-4b6c-a0ef-1b3e41030f64', '{""de"":""Digitaler Tachograph"",""en"":""Digital Tachograph"",""fr"":""Tachygraphe numérique"",""it"":""Tachigrafo digitale""}'::jsonb, '📟', false, 2, 3, false, NOW(), 'seed', '', ''),
                ('d119c40e-3390-4aac-bd15-bd869629039d', '{""de"":""Zollabwicklung"",""en"":""Customs Clearance"",""fr"":""Dédouanement"",""it"":""Sdoganamento""}'::jsonb, '📋', false, 2, 3, false, NOW(), 'seed', '', ''),
                ('b9543ec0-6281-4161-b0f8-7f036d48ddae', '{""de"":""HACCP (Lebensmittellogistik)"",""en"":""HACCP (Food Logistics)"",""fr"":""HACCP (Logistique alimentaire)"",""it"":""HACCP (Logistica alimentare)""}'::jsonb, '🌡️', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('0c562163-931d-4982-bfcc-0388bec2ef9f', '{""de"":""Kühlkette"",""en"":""Cold Chain Management"",""fr"":""Gestion de la chaîne du froid"",""it"":""Gestion de la chaîne du froid""}'::jsonb, '❄️', false, 2, 3, false, NOW(), 'seed', '', ''),
                ('90d38d22-a05e-46cb-a4e2-deea6d3cc9f4', '{""de"":""Schwertransport-Genehmigung"",""en"":""Heavy Transport Permit"",""fr"":""Autorisation de transport exceptionnel"",""it"":""Autorizzazione trasporto eccezionale""}'::jsonb, '⚖️', true, 2, 3, false, NOW(), 'seed', '', ''),

                -- Logistics / Spedition – International (type = 2, category = 3)
                ('9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c', '{""de"":""Code 95 / EU-Berufskraftfahrerqualifikation (BKF)"",""en"":""Code 95 / EU Certificate of Professional Competence (CPC)"",""fr"":""Code 95 / Qualification professionnelle conducteur UE (CPC)"",""it"":""Codice 95 / Qualifica professionale autista UE (CPC)""}'::jsonb, '🇪🇺', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('17615f16-9d2b-42e7-9e61-b1f0a407bb3b', '{""de"":""Japanischer Führerschein (JAF)"",""en"":""Japanese Driving Licence (JAF)"",""fr"":""Permis de conduire japonais (JAF)"",""it"":""Patente di guida giapponese (JAF)""}'::jsonb, '🇯🇵', false, 2, 3, false, NOW(), 'seed', '', ''),
                ('99dfebb6-0054-4758-83aa-d17439647144', '{""de"":""Halal-Logistik-Zertifizierung"",""en"":""Halal Logistics Certification"",""fr"":""Certification logistique halal"",""it"":""Certificazione logistica halal""}'::jsonb, '🌙', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('0571b080-ec96-41d7-9852-e49ac432ab83', '{""de"":""Cargo Security Protokoll Ben-Gurion (IOSA)"",""en"":""Ben-Gurion Cargo Security Protocol (IOSA)"",""fr"":""Protocole sécurité fret Ben Gourion (IOSA)"",""it"":""Protocollo sicurezza merci Ben Gurion (IOSA)""}'::jsonb, '🇮🇱', true, 2, 3, false, NOW(), 'seed', '', ''),
                ('abaef6e9-0172-4acd-9204-ff5980e976c2', '{""de"":""Chinesischer Führerschein (中国驾照)"",""en"":""Chinese Driving Licence (中国驾照)"",""fr"":""Permis de conduire chinois (中国驾照)"",""it"":""Patente di guida cinese (中国驾照)""}'::jsonb, '🇨🇳', false, 2, 3, false, NOW(), 'seed', '', ''),
                ('1b0e9baa-76b5-4d20-93ae-adcf181892a3', '{""de"":""AEO-Zertifizierung (Zugelassener Wirtschaftsbeteiligter)"",""en"":""AEO Certification (Authorised Economic Operator)"",""fr"":""Certification OEA (Opérateur économique agréé)"",""it"":""Certificazione OEA (Operatore economico autorizzato)""}'::jsonb, '📦', true, 2, 3, false, NOW(), 'seed', '', ''),

                -- Healthcare (type = 2, category = 4)
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000401', '{""de"":""NDS HF Intensivpflege"",""en"":""NDS HF Intensive Care Nursing"",""fr"":""NDS ES Soins intensifs"",""it"":""NDS SSS Cure intensive""}'::jsonb, '🏥', false, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000402', '{""de"":""NDS HF Anästhesiepflege"",""en"":""NDS HF Anaesthesia Nursing"",""fr"":""NDS ES Soins en anesthesie"",""it"":""NDS SSS Cure anestesiologiche""}'::jsonb, '😴', false, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000403', '{""de"":""NDS HF Notfallpflege"",""en"":""NDS HF Emergency Nursing"",""fr"":""NDS ES Soins urgences avances"",""it"":""NDS SSS Cure urgenza avanzate""}'::jsonb, '🚨', false, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000404', '{""de"":""ACLS (Advanced Cardiac Life Support)"",""en"":""ACLS (Advanced Cardiac Life Support)"",""fr"":""ACLS (Advanced Cardiac Life Support)"",""it"":""ACLS (Advanced Cardiac Life Support)""}'::jsonb, '💗', true, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000405', '{""de"":""PALS (Paediatric Advanced Life Support)"",""en"":""PALS (Paediatric Advanced Life Support)"",""fr"":""PALS (Paediatric Advanced Life Support)"",""it"":""PALS (Paediatric Advanced Life Support)""}'::jsonb, '👶', true, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000406', '{""de"":""Strahlenschutz (Röntgen/Nuklearmedizin)"",""en"":""Radiation Protection (X-ray/Nuclear Medicine)"",""fr"":""Radioprotection (radiologie/medecine nucleaire)"",""it"":""Radioprotezione (radiologia/medicina nucleare)""}'::jsonb, '☢️', true, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000407', '{""de"":""Operationspflege (OPS-Fachkurs)"",""en"":""Operating Theatre Nursing"",""fr"":""Soins salle operation"",""it"":""Cure sala operatoria""}'::jsonb, '🔬', false, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000408', '{""de"":""Sterilisationsfachkraft (ZSVA)"",""en"":""Sterile Supply Technician (CSSD)"",""fr"":""Technicien sterilisation CSSD"",""it"":""Tecnico sterilizzazione CSSD""}'::jsonb, '🧪', true, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000409', '{""de"":""Psychiatriepflege"",""en"":""Psychiatric Nursing"",""fr"":""Soins en psychiatrie"",""it"":""Cure psichiatriche""}'::jsonb, '🧠', false, 2, 4, false, NOW(), 'seed', '', ''),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000410', '{""de"":""Infektionsprävention (Hygienefachkraft)"",""en"":""Infection Prevention Hygiene Specialist"",""fr"":""Prevention infections specialiste hygiene"",""it"":""Prevenzione infezioni specialista igiene""}'::jsonb, '🦠', true, 2, 4, false, NOW(), 'seed', '', ''),

                -- Gastronomy (type = 2, category = 5)
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000501', '{""de"":""Wirtepatent (Gastgewerbebewilligung)"",""en"":""Innkeeper Licence (Catering Permit)"",""fr"":""Brevet cafetier-restaurateur"",""it"":""Brevetto esercente""}'::jsonb, '🍽️', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000502', '{""de"":""Koch/Köchin EFZ"",""en"":""Professional Cook (EFZ)"",""fr"":""Cuisinier/Cuisiniere CFC"",""it"":""Cuoco/Cuoca AFC""}'::jsonb, '🍳', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000503', '{""de"":""Restaurationsfachmann/-frau EFZ"",""en"":""Restaurant Specialist (EFZ)"",""fr"":""Specialiste restauration CFC"",""it"":""Specialista ristorante AFC""}'::jsonb, '🥂', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000504', '{""de"":""Hotelfachmann/-frau EFZ"",""en"":""Hotel Specialist (EFZ)"",""fr"":""Specialiste hotellerie CFC"",""it"":""Specialista alberghiero AFC""}'::jsonb, '🏨', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000505', '{""de"":""Lebensmittelhygiene-Schulung (GHP/GMP)"",""en"":""Food Hygiene Training (GHP/GMP)"",""fr"":""Formation hygiene alimentaire GHP GMP"",""it"":""Formazione igiene alimentare GHP GMP""}'::jsonb, '🌡️', true, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000506', '{""de"":""Barkeeper / Mixologe (Barfachmann)"",""en"":""Bartender / Mixologist"",""fr"":""Barman specialiste bar"",""it"":""Barman/Barlady""}'::jsonb, '🍸', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000507', '{""de"":""Sommelierausweis (ASSP)"",""en"":""Sommelier Certificate (ASSP)"",""fr"":""Brevet de sommelier ASSP"",""it"":""Brevetto sommelier ASSP""}'::jsonb, '🍷', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', '{""de"":""Allergenkennzeichnung (EU-Lebensmittelinformationsverordnung)"",""en"":""Allergen Labelling (EU Food Information Regulation)"",""fr"":""Etiquetage allergenes reglement UE"",""it"":""Etichettatura allergeni regolamento UE""}'::jsonb, '⚠️', true, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000509', '{""de"":""Kassensystem-Zertifizierung (POS)"",""en"":""Point-of-Sale System Certification"",""fr"":""Certification systeme caisse POS"",""it"":""Certificazione sistema cassa POS""}'::jsonb, '🖥️', false, 2, 5, false, NOW(), 'seed', '', ''),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000510', '{""de"":""Eventcatering-Zertifikat"",""en"":""Event Catering Certificate"",""fr"":""Certificat traiteur evenementiel"",""it"":""Certificato catering eventi""}'::jsonb, '🎉', false, 2, 5, false, NOW(), 'seed', '', ''),

                -- Construction (type = 2, category = 6)
                ('4c5d6e7f-8091-4a00-8b00-ccddef000601', '{""de"":""EFZ Maurer/Maurerin"",""en"":""Mason (EFZ)"",""fr"":""Macon CFC"",""it"":""Muratore AFC""}'::jsonb, '🧱', false, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000602', '{""de"":""EFZ Zimmermann/Zimmerin"",""en"":""Carpenter (EFZ)"",""fr"":""Charpentier CFC"",""it"":""Falegname AFC""}'::jsonb, '🪵', false, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', '{""de"":""Schweissausweis EN ISO 9606 (Lichtbogen/MIG/MAG/WIG)"",""en"":""Welding Certificate EN ISO 9606 (MMA/MIG/MAG/TIG)"",""fr"":""Certificat soudeur EN ISO 9606"",""it"":""Certificato saldatore EN ISO 9606""}'::jsonb, '🔧', true, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000604', '{""de"":""Elektro-Installationsbewilligung NIV (ESTI)"",""en"":""Electrical Installation Permit NIV (ESTI)"",""fr"":""Permis installation electrique OIBT ESTI"",""it"":""Autorizzazione installazione elettrica OIBT ESTI""}'::jsonb, '⚡', true, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000605', '{""de"":""SUVA-Asbestbewilligung (Asbestsanierung)"",""en"":""SUVA Asbestos Removal Licence"",""fr"":""Autorisation amiante SUVA"",""it"":""Autorizzazione SUVA rimozione amianto""}'::jsonb, '🏗️', true, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000606', '{""de"":""Sprengausweis SUVA (Sprengtechnik)"",""en"":""SUVA Blasting Licence (Explosives)"",""fr"":""Autorisation tir SUVA explosifs"",""it"":""Autorizzazione SUVA esplosivi""}'::jsonb, '💥', true, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000607', '{""de"":""Gerüstbau-Ausweis (SGUV)"",""en"":""Scaffolding Certificate (SGUV)"",""fr"":""Certificat echafaudage SSUV"",""it"":""Certificato ponteggi SSUV""}'::jsonb, '🪜', true, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000608', '{""de"":""Sanitärinstallateur EFZ"",""en"":""Sanitary Installer (EFZ)"",""fr"":""Installateur sanitaire CFC"",""it"":""Installatore sanitario AFC""}'::jsonb, '🔩', false, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000609', '{""de"":""Tiefbaufachmann/-frau EFZ (Strassenbau)"",""en"":""Civil Engineering Specialist EFZ (Road Construction)"",""fr"":""Specialiste construction CFC genie civil"",""it"":""Specialista edilizia civile AFC""}'::jsonb, '🛣️', false, 2, 6, false, NOW(), 'seed', '', ''),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000610', '{""de"":""Seilbahntechniker ASTRA (Bergbahnen)"",""en"":""Cable Car Technician ASTRA"",""fr"":""Technicien telepherique OFROU"",""it"":""Tecnico funivie USTRA""}'::jsonb, '🚡', true, 2, 6, false, NOW(), 'seed', '', ''),

                -- Cleaning (type = 2, category = 7)
                ('5d6e7f80-9102-4b00-8c00-ddeeff000701', '{""de"":""Gebäudereiniger/-in EFZ"",""en"":""Building Cleaner (EFZ)"",""fr"":""Nettoyeur batiments CFC"",""it"":""Addetto pulizie AFC""}'::jsonb, '🧹', false, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000702', '{""de"":""Textilpflege EBA (Wäscherei/Reinigung)"",""en"":""Textile Care EBA (Laundry/Dry Cleaning)"",""fr"":""Entretien textile AFP blanchisserie"",""it"":""Cura tessile CFP lavanderia""}'::jsonb, '👕', false, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000703', '{""de"":""Schädlingsbekämpfung Fachbewilligung (SBKV)"",""en"":""Pest Control Licence (SBKV)"",""fr"":""Lutte antiparasitaire SBKV"",""it"":""Derattizzazione disinfestazione SBKV""}'::jsonb, '🐀', true, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000704', '{""de"":""Hygieneschulung Facility Management"",""en"":""Hygiene Training Facility Management"",""fr"":""Formation hygiene facility management"",""it"":""Formazione igiene facility management""}'::jsonb, '🧴', true, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', '{""de"":""Umgang mit Reinigungschemikalien (GHS/CLP)"",""en"":""Handling Cleaning Chemicals (GHS/CLP)"",""fr"":""Manipulation produits chimiques GHS CLP"",""it"":""Gestione prodotti chimici pulizia GHS CLP""}'::jsonb, '⚗️', true, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000706', '{""de"":""Unterhaltsreinigung Spital/Klinik"",""en"":""Hospital/Clinical Housekeeping"",""fr"":""Entretien hopital clinique"",""it"":""Pulizie ospedaliere cliniche""}'::jsonb, '🏥', false, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000707', '{""de"":""Reinraumreinigung (Pharma/Labor)"",""en"":""Cleanroom Cleaning (Pharma/Laboratory)"",""fr"":""Nettoyage salle blanche pharma laboratoire"",""it"":""Pulizia camera bianca farmaceutica""}'::jsonb, '🔬', true, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000708', '{""de"":""Fassadenreinigung / Höhenarbeit (SUVA)"",""en"":""Facade Cleaning / Work at Height (SUVA)"",""fr"":""Nettoyage facades travail hauteur SUVA"",""it"":""Pulizia facciate lavoro quota SUVA""}'::jsonb, '🪟', true, 2, 7, false, NOW(), 'seed', '', ''),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000709', '{""de"":""Grünanlagenpflege / Hauswart EFZ"",""en"":""Grounds Maintenance / Caretaker EFZ"",""fr"":""Entretien espaces verts CFC"",""it"":""Manutenzione verde AFC""}'::jsonb, '🌿', false, 2, 7, false, NOW(), 'seed', '', ''),

                -- Transport (type = 2, category = 8)
                ('6e7f8091-0213-4c00-8d00-ef0011000801', '{""de"":""Führerausweis D (Linienbus)"",""en"":""Driving Licence D (Bus)"",""fr"":""Permis de conduire D autobus"",""it"":""Patente D autobus""}'::jsonb, '🚌', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', '{""de"":""Führerausweis D1 (Kleinbus / Reisecar)"",""en"":""Driving Licence D1 (Minibus / Coach)"",""fr"":""Permis de conduire D1 minibus"",""it"":""Patente D1 minibus""}'::jsonb, '🚐', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000803', '{""de"":""Berufsmässiger Personentransport BPT (Taxiausweis)"",""en"":""Professional Passenger Transport BPT (Taxi Licence)"",""fr"":""Transport professionnel personnes BPT"",""it"":""Trasporto professionale persone BPT""}'::jsonb, '🚕', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000804', '{""de"":""CZV-Weiterbildung Personentransport (Kategorie D)"",""en"":""CZV Continuing Training Passenger Transport (Category D)"",""fr"":""Formation continue LCR transport personnes categorie D"",""it"":""Formazione continua LCStr trasporto persone categoria D""}'::jsonb, '📋', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', '{""de"":""Verkehrsmedizinische Untersuchung (Fahreignung)"",""en"":""Medical Fitness to Drive Examination"",""fr"":""Examen medical aptitude conduite"",""it"":""Visita medica idoneita guida""}'::jsonb, '🩺', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000806', '{""de"":""Rettungsfahrer-Ausbildung (IVR Stufe 1/2/3)"",""en"":""Emergency Driver Training (IVR Level 1/2/3)"",""fr"":""Formation conducteur urgences IAS niveau 1 2 3"",""it"":""Formazione autista emergenza IVR livello 1 2 3""}'::jsonb, '🚑', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000807', '{""de"":""Krankenfahrer-Zertifikat (Liegendstuhl/Rollstuhl)"",""en"":""Patient Transport Driver Certificate"",""fr"":""Certificat conducteur transport sanitaire"",""it"":""Certificato autista trasporto sanitario""}'::jsonb, '♿', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000808', '{""de"":""Schülertransport-Bewilligung (ASSA)"",""en"":""School Bus Permit (ASSA)"",""fr"":""Autorisation transport scolaire ASSA"",""it"":""Autorizzazione trasporto scolastico ASSA""}'::jsonb, '🏫', true, 2, 8, false, NOW(), 'seed', '', ''),
                ('6e7f8091-0213-4c00-8d00-ef0011000809', '{""de"":""Chauffeur eidg. Fachausweis"",""en"":""Chauffeur Federal Diploma of Professional Competence"",""fr"":""Brevet federal chauffeur"",""it"":""Attestato federale autista""}'::jsonb, '🎓', false, 2, 8, false, NOW(), 'seed', '', '')

                ON CONFLICT (id) DO NOTHING;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                VALUES
                -- Deutsch: spoken in DE, AT, CH
                ('744d6e64-3ddf-41d0-87c8-30b690c451d9', 'DE'),
                ('744d6e64-3ddf-41d0-87c8-30b690c451d9', 'AT'),
                ('744d6e64-3ddf-41d0-87c8-30b690c451d9', 'CH'),
                -- Englisch: spoken in GB, US, AU, CA, IE, CH
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'GB'),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'US'),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'AU'),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'CA'),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'IE'),
                ('17e5181d-8b20-4ee5-8e15-5beca491f2c0', 'CH'),
                -- Französisch: spoken in FR, BE, CH
                ('664351fc-0c5a-4610-9c05-f5faf778ff90', 'FR'),
                ('664351fc-0c5a-4610-9c05-f5faf778ff90', 'BE'),
                ('664351fc-0c5a-4610-9c05-f5faf778ff90', 'CH'),
                -- Italienisch: spoken in IT, CH, SM
                ('4a272d5a-d188-4b1e-9631-5870171c8d8f', 'IT'),
                ('4a272d5a-d188-4b1e-9631-5870171c8d8f', 'CH'),
                ('4a272d5a-d188-4b1e-9631-5870171c8d8f', 'SM'),
                -- JLPT N3 (Japanisch)
                ('3f39546f-5573-44a9-8260-4bd28574eadb', 'JP'),
                -- Arabischkenntnisse
                ('e037374a-1a39-4020-8a13-ee1051dd359f', 'SA'),
                -- Ivrit-Kenntnisse (Hebräisch)
                ('cef4f851-2ce1-4c86-8367-5fb890c98334', 'IL'),
                -- Mandarin-Kenntnisse (HSK)
                ('178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4', 'CN'),
                -- Koreanisch (TOPIK)
                ('0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e', 'KR'),
                -- Spitex / Home Care – CH
                ('53d33bd0-cd52-48e9-ac0e-618bc94760c7', 'CH'),
                ('72a86d8b-fc16-49da-b5d9-e6feee81c329', 'CH'),
                ('eb12579a-9262-44a6-be60-d3c9be2954f6', 'CH'),
                ('d75c138d-23f1-4064-a3b7-5c202d9e9363', 'CH'),
                ('cd74f2e9-add7-42f5-b707-3caf1597a25e', 'CH'),
                ('a8bb8129-a47b-4b7b-be2c-4c3b918b19ea', 'CH'),
                ('ac78925e-5019-40be-8db6-d0d20f444171', 'CH'),
                ('75b24b9c-4a48-4818-be68-4f65744463d8', 'CH'),
                ('d98105e2-b60c-4d37-9659-8d685c514ebc', 'CH'),
                ('18c4ae01-7e43-46b4-a4e4-bd12437bd5e3', 'CH'),
                ('b702a794-c83c-4d94-a3fa-f28b850b79e6', 'CH'),
                ('3fd89ae5-21e6-474a-87de-07fdbbe82c41', 'CH'),
                ('1190a2fd-c313-457f-b186-3b189ca59344', 'CH'),
                ('239c2e84-49d6-4023-b982-3658cfc85b4c', 'CH'),
                -- Spitex / Home Care – International
                ('dc70fef8-2822-430e-9d25-e6a709984d7e', 'EU'),
                ('4df5b030-7650-4f6d-93cf-0d5e29658bac', 'JP'),
                ('5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1', 'SA'),
                ('b9154bfa-5abb-4cd7-9f3b-16a339996add', 'IL'),
                ('e3201f77-d5f2-4a8b-af49-8214b3eeda95', 'CN'),
                ('c5ecea26-4bd7-4080-a728-aa26ebd7b226', 'TW'),
                ('40b1bcf6-bb79-40aa-9f59-f60aa381021a', 'KR'),
                -- Security – CH
                ('ea7b299d-96fd-47c2-83f3-e5ba340e4b73', 'CH'),
                ('cb3b1969-9c8b-4b51-b9d0-a534d3073f87', 'CH'),
                -- Security – International
                ('b257acc2-67ea-4b81-9038-f774c863962e', 'DE'),
                ('e8354115-4462-4c0d-8fce-a35369e967cb', 'JP'),
                ('8ddfb12f-0373-4c24-ab56-1390d2895e39', 'SA'),
                ('f7521782-553d-4cf2-9e81-7ec88c51af60', 'IL'),
                ('0127ea12-fa4b-4a11-8dd7-b78d93e65779', 'CN'),
                ('bd8f022d-0f17-4c11-bc97-151015b3b5d2', 'KR'),
                ('917f486e-e48a-4e11-bd68-a531702b20a8', 'TW'),
                -- Logistics – CH
                ('9a185d56-180a-4450-8aee-ea98ac897f49', 'CH'),
                ('88800ec7-b4fb-471c-ac30-2b759743cf67', 'CH'),
                ('6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd', 'CH'),
                ('19f57576-eb75-417f-986d-a33cc83b6502', 'CH'),
                ('90d38d22-a05e-46cb-a4e2-deea6d3cc9f4', 'CH'),
                -- Logistics – International
                ('9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c', 'EU'),
                ('17615f16-9d2b-42e7-9e61-b1f0a407bb3b', 'JP'),
                ('0571b080-ec96-41d7-9852-e49ac432ab83', 'IL'),
                ('abaef6e9-0172-4acd-9204-ff5980e976c2', 'CN'),
                -- Healthcare – CH/DE/AT/US
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000401', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000402', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000403', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000404', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000404', 'DE'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000404', 'AT'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000404', 'US'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000405', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000405', 'DE'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000405', 'AT'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000405', 'US'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000406', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000406', 'DE'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000406', 'AT'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000407', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000408', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000408', 'DE'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000408', 'AT'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000409', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000410', 'CH'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000410', 'DE'),
                ('2a3b4c5d-6e7f-4800-8900-aabbcc000410', 'AT'),
                -- Gastronomy – CH/DE/AT/FR/IT
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000501', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000502', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000503', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000504', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000505', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000505', 'DE'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000505', 'AT'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000505', 'FR'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000506', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000506', 'DE'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000506', 'AT'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000507', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000507', 'FR'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000507', 'IT'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000507', 'DE'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', 'DE'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', 'AT'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', 'FR'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000508', 'IT'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000509', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000510', 'CH'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000510', 'DE'),
                ('3b4c5d6e-7f80-4900-8a00-bbccdd000510', 'AT'),
                -- Construction – CH/DE/AT/FR/IT
                ('4c5d6e7f-8091-4a00-8b00-ccddef000601', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000602', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', 'DE'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', 'AT'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', 'FR'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000603', 'IT'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000604', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000605', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000606', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000607', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000607', 'DE'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000607', 'AT'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000608', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000609', 'CH'),
                ('4c5d6e7f-8091-4a00-8b00-ccddef000610', 'CH'),
                -- Cleaning – CH/DE/AT/FR/IT
                ('5d6e7f80-9102-4b00-8c00-ddeeff000701', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000702', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000703', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000704', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000704', 'DE'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000704', 'AT'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', 'DE'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', 'AT'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', 'FR'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000705', 'IT'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000706', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000707', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000707', 'DE'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000708', 'CH'),
                ('5d6e7f80-9102-4b00-8c00-ddeeff000709', 'CH'),
                -- Transport – CH/DE/AT/FR/IT
                ('6e7f8091-0213-4c00-8d00-ef0011000801', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000801', 'DE'),
                ('6e7f8091-0213-4c00-8d00-ef0011000801', 'AT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000801', 'FR'),
                ('6e7f8091-0213-4c00-8d00-ef0011000801', 'IT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', 'DE'),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', 'AT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', 'FR'),
                ('6e7f8091-0213-4c00-8d00-ef0011000802', 'IT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000803', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000804', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000804', 'DE'),
                ('6e7f8091-0213-4c00-8d00-ef0011000804', 'AT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', 'DE'),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', 'AT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', 'FR'),
                ('6e7f8091-0213-4c00-8d00-ef0011000805', 'IT'),
                ('6e7f8091-0213-4c00-8d00-ef0011000806', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000807', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000808', 'CH'),
                ('6e7f8091-0213-4c00-8d00-ef0011000809', 'CH')

                ON CONFLICT (qualification_id, country_code) DO NOTHING;
            ");
        }
    }
}
