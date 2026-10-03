using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitaSignal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientCodeAndReadingsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Patients",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Patients"
                SET "Code" = 'PAC-' || upper(substr(replace("Id"::text, '-', ''), 1, 12));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VitalReadings_PatientId_RecordedAtUtc_Id",
                table: "VitalReadings",
                columns: new[] { "PatientId", "RecordedAtUtc", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Code",
                table: "Patients",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VitalReadings_Patients_PatientId",
                table: "VitalReadings",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalReadings_Patients_PatientId",
                table: "VitalReadings");

            migrationBuilder.DropIndex(
                name: "IX_VitalReadings_PatientId_RecordedAtUtc_Id",
                table: "VitalReadings");

            migrationBuilder.DropIndex(
                name: "IX_Patients_Code",
                table: "Patients");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""UPDATE "Patients" SET "DisplayName" = "Code";""");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Patients");
        }
    }
}
