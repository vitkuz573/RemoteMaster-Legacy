using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddPreventDeleteRootAdministratorTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_PreventDeleteRootAdministrator
                ON AspNetUsers
                INSTEAD OF DELETE
                AS
                BEGIN
                    DECLARE @rootAdminRoleId NVARCHAR(450);
                    DECLARE @deletedUserId NVARCHAR(450);

                    -- Get the ID of the RootAdministrator role
                    SELECT @rootAdminRoleId = Id 
                    FROM AspNetRoles 
                    WHERE Name = 'RootAdministrator';

                    -- Check if any of the deleted users have the RootAdministrator role
                    IF EXISTS (
                        SELECT 1 
                        FROM deleted d
                        INNER JOIN AspNetUserRoles ur ON d.Id = ur.UserId
                        WHERE ur.RoleId = @rootAdminRoleId
                    )
                    BEGIN
                        -- If such a user exists, raise an error
                        RAISERROR ('RootAdministrator cannot be deleted.', 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END

                    -- If the check passes, delete the record
                    DELETE FROM AspNetUsers
                    WHERE Id IN (SELECT Id FROM deleted);
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removing the trigger in case of rollback
            migrationBuilder.Sql("DROP TRIGGER TR_PreventDeleteRootAdministrator");
        }
    }
}
