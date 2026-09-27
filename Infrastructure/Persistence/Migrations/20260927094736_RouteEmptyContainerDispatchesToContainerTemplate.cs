using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Re-points already persisted empty_container inbox messages from the schedule to the container's
    /// own slot-template page. New messages get that route from EmptyContainerTriggerEvent, but a stored
    /// message is never re-created while its condition stays open (dedup), so without this backfill the
    /// old rows would keep sending the user to the schedule. The DedupKey of this kind is the container
    /// shift id, and only rows whose key really is a uuid are touched.
    /// </summary>
    public partial class RouteEmptyContainerDispatchesToContainerTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE agent_trigger_dispatches " +
                "SET action_route = '/workplace/container-template/' || dedup_key, action_params_json = NULL " +
                "WHERE trigger_kind = 'empty_container' " +
                "AND action_route = '/workplace/schedule' " +
                "AND dedup_key ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE agent_trigger_dispatches " +
                "SET action_route = '/workplace/schedule' " +
                "WHERE trigger_kind = 'empty_container' " +
                "AND action_route LIKE '/workplace/container-template/%';");
        }
    }
}
