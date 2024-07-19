using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddRestrictDeleteBehaviorToComputers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computers_OrganizationalUnits_ParentId",
                table: "Computers");

            migrationBuilder.AddForeignKey(
                name: "FK_Computers_OrganizationalUnits_ParentId",
                table: "Computers",
                column: "ParentId",
                principalTable: "OrganizationalUnits",
                principalColumn: "NodeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computers_OrganizationalUnits_ParentId",
                table: "Computers");

            migrationBuilder.AddForeignKey(
                name: "FK_Computers_OrganizationalUnits_ParentId",
                table: "Computers",
                column: "ParentId",
                principalTable: "OrganizationalUnits",
                principalColumn: "NodeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
