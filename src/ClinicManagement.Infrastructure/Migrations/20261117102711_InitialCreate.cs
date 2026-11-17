using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261117102711_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AllergyTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AllergyTypes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Clinic",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clinic", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                DateOfBirth = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Providers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Specialty = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ClinicId = table.Column<Guid>(type: "TEXT", nullable: false),
                ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Providers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Providers_Clinic_ClinicId",
                    column: x => x.ClinicId,
                    principalTable: "Clinic",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Providers_Clinic_ProviderId",
                    column: x => x.ProviderId,
                    principalTable: "Clinic",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Allergies",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PatientId1 = table.Column<Guid>(type: "TEXT", nullable: false),
                AllergyTypeId1 = table.Column<Guid>(type: "TEXT", nullable: false),
                Severity = table.Column<int>(type: "INTEGER", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                AllergyTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Allergies", x => x.Id);
                table.ForeignKey(
                    name: "FK_Allergies_AllergyTypes_AllergyTypeId",
                    column: x => x.AllergyTypeId,
                    principalTable: "AllergyTypes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Allergies_AllergyTypes_AllergyTypeId1",
                    column: x => x.AllergyTypeId1,
                    principalTable: "AllergyTypes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Allergies_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Allergies_Patients_PatientId1",
                    column: x => x.PatientId1,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PatientId1 = table.Column<Guid>(type: "TEXT", nullable: false),
                ProviderId = table.Column<Guid>(type: "TEXT", nullable: false),
                AppointmentDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ReasonForVisit = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                AppointmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                PatientId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Appointments_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Appointments_Patients_PatientId1",
                    column: x => x.PatientId1,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Appointments_Providers_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Providers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Appointments_Providers_ProviderId",
                    column: x => x.ProviderId,
                    principalTable: "Providers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Allergies_AllergyTypeId",
            table: "Allergies",
            column: "AllergyTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_Allergies_AllergyTypeId1",
            table: "Allergies",
            column: "AllergyTypeId1");

        migrationBuilder.CreateIndex(
            name: "IX_Allergies_PatientId",
            table: "Allergies",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Allergies_PatientId1",
            table: "Allergies",
            column: "PatientId1");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_AppointmentId",
            table: "Appointments",
            column: "AppointmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_PatientId",
            table: "Appointments",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_PatientId1",
            table: "Appointments",
            column: "PatientId1");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_ProviderId",
            table: "Appointments",
            column: "ProviderId");

        migrationBuilder.CreateIndex(
            name: "IX_Providers_ClinicId",
            table: "Providers",
            column: "ClinicId");

        migrationBuilder.CreateIndex(
            name: "IX_Providers_ProviderId",
            table: "Providers",
            column: "ProviderId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Allergies");

        migrationBuilder.DropTable(
            name: "Appointments");

        migrationBuilder.DropTable(
            name: "AllergyTypes");

        migrationBuilder.DropTable(
            name: "Patients");

        migrationBuilder.DropTable(
            name: "Providers");

        migrationBuilder.DropTable(
            name: "Clinic");
    }
}
