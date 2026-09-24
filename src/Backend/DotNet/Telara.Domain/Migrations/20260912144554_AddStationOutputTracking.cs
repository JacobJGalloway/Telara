using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telara.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddStationOutputTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "target_output_per_shift",
                schema: "dbo",
                table: "Stations",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StationOutputRecords",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    facility_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    station_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    recorded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    units_produced = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationOutputRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_StationOutputRecords_Stations_facility_id_station_id",
                        columns: x => new { x.facility_id, x.station_id },
                        principalSchema: "dbo",
                        principalTable: "Stations",
                        principalColumns: new[] { "facility_id", "station_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StationOutputRecords_facility_id_station_id_recorded_at_utc",
                schema: "dbo",
                table: "StationOutputRecords",
                columns: new[] { "facility_id", "station_id", "recorded_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationOutputRecords",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "target_output_per_shift",
                schema: "dbo",
                table: "Stations");
        }
    }
}
