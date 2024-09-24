using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class ContinueRenameComputerToHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRenewalTasks_Hosts_ComputerId",
                table: "CertificateRenewalTasks");

            migrationBuilder.RenameColumn(
                name: "ComputerId",
                table: "CertificateRenewalTasks",
                newName: "HostId");

            migrationBuilder.RenameIndex(
                name: "IX_CertificateRenewalTasks_ComputerId",
                table: "CertificateRenewalTasks",
                newName: "IX_CertificateRenewalTasks_HostId");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRenewalTasks_Hosts_HostId",
                table: "CertificateRenewalTasks",
                column: "HostId",
                principalTable: "Hosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRenewalTasks_Hosts_HostId",
                table: "CertificateRenewalTasks");

            migrationBuilder.RenameColumn(
                name: "HostId",
                table: "CertificateRenewalTasks",
                newName: "ComputerId");

            migrationBuilder.RenameIndex(
                name: "IX_CertificateRenewalTasks_HostId",
                table: "CertificateRenewalTasks",
                newName: "IX_CertificateRenewalTasks_ComputerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRenewalTasks_Hosts_ComputerId",
                table: "CertificateRenewalTasks",
                column: "ComputerId",
                principalTable: "Hosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
