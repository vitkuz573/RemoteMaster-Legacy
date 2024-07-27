using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class RenameNodeIdToId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "Organizations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "OrganizationalUnits",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "NodeId",
                table: "Computers",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Organizations",
                newName: "NodeId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OrganizationalUnits",
                newName: "NodeId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Computers",
                newName: "NodeId");
        }
    }
}
