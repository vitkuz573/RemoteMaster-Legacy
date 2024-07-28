using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddReplacedByTokenIdConstraintToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshTokens_ReplacedByTokenId_Required",
                table: "RefreshTokens",
                sql: "[RevocationReason] <> 'Replaced' OR [ReplacedByTokenId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshTokens_ReplacedByTokenId_Required",
                table: "RefreshTokens");
        }
    }
}
