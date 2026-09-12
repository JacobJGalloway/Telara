using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telara.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddStationRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_loading_dock",
                schema: "dbo",
                table: "Stations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "next_station_id",
                schema: "dbo",
                table: "Stations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stations_facility_id_next_station_id",
                schema: "dbo",
                table: "Stations",
                columns: new[] { "facility_id", "next_station_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_Stations_facility_id_next_station_id",
                schema: "dbo",
                table: "Stations",
                columns: new[] { "facility_id", "next_station_id" },
                principalSchema: "dbo",
                principalTable: "Stations",
                principalColumns: new[] { "facility_id", "station_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_Stations_facility_id_next_station_id",
                schema: "dbo",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_Stations_facility_id_next_station_id",
                schema: "dbo",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "is_loading_dock",
                schema: "dbo",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "next_station_id",
                schema: "dbo",
                table: "Stations");
        }
    }
}
