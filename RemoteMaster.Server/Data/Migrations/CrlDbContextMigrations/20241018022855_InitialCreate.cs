﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.CrlDbContextMigrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateRevocationLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRevocationLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevokedCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RevocationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CrlId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevokedCertificates_CertificateRevocationLists_CrlId",
                        column: x => x.CrlId,
                        principalTable: "CertificateRevocationLists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RevokedCertificates_CrlId",
                table: "RevokedCertificates",
                column: "CrlId");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedCertificates_SerialNumber",
                table: "RevokedCertificates",
                column: "SerialNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RevokedCertificates");

            migrationBuilder.DropTable(
                name: "CertificateRevocationLists");
        }
    }
}
