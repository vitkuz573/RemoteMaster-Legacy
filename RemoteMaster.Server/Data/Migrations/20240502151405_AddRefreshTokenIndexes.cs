using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Expires",
                table: "RefreshTokens",
                column: "Expires");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens",
                column: "Revoked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Expires",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens");
        }
    }
}
