using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.NodesDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddOrganizationToOrganizationalUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Nodes",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Nodes");
        }
    }
}
