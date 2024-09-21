using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
  public class AbsencesSeed
  {
    public static void SeedData(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.Sql(
                          @"INSERT INTO public.absence (id,color,default_length,default_value,description_de,description_en,description_fr,description_it,hide_in_gantt,name_de,name_en,name_fr,name_it,undeletable,with_holiday,with_saturday,with_sunday,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted ) VALUES
	                           ('1070d7e6-f314-4d20-bc18-98c5357a4f89','#ff0000',1,1.0,NULL,NULL,NULL,NULL,false,'Krankheit/Unfall','Illness/Accident','Maladie/Accident','Malattia/Infortunio',false,true,true,true,'2022-07-16 09:34:42.697','admin','2022-07-16 09:38:20.871','admin',NULL,false,'' ),
	                           ('1d5a1964-7fad-4da9-945c-3ad00c0edaa8','#ff0000',1,0.5,NULL,NULL,NULL,NULL,false,'Krankheit/Unfall 50%','Illness/Accident 50%','Maladie/Accident 50%','Malattia/Infortunio 50%',false,true,true,true,'2022-07-16 09:38:06.909','admin','2022-08-25 21:47:33.696','admin',NULL,false,'' ),
	                           ('dca0367a-530e-4eae-be34-b19d2262587e','#ff80c0',1,1.0,NULL,NULL,NULL,NULL,false,'Schulung','Training','Formation','Formazione',false,true,true,true,'2022-07-16 09:35:54.261','admin','2022-07-16 09:38:26.506','admin',NULL,false,'' ),
	                           ('15ee57e6-31d1-492e-bf83-d3c386ef7472','#0080c0',5,1.0,NULL,NULL,NULL,NULL,false,'Ferien','Vacancies','Vacances','Vacanze',false,false,false,false,'2022-07-14 18:23:15.037','admin','2022-10-15 09:56:18.505','admin',NULL,false,'' ),
	                           ('53851d0a-ff7f-460a-82a0-481aa3547d7e','#0080c0',5,0.5,NULL,NULL,NULL,NULL,false,'1/2 Ferien','1/2 Vacancies','1/2 Vacances','1/2 Vacanze',false,false,false,false,'2022-07-16 09:39:31.963','admin','2022-10-24 06:15:57.852','admin',NULL,false,'' ),
	                           ('a04f8e87-8966-47c0-b293-931ea4f949ae','#808000',1,1.0,NULL,NULL,NULL,NULL,false,'Militär','Military','Militaires','Militare',false,true,true,true,'2022-07-16 09:33:21.914','admin','2023-03-24 09:49:36.806','admin',NULL,false,'' ),
	                           ('72c43150-bd93-4a41-be99-9d7a603ff596','#808080',1,1.0,'Tag das nicht zur Planung zu Verfügung steht','Day that is not available for planning','Jour non disponible pour la planification','Giorno non disponibile per la pianificazione',false,'Gesperrter Tag','Indisposed','Indisposé','Indisposizione',false,true,true,true,'2022-07-16 09:37:15.104','admin','2023-06-04 06:07:49.878','admin',NULL,false,'' );
                          "
                          );
    }
  }
}
