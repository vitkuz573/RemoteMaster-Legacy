using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddOrganizationConfigurationImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Locality_State_Country",
                table: "Organizations",
                columns: new[] { "Locality", "State", "Country" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_Locality_State_Country",
                table: "Organizations");
        }
    }
}
