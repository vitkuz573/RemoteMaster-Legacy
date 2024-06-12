using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemoteMaster.Server.Data.Migrations.ApplicationDbContextMigrations
{
    /// <inheritdoc />
    public partial class AddRootAdministratorTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_EnsureSingleRootAdministrator
                ON AspNetUserRoles
                INSTEAD OF INSERT
                AS
                BEGIN
                    DECLARE @roleId NVARCHAR(450);
                    DECLARE @newUserId NVARCHAR(450);
                    DECLARE @rootAdminRoleId NVARCHAR(450);

                    -- Get the ID of the RootAdministrator role
                    SELECT @rootAdminRoleId = Id 
                    FROM AspNetRoles 
                    WHERE Name = 'RootAdministrator';

                    -- Check if the inserted record has the RootAdministrator role
                    SELECT @roleId = RoleId, @newUserId = UserId
                    FROM inserted
                    WHERE RoleId = @rootAdminRoleId;

                    IF @roleId IS NOT NULL
                    BEGIN
                        -- Check if there is already a user with this role
                        IF EXISTS (SELECT 1 FROM AspNetUserRoles WHERE RoleId = @rootAdminRoleId)
                        BEGIN
                            -- If such a user already exists, raise an error
                            RAISERROR ('There can only be one RootAdministrator.', 16, 1);
                            ROLLBACK TRANSACTION;
                            RETURN;
                        END
                    END

                    -- If the check passes, insert the record
                    INSERT INTO AspNetUserRoles (UserId, RoleId)
                    SELECT UserId, RoleId
                    FROM inserted;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removing the trigger in case of rollback
            migrationBuilder.Sql("DROP TRIGGER TR_EnsureSingleRootAdministrator");
        }
    }
}
