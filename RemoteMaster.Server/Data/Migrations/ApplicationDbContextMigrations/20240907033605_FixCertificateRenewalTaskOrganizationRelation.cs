using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class FixCertificateRenewalTaskOrganizationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRenewalTasks_Organizations_OrganizationId1",
                table: "CertificateRenewalTasks");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRenewalTasks_OrganizationId1",
                table: "CertificateRenewalTasks");

            migrationBuilder.DropColumn(
                name: "OrganizationId1",
                table: "CertificateRenewalTasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId1",
                table: "CertificateRenewalTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRenewalTasks_OrganizationId1",
                table: "CertificateRenewalTasks",
                column: "OrganizationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRenewalTasks_Organizations_OrganizationId1",
                table: "CertificateRenewalTasks",
                column: "OrganizationId1",
                principalTable: "Organizations",
                principalColumn: "Id");
        }
    }
}
