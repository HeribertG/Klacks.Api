using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class CalendarRulesSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
              @"INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description_de,description_en,description_fr,description_it,name_de,name_en,name_fr,name_it) VALUES
               ('611f688c-8f58-4e8c-a3a6-a6c54f40f874', '11/01+21+SO', '', false, false, '4', 'CH', NULL, NULL, NULL, NULL, 'Eidg. Dank-, Buss- und Bettag', 'Federal Day of Thanksgiving, Repentance, and Prayer', 'Jeûne fédéral', 'Giorno federale di ringraziamento, pentimento e preghiera'),
               ('613c22be-e39f-4a40-be5a-e1202d21678f', '01/01', '', true, true, '4', 'CH', NULL, NULL, NULL, NULL, 'Neujahr', 'New Year''s Day', 'Jour de l''An', 'Capodanno'),
	             ('317b4823-568e-4445-a262-23114affa987', '01/02', '', true, true, '4', 'CH', NULL, NULL, NULL, NULL, 'Barzelistag', 'St. Berchtold''s Day','Jour de Saint Berthold','Giorno di San Berchtold'),
	             ('87325d0a-5865-475e-8fe2-f372c5c9c9f3', 'EASTER-02', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Karfreitag', 'Good Friday', 'Vendredi saint', 'Venerdì Santo'),
	             ('119a32f7-9d18-45cb-bdc6-c539304db33a', 'EASTER', '', true, true, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Ostersonntag', 'Easter Sunday', 'Pâques', 'Pasqua'),
	             ('eea656a6-7df4-4e13-9cb8-2cdb507f2b2c', '05/01+07+SO', '', false, false, 'CH', 'CH', NULL, NULL, NULL, NULL, 'Muttertag', 'Mother''s Day','Fête des Mères','esta della Mamma'),
	             ('2a99de50-523e-4d6c-9f0a-350979d7c8af', '12/24-21-SO', '', false, false, 'CH', 'CH', NULL, NULL, NULL, NULL, '1.Advent', 'First Advent', 'Premier dimanche de l''Avent','Prima domenica di Avvento'),
	             ('2c57e344-484f-41ff-8020-b96ebb14bcbd', '11/01+21+TH', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Thankgiving Day', 'Thanksgiving Day', 'Jour de l''Action de grâce','Thanksgiving Day'),
	             ('0cb07c92-0c73-4f76-a863-b04a672c2b5b', '01/15+00+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'Martin Luter King Day', 'Martin Luter King Day', 'Jour de Martin Luther King', 'Martin Luter King Day'),
	             ('f218601f-e2ef-4b39-9f4b-5da949fe2ab1', '02/01+14+MO', '', false, false, 'USA', 'USA', NULL, NULL, NULL, NULL, 'President`s Day', 'President`s Day', 'Jour du Président', 'President`s Day');
         INSERT INTO public.calendar_rule(id,rule, sub_rule, is_mandatory, is_paid, state, country, description_de, description_en, description_fr, description_it, name_de, name_en, name_fr, name_it) VALUES
               ('76d0cfdf-17a9-449c-840b-3742ad3506e5','05/01+27+MO','',false,false,'USA','USA', NULL, NULL, NULL, NULL,'Memorial Day','Memorial Day','Memorial Day','Memorial Day'),
	             ('f89e47b6-fc49-44cc-9977-a273ccf1f866','07/04','SA-1;SO+1',true,true,'USA','USA',NULL,NULL,NULL,NULL,'Independence Day','Independence Day','Independence Day','Independence Day'),
	             ('1d637ec5-ed5d-40e0-8df8-9df451ac2b5c','09/01+00+MO','',false,false,'USA','USA',NULL,NULL,NULL,NULL,'Labor Day','Labor Day','Labor Day','Labor Day'),
	             ('85bcb272-aeae-4d3c-8610-94a7a2a0d18d','10/01+07+MO','',false,false,'USA','USA',NULL,NULL,NULL,NULL,'Columbus Day','Columbus Day','Columbus Day','Columbus Day'),
	             ('fedc59db-1ac5-4819-846e-5695ae738881','EASTER+49','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Pfingsten',' Pentecost','Pentecôte','Pentecoste'),
	             ('acb8ce90-4f43-4383-9aec-b6286934455b','EASTER+50','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Pfingsmontag','Whit Monday','Lundi de Pentecôte','Lunedì di Pentecoste'),
	             ('e01194f9-6466-434e-be6a-eb964c894394','EASTER+60','',false,false,'BE','CH',NULL,NULL,NULL,NULL,'Fronleichnam','Corpus Christi','Fête-Dieu','Corpus Domini'),
	             ('682409ab-0b61-450e-84c3-309a72765de9','08/01','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Nationalfeiertag','National Day','Fête nationale','Festa nazionale'),
	             ('3f9ed34f-ad82-4eae-a1bd-eebca7d1a561','08/15','',false,false,'BE','CH',NULL,NULL,NULL,NULL,'Mariä Himmelfahrt','Assumption Day','Assomption','Assunzione di Maria'),
	             ('192711d6-d152-4d7a-a906-9eca2a44428b','11/01','',false,false,'BE','CH',NULL,NULL,NULL,NULL,'Allerheiligen','All Saints'' Day','Toussaint','Ognissanti');
        INSERT INTO public.calendar_rule(id,rule, sub_rule, is_mandatory, is_paid, state, country, description_de, description_en, description_fr, description_it, name_de, name_en, name_fr, name_it) VALUES
               ('fe3a3ab7-fd51-4ff4-b136-aab88db8ed3f','12/08','',false,false,'EB','CH', NULL, NULL, NULL, NULL,'Mariä Empfängnis','Immaculate Conception','Immaculée Conception','Immacolata Concezione'),
	             ('ea747828-228d-4cb0-85c2-0a3e69e71e5d','EASTER+01','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Ostermontag','Easter Monday','Lundi de Pâques','Lunedì di Pasqua'),
	             ('f54d9dd4-5e9c-49d6-81a0-c04d9715d519','EASTER+39','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Auffahrt','Ascension Day','Ascension','Ascensione'),
	             ('99fb49c4-8fea-4266-8836-e2b216345470','12/25','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'1.Weihnachtstag','Christmas Day','Jour de Noël','Natale'),
	             ('bebf9bdf-13c9-4965-8bb1-04ed41a863da','12/24','',true,true,'BE','CH',NULL,NULL,NULL,NULL,'Heiligabend','Christmas Eve','Veille de Noël','Vigilia di Natale'),
	             ('b4c7e8a7-ec66-4d48-88f3-9e08b59df86d','12/26','',true,true,'BE','CH',NULL,NULL,NULL,NULL,'2.Weihnachtstag','Boxing Day
            ','lendemain de Noël','Santo Stefano'),

               ('22e18646-8ded-4c6c-942b-82a060318416','12/31','',true,true,'BE','CH',NULL,NULL,NULL,NULL,'Silvester','New Year''s Eve','Saint-Sylvestre','Capodanno'),
	             ('1514ac26-c9e6-44cf-97e6-480cb7c67bce','EASTER-40-WE','',false,false,'BE','CH',NULL,NULL,NULL,NULL,'Aschermittwoch','Ash Wednesday','Mercredi des Cendres','Mercoledì delle Ceneri'),
	             ('b4fe6df7-e1c2-45ea-b536-37d221f0c3a9','01/06','',true,false,'BE','CH',NULL,NULL,NULL,NULL,'DreiKönigstag','Epiphany','Épiphanie','Epifania'),
	             ('21fb9d8e-4a1c-49a1-aca7-b28a2687f3a6','05/01','',true,true,'CH','CH',NULL,NULL,NULL,NULL,'Tag der Arbeit','Labor Day','Fête du Travail','Festa del Lavoro');
        INSERT INTO public.calendar_rule(id,rule, sub_rule, is_mandatory, is_paid, state, country, description_de, description_en, description_fr, description_it, name_de, name_en, name_fr, name_it) VALUES
               ('02217602-f018-4f1f-9132-7c9f63edc6c2','11/01+21+MO','',true,false,'BE','CH', NULL, NULL, NULL, NULL,'Zwibelemärit','Onion Market (traditional market in Bern)','Marché aux oignons (marché traditionnel à Berne)','Mercato della cipolla (mercato tradizionale a Berna)'),
	             ('3fcc0747-a7c5-4b57-af8c-c6adff8b71d4','09/01+07+TH','',true,true,'GE','CH',NULL,NULL,NULL,NULL,'Jeûne genevois','Geneva Fast','Jeûne genevois','Digiuno ginevrino'),
	             ('b77ca0b8-edd7-44f8-ace0-604d61881c5f','2/31','',true,true,'GE','CH',NULL,NULL,NULL,NULL,'Restauration de la République Genève','Restoration of the Republic Geneva','Restauration de la République Genève','Restaurazione della Repubblica di Ginevra'),
	             ('e51f2731-772b-424b-8181-e9379184dfe2','11/11','SA-1;SO+1',false,false,'USA','USA',NULL,NULL,NULL,NULL,'Veterans` Day','Veterans` Day','Veterans` Day','Veterans` Day'),
	             ('9b91ceed-b663-4eee-8af1-1bde6940b7b8','04/01+21+MO','',true,false,'ZH','CH','','','','','Sechseläuten
            ','Sechseläuten(traditional spring holiday in Zurich)','Sechseläuten(fête traditionnelle du printemps à Zurich)','Sechseläuten(festa tradizionale di primavera a Zurigo)'),
	             ('8126395d-9180-4eb6-aec4-729593c0518c','11/01+14+MO','',true,false,'ZH','CH','','','','','Knabenschiessen','Knabenschiessen (traditional shooting competition in Zurich)','Knabenschiessen (compétition de tir traditionnelle à Zurich)','Knabenschiessen (competizione di tiro tradizionale a Zurigo)');"
            );
        }
    }
}
