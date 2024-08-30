using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.CertificateDbContextMigrations
{
    /// <inheritdoc />
    public partial class UpdateCrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrlInfos");

            migrationBuilder.AddColumn<int>(
                name: "CrlId",
                table: "RevokedCertificates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Crl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crl", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevokedCertificates_CrlId",
                table: "RevokedCertificates",
                column: "CrlId");

            migrationBuilder.AddForeignKey(
                name: "FK_RevokedCertificates_Crl_CrlId",
                table: "RevokedCertificates",
                column: "CrlId",
                principalTable: "Crl",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RevokedCertificates_Crl_CrlId",
                table: "RevokedCertificates");

            migrationBuilder.DropTable(
                name: "Crl");

            migrationBuilder.DropIndex(
                name: "IX_RevokedCertificates_CrlId",
                table: "RevokedCertificates");

            migrationBuilder.DropColumn(
                name: "CrlId",
                table: "RevokedCertificates");

            migrationBuilder.CreateTable(
                name: "CrlInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CrlHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CrlNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextUpdate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrlInfos", x => x.Id);
                });
        }
    }
}
