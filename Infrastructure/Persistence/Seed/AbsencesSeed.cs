using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public class AbsencesSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                                @"INSERT INTO public.absence (id,color,default_length,default_value,description,hide_in_gantt,name,abbreviation,undeletable,with_holiday,with_saturday,with_sunday,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted ) VALUES
	                           ('1070d7e6-f314-4d20-bc18-98c5357a4f89','#ff0000',1,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Krankheit/Unfall"",""en"":""Illness/Accident"",""fr"":""Maladie/Accident"",""it"":""Malattia/Infortunio""}','{""de"":""KU"",""en"":""IA"",""fr"":""MA"",""it"":""MI""}',false,true,true,true,'2022-07-16 09:34:42.697','admin','2022-07-16 09:38:20.871','admin',NULL,false,'' ),
	                           ('1d5a1964-7fad-4da9-945c-3ad00c0edaa8','#ff0000',1,0.5,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Krankheit/Unfall 50%"",""en"":""Illness/Accident 50%"",""fr"":""Maladie/Accident 50%"",""it"":""Malattia/Infortunio 50%""}','{""de"":""KU½"",""en"":""IA½"",""fr"":""MA½"",""it"":""MI½""}',false,true,true,true,'2022-07-16 09:38:06.909','admin','2022-08-25 21:47:33.696','admin',NULL,false,'' ),
	                           ('dca0367a-530e-4eae-be34-b19d2262587e','#ff80c0',1,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Schulung"",""en"":""Training"",""fr"":""Formation"",""it"":""Formazione""}','{""de"":""Sc"",""en"":""Tr"",""fr"":""Fo"",""it"":""Fo""}',false,true,true,true,'2022-07-16 09:35:54.261','admin','2022-07-16 09:38:26.506','admin',NULL,false,'' ),
	                           ('15ee57e6-31d1-492e-bf83-d3c386ef7472','#0080c0',5,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Ferien"",""en"":""Vacancies"",""fr"":""Vacances"",""it"":""Vacanze""}','{""de"":""Fe"",""en"":""Va"",""fr"":""Va"",""it"":""Va""}',false,false,false,false,'2022-07-14 18:23:15.037','admin','2022-10-15 09:56:18.505','admin',NULL,false,'' ),
	                           ('53851d0a-ff7f-460a-82a0-481aa3547d7e','#0080c0',5,0.5,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""1/2 Ferien"",""en"":""1/2 Vacancies"",""fr"":""1/2 Vacances"",""it"":""1/2 Vacanze""}','{""de"":""½Fe"",""en"":""½Va"",""fr"":""½Va"",""it"":""½Va""}',false,false,false,false,'2022-07-16 09:39:31.963','admin','2022-10-24 06:15:57.852','admin',NULL,false,'' ),
	                           ('a04f8e87-8966-47c0-b293-931ea4f949ae','#808000',1,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Militär"",""en"":""Military"",""fr"":""Militaires"",""it"":""Militare""}','{""de"":""M"",""en"":""M"",""fr"":""M"",""it"":""M""}',false,true,true,true,'2022-07-16 09:33:21.914','admin','2023-03-24 09:49:36.806','admin',NULL,false,'' ),
	                           ('72c43150-bd93-4a41-be99-9d7a603ff596','#808080',1,1.0,'{""de"":""Tag das nicht zur Planung zu Verfügung steht"",""en"":""Day that is not available for planning"",""fr"":""Jour non disponible pour la planification"",""it"":""Giorno non disponibile per la pianificazione""}',false,'{""de"":""Gesperrter Tag"",""en"":""Indisposed"",""fr"":""Indisposé"",""it"":""Indisposizione""}','{""de"":""G"",""en"":""I"",""fr"":""I"",""it"":""I""}',false,true,true,true,'2022-07-16 09:37:15.104','admin','2023-06-04 06:07:49.878','admin',NULL,false,'' ),
	                           ('b8e3c5a1-2d4f-4e6b-9c8a-1f2e3d4c5b6a','#c4e8a6',0,0.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',true,'{""de"":""Pause unbezahlt"",""en"":""Unpaid break"",""fr"":""Pause non payée"",""it"":""Pausa non retribuita""}','{""de"":""P-"",""en"":""P-"",""fr"":""P-"",""it"":""P-""}',false,true,true,true,'2025-12-06 17:00:00.000','admin','2025-12-06 17:00:00.000','admin',NULL,false,'' ),
	                           ('c9f4d6b2-3e5a-4f7c-ad9b-2a3f4e5d6c7b','#c4e8a6',0,0.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',true,'{""de"":""Pause bezahlt"",""en"":""Paid break"",""fr"":""Pause payée"",""it"":""Pausa retribuita""}','{""de"":""P+"",""en"":""P+"",""fr"":""P+"",""it"":""P+""}',false,true,true,true,'2025-12-06 17:00:00.000','admin','2025-12-06 17:00:00.000','admin',NULL,false,'' );
                          "
                                );
        }
    }
}
