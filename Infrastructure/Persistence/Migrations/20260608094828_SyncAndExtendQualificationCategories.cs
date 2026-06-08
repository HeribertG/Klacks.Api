using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncAndExtendQualificationCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE qualification SET category = 1 WHERE id IN (
                    '53d33bd0-cd52-48e9-ac0e-618bc94760c7','72a86d8b-fc16-49da-b5d9-e6feee81c329',
                    'eb12579a-9262-44a6-be60-d3c9be2954f6','d75c138d-23f1-4064-a3b7-5c202d9e9363',
                    'cd74f2e9-add7-42f5-b707-3caf1597a25e','a8bb8129-a47b-4b7b-be2c-4c3b918b19ea',
                    'ac78925e-5019-40be-8db6-d0d20f444171','75b24b9c-4a48-4818-be68-4f65744463d8',
                    'd98105e2-b60c-4d37-9659-8d685c514ebc','18c4ae01-7e43-46b4-a4e4-bd12437bd5e3',
                    'b702a794-c83c-4d94-a3fa-f28b850b79e6','3fd89ae5-21e6-474a-87de-07fdbbe82c41',
                    '1190a2fd-c313-457f-b186-3b189ca59344','239c2e84-49d6-4023-b982-3658cfc85b4c',
                    'dc70fef8-2822-430e-9d25-e6a709984d7e','4df5b030-7650-4f6d-93cf-0d5e29658bac',
                    '5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1','68d28df9-113a-40cf-bf6b-3c9bb18660e3',
                    'b9154bfa-5abb-4cd7-9f3b-16a339996add','e3201f77-d5f2-4a8b-af49-8214b3eeda95',
                    'c5ecea26-4bd7-4080-a728-aa26ebd7b226','40b1bcf6-bb79-40aa-9f59-f60aa381021a'
                );

                UPDATE qualification SET category = 2 WHERE id IN (
                    'ea7b299d-96fd-47c2-83f3-e5ba340e4b73','cb3b1969-9c8b-4b51-b9d0-a534d3073f87',
                    '5da22bab-3fa8-40e0-90e7-ddbf5289e9bf','76688c0e-4a46-43b9-a321-37ce4243e3d3',
                    '022a55a6-122e-4b6e-be57-8d9a3a173c75','72c25a08-fcde-4183-a657-ad0217d38694',
                    '78310b09-3a61-483f-b1bd-e69a761ca207','0034f092-0355-4552-9e86-8e3ac3b5b3b2',
                    '3d4b9396-25b8-47a8-8202-668f8f81030e','b257acc2-67ea-4b81-9038-f774c863962e',
                    'e8354115-4462-4c0d-8fce-a35369e967cb','8ddfb12f-0373-4c24-ab56-1390d2895e39',
                    'f7521782-553d-4cf2-9e81-7ec88c51af60','0127ea12-fa4b-4a11-8dd7-b78d93e65779',
                    'bd8f022d-0f17-4c11-bc97-151015b3b5d2','917f486e-e48a-4e11-bd68-a531702b20a8'
                );

                UPDATE qualification SET category = 3 WHERE id IN (
                    '9a185d56-180a-4450-8aee-ea98ac897f49','88800ec7-b4fb-471c-ac30-2b759743cf67',
                    '6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd','19f57576-eb75-417f-986d-a33cc83b6502',
                    '99391d2c-6dc2-419a-b789-8d1e2032c7b4','3ff965ae-6cde-46e1-8094-f51889fb64eb',
                    '1a9a21b3-535d-4bfa-ab88-272c4b9f3810','ec71642b-facc-4b6c-a0ef-1b3e41030f64',
                    'd119c40e-3390-4aac-bd15-bd869629039d','b9543ec0-6281-4161-b0f8-7f036d48ddae',
                    '0c562163-931d-4982-bfcc-0388bec2ef9f','90d38d22-a05e-46cb-a4e2-deea6d3cc9f4',
                    '9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c','17615f16-9d2b-42e7-9e61-b1f0a407bb3b',
                    '99dfebb6-0054-4758-83aa-d17439647144','0571b080-ec96-41d7-9852-e49ac432ab83',
                    'abaef6e9-0172-4acd-9204-ff5980e976c2','1b0e9baa-76b5-4d20-93ae-adcf181892a3'
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO qualification (id, name, emoji, is_time_limited, type, category, is_deleted, create_time, current_user_created, current_user_updated, current_user_deleted)
                VALUES

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
                -- Healthcare
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
                -- Gastronomy
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
                -- Construction
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
                -- Cleaning
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
                -- Transport
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE qualification SET category = 0 WHERE id IN (
                    '53d33bd0-cd52-48e9-ac0e-618bc94760c7','72a86d8b-fc16-49da-b5d9-e6feee81c329',
                    'eb12579a-9262-44a6-be60-d3c9be2954f6','d75c138d-23f1-4064-a3b7-5c202d9e9363',
                    'cd74f2e9-add7-42f5-b707-3caf1597a25e','a8bb8129-a47b-4b7b-be2c-4c3b918b19ea',
                    'ac78925e-5019-40be-8db6-d0d20f444171','75b24b9c-4a48-4818-be68-4f65744463d8',
                    'd98105e2-b60c-4d37-9659-8d685c514ebc','18c4ae01-7e43-46b4-a4e4-bd12437bd5e3',
                    'b702a794-c83c-4d94-a3fa-f28b850b79e6','3fd89ae5-21e6-474a-87de-07fdbbe82c41',
                    '1190a2fd-c313-457f-b186-3b189ca59344','239c2e84-49d6-4023-b982-3658cfc85b4c',
                    'dc70fef8-2822-430e-9d25-e6a709984d7e','4df5b030-7650-4f6d-93cf-0d5e29658bac',
                    '5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1','68d28df9-113a-40cf-bf6b-3c9bb18660e3',
                    'b9154bfa-5abb-4cd7-9f3b-16a339996add','e3201f77-d5f2-4a8b-af49-8214b3eeda95',
                    'c5ecea26-4bd7-4080-a728-aa26ebd7b226','40b1bcf6-bb79-40aa-9f59-f60aa381021a',
                    'ea7b299d-96fd-47c2-83f3-e5ba340e4b73','cb3b1969-9c8b-4b51-b9d0-a534d3073f87',
                    '5da22bab-3fa8-40e0-90e7-ddbf5289e9bf','76688c0e-4a46-43b9-a321-37ce4243e3d3',
                    '022a55a6-122e-4b6e-be57-8d9a3a173c75','72c25a08-fcde-4183-a657-ad0217d38694',
                    '78310b09-3a61-483f-b1bd-e69a761ca207','0034f092-0355-4552-9e86-8e3ac3b5b3b2',
                    '3d4b9396-25b8-47a8-8202-668f8f81030e','b257acc2-67ea-4b81-9038-f774c863962e',
                    'e8354115-4462-4c0d-8fce-a35369e967cb','8ddfb12f-0373-4c24-ab56-1390d2895e39',
                    'f7521782-553d-4cf2-9e81-7ec88c51af60','0127ea12-fa4b-4a11-8dd7-b78d93e65779',
                    'bd8f022d-0f17-4c11-bc97-151015b3b5d2','917f486e-e48a-4e11-bd68-a531702b20a8',
                    '9a185d56-180a-4450-8aee-ea98ac897f49','88800ec7-b4fb-471c-ac30-2b759743cf67',
                    '6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd','19f57576-eb75-417f-986d-a33cc83b6502',
                    '99391d2c-6dc2-419a-b789-8d1e2032c7b4','3ff965ae-6cde-46e1-8094-f51889fb64eb',
                    '1a9a21b3-535d-4bfa-ab88-272c4b9f3810','ec71642b-facc-4b6c-a0ef-1b3e41030f64',
                    'd119c40e-3390-4aac-bd15-bd869629039d','b9543ec0-6281-4161-b0f8-7f036d48ddae',
                    '0c562163-931d-4982-bfcc-0388bec2ef9f','90d38d22-a05e-46cb-a4e2-deea6d3cc9f4',
                    '9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c','17615f16-9d2b-42e7-9e61-b1f0a407bb3b',
                    '99dfebb6-0054-4758-83aa-d17439647144','0571b080-ec96-41d7-9852-e49ac432ab83',
                    'abaef6e9-0172-4acd-9204-ff5980e976c2','1b0e9baa-76b5-4d20-93ae-adcf181892a3'
                );

                DELETE FROM qualification_country WHERE qualification_id IN (
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000401','2a3b4c5d-6e7f-4800-8900-aabbcc000402',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000403','2a3b4c5d-6e7f-4800-8900-aabbcc000404',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000405','2a3b4c5d-6e7f-4800-8900-aabbcc000406',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000407','2a3b4c5d-6e7f-4800-8900-aabbcc000408',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000409','2a3b4c5d-6e7f-4800-8900-aabbcc000410',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000501','3b4c5d6e-7f80-4900-8a00-bbccdd000502',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000503','3b4c5d6e-7f80-4900-8a00-bbccdd000504',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000505','3b4c5d6e-7f80-4900-8a00-bbccdd000506',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000507','3b4c5d6e-7f80-4900-8a00-bbccdd000508',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000509','3b4c5d6e-7f80-4900-8a00-bbccdd000510',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000601','4c5d6e7f-8091-4a00-8b00-ccddef000602',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000603','4c5d6e7f-8091-4a00-8b00-ccddef000604',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000605','4c5d6e7f-8091-4a00-8b00-ccddef000606',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000607','4c5d6e7f-8091-4a00-8b00-ccddef000608',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000609','4c5d6e7f-8091-4a00-8b00-ccddef000610',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000701','5d6e7f80-9102-4b00-8c00-ddeeff000702',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000703','5d6e7f80-9102-4b00-8c00-ddeeff000704',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000705','5d6e7f80-9102-4b00-8c00-ddeeff000706',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000707','5d6e7f80-9102-4b00-8c00-ddeeff000708',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000709',
                    '6e7f8091-0213-4c00-8d00-ef0011000801','6e7f8091-0213-4c00-8d00-ef0011000802',
                    '6e7f8091-0213-4c00-8d00-ef0011000803','6e7f8091-0213-4c00-8d00-ef0011000804',
                    '6e7f8091-0213-4c00-8d00-ef0011000805','6e7f8091-0213-4c00-8d00-ef0011000806',
                    '6e7f8091-0213-4c00-8d00-ef0011000807','6e7f8091-0213-4c00-8d00-ef0011000808',
                    '6e7f8091-0213-4c00-8d00-ef0011000809'
                );

                DELETE FROM qualification WHERE id IN (
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000401','2a3b4c5d-6e7f-4800-8900-aabbcc000402',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000403','2a3b4c5d-6e7f-4800-8900-aabbcc000404',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000405','2a3b4c5d-6e7f-4800-8900-aabbcc000406',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000407','2a3b4c5d-6e7f-4800-8900-aabbcc000408',
                    '2a3b4c5d-6e7f-4800-8900-aabbcc000409','2a3b4c5d-6e7f-4800-8900-aabbcc000410',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000501','3b4c5d6e-7f80-4900-8a00-bbccdd000502',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000503','3b4c5d6e-7f80-4900-8a00-bbccdd000504',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000505','3b4c5d6e-7f80-4900-8a00-bbccdd000506',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000507','3b4c5d6e-7f80-4900-8a00-bbccdd000508',
                    '3b4c5d6e-7f80-4900-8a00-bbccdd000509','3b4c5d6e-7f80-4900-8a00-bbccdd000510',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000601','4c5d6e7f-8091-4a00-8b00-ccddef000602',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000603','4c5d6e7f-8091-4a00-8b00-ccddef000604',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000605','4c5d6e7f-8091-4a00-8b00-ccddef000606',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000607','4c5d6e7f-8091-4a00-8b00-ccddef000608',
                    '4c5d6e7f-8091-4a00-8b00-ccddef000609','4c5d6e7f-8091-4a00-8b00-ccddef000610',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000701','5d6e7f80-9102-4b00-8c00-ddeeff000702',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000703','5d6e7f80-9102-4b00-8c00-ddeeff000704',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000705','5d6e7f80-9102-4b00-8c00-ddeeff000706',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000707','5d6e7f80-9102-4b00-8c00-ddeeff000708',
                    '5d6e7f80-9102-4b00-8c00-ddeeff000709',
                    '6e7f8091-0213-4c00-8d00-ef0011000801','6e7f8091-0213-4c00-8d00-ef0011000802',
                    '6e7f8091-0213-4c00-8d00-ef0011000803','6e7f8091-0213-4c00-8d00-ef0011000804',
                    '6e7f8091-0213-4c00-8d00-ef0011000805','6e7f8091-0213-4c00-8d00-ef0011000806',
                    '6e7f8091-0213-4c00-8d00-ef0011000807','6e7f8091-0213-4c00-8d00-ef0011000808',
                    '6e7f8091-0213-4c00-8d00-ef0011000809'
                );
            ");
        }
    }
}
