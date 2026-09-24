using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telara.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddStationRoutingCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Stations_LoadingDock_NoSuccessor",
                schema: "dbo",
                table: "Stations",
                sql: "NOT ([is_loading_dock] = 1 AND [next_station_id] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stations_NextStationId_NotSelf",
                schema: "dbo",
                table: "Stations",
                sql: "[next_station_id] IS NULL OR [next_station_id] <> [station_id]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Stations_LoadingDock_NoSuccessor",
                schema: "dbo",
                table: "Stations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stations_NextStationId_NotSelf",
                schema: "dbo",
                table: "Stations");
        }
    }
}
