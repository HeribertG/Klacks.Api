using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPikettOnCallAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO public.absence (id,color,default_length,default_value,description,hide_in_gantt,name,abbreviation,undeletable,with_holiday,with_saturday,with_sunday,applies_to_container,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,is_unpaid ) VALUES
                ('5cb57c1c-ea82-455c-92c6-7920a0d6b19f','#ffa500',1,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Pikett"",""en"":""On-call"",""fr"":""Piquet"",""it"":""Picchetto""}','{""de"":""Pik"",""en"":""OC"",""fr"":""Pi"",""it"":""Pic""}',false,true,true,true,false,'2026-07-27 00:00:00','admin','2026-07-27 00:00:00','admin',NULL,false,'',false );
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM public.absence WHERE id = '5cb57c1c-ea82-455c-92c6-7920a0d6b19f';");
        }
    }
}
