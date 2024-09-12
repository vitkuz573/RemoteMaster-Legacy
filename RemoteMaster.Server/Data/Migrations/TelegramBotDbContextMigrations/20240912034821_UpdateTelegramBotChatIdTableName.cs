using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.TelegramBotDbContextMigrations
{
    /// <inheritdoc />
    public partial class UpdateTelegramBotChatIdTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramBotChatId_TelegramBots_TelegramBotId",
                table: "TelegramBotChatId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramBotChatId",
                table: "TelegramBotChatId");

            migrationBuilder.RenameTable(
                name: "TelegramBotChatId",
                newName: "TelegramBotChatIds");

            migrationBuilder.RenameIndex(
                name: "IX_TelegramBotChatId_TelegramBotId",
                table: "TelegramBotChatIds",
                newName: "IX_TelegramBotChatIds_TelegramBotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramBotChatIds",
                table: "TelegramBotChatIds",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramBotChatIds_TelegramBots_TelegramBotId",
                table: "TelegramBotChatIds",
                column: "TelegramBotId",
                principalTable: "TelegramBots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TelegramBotChatIds_TelegramBots_TelegramBotId",
                table: "TelegramBotChatIds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramBotChatIds",
                table: "TelegramBotChatIds");

            migrationBuilder.RenameTable(
                name: "TelegramBotChatIds",
                newName: "TelegramBotChatId");

            migrationBuilder.RenameIndex(
                name: "IX_TelegramBotChatIds_TelegramBotId",
                table: "TelegramBotChatId",
                newName: "IX_TelegramBotChatId_TelegramBotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramBotChatId",
                table: "TelegramBotChatId",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TelegramBotChatId_TelegramBots_TelegramBotId",
                table: "TelegramBotChatId",
                column: "TelegramBotId",
                principalTable: "TelegramBots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
