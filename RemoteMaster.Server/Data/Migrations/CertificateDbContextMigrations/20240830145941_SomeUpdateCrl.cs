using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.CertificateDbContextMigrations
{
    /// <inheritdoc />
    public partial class SomeUpdateCrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RevokedCertificates_Crl_CrlId",
                table: "RevokedCertificates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Crl",
                table: "Crl");

            migrationBuilder.RenameTable(
                name: "Crl",
                newName: "CertificateRevocationLists");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CertificateRevocationLists",
                table: "CertificateRevocationLists",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevokedCertificates_CertificateRevocationLists_CrlId",
                table: "RevokedCertificates",
                column: "CrlId",
                principalTable: "CertificateRevocationLists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RevokedCertificates_CertificateRevocationLists_CrlId",
                table: "RevokedCertificates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CertificateRevocationLists",
                table: "CertificateRevocationLists");

            migrationBuilder.RenameTable(
                name: "CertificateRevocationLists",
                newName: "Crl");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Crl",
                table: "Crl",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RevokedCertificates_Crl_CrlId",
                table: "RevokedCertificates",
                column: "CrlId",
                principalTable: "Crl",
                principalColumn: "Id");
        }
    }
}
