using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Telara.Domain.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "EquipmentTypes",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                schema: "dbo",
                columns: table => new
                {
                    facility_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    station_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    last_operator_action_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => new { x.facility_id, x.station_id });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    first_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assigned_station_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assigned_station_equipment_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    assigned_role_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_assigned_role_id",
                        column: x => x.assigned_role_id,
                        principalSchema: "dbo",
                        principalTable: "Roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StationEquipment",
                schema: "dbo",
                columns: table => new
                {
                    facility_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    station_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    equipment_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    equipment_type_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    last_sensor_reading_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    active_instance_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lease_expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationEquipment", x => new { x.facility_id, x.station_id, x.equipment_id });
                    table.ForeignKey(
                        name: "FK_StationEquipment_EquipmentTypes_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalSchema: "dbo",
                        principalTable: "EquipmentTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StationEquipment_Stations_facility_id_station_id",
                        columns: x => new { x.facility_id, x.station_id },
                        principalSchema: "dbo",
                        principalTable: "Stations",
                        principalColumns: new[] { "facility_id", "station_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    family_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_user_id",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    facility_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    station_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    equipment_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    sensor_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    reading_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    reading_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.id);
                    table.ForeignKey(
                        name: "FK_SensorReadings_StationEquipment_facility_id_station_id_equipment_id",
                        columns: x => new { x.facility_id, x.station_id, x.equipment_id },
                        principalSchema: "dbo",
                        principalTable: "StationEquipment",
                        principalColumns: new[] { "facility_id", "station_id", "equipment_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentTypes_name",
                schema: "dbo",
                table: "EquipmentTypes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_family_id",
                schema: "dbo",
                table: "RefreshTokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_token_hash",
                schema: "dbo",
                table: "RefreshTokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_user_id",
                schema: "dbo",
                table: "RefreshTokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_facility_id_station_id_equipment_id_sensor_id_reading_at_utc",
                schema: "dbo",
                table: "SensorReadings",
                columns: new[] { "facility_id", "station_id", "equipment_id", "sensor_id", "reading_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_StationEquipment_equipment_type_id",
                schema: "dbo",
                table: "StationEquipment",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_assigned_role_id",
                schema: "dbo",
                table: "Users",
                column: "assigned_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                schema: "dbo",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SensorReadings",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "StationEquipment",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EquipmentTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Stations",
                schema: "dbo");
        }
    }
}
