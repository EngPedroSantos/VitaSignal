using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitaSignal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VitalAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalAlerts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalAlerts_VitalReadings_ReadingId",
                        column: x => x.ReadingId,
                        principalTable: "VitalReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VitalAlerts_PatientId_RaisedAtUtc_Id",
                table: "VitalAlerts",
                columns: new[] { "PatientId", "RaisedAtUtc", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_VitalAlerts_Pending",
                table: "VitalAlerts",
                columns: new[] { "PatientId", "RaisedAtUtc" },
                filter: "\"AcknowledgedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VitalAlerts_ReadingId",
                table: "VitalAlerts",
                column: "ReadingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VitalAlerts");
        }
    }
}
