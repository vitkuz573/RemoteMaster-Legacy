using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class CreateCertificateRenewalTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Organizations",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "CertificateRenewalTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComputerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAttemptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRenewalTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRenewalTasks_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateRenewalTasks_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRenewalTasks_ComputerId",
                table: "CertificateRenewalTasks",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRenewalTasks_OrganizationId",
                table: "CertificateRenewalTasks",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateRenewalTasks");

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);
        }
    }
}
