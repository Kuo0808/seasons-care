using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeasonsCare.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CareGroupMembers_CareGroups_CareGroupId",
                table: "CareGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_CareGroupMembers_Users_UserId",
                table: "CareGroupMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CareGroups",
                table: "CareGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CareGroupMembers",
                table: "CareGroupMembers");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "CareGroups",
                newName: "care_groups");

            migrationBuilder.RenameTable(
                name: "CareGroupMembers",
                newName: "care_group_members");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "users",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "users",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "users",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "users",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "users",
                newName: "ix_users_email");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "care_groups",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "care_groups",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "care_groups",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "care_groups",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RecipientName",
                table: "care_groups",
                newName: "recipient_name");

            migrationBuilder.RenameColumn(
                name: "InviteCode",
                table: "care_groups",
                newName: "invite_code");

            migrationBuilder.RenameColumn(
                name: "HealthStatus",
                table: "care_groups",
                newName: "health_status");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "care_groups",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "care_groups",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "care_groups",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "care_group_members",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "care_group_members",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "care_group_members",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "care_group_members",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "care_group_members",
                newName: "joined_at");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "care_group_members",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "care_group_members",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "care_group_members",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CareGroupId",
                table: "care_group_members",
                newName: "care_group_id");

            migrationBuilder.RenameIndex(
                name: "IX_CareGroupMembers_UserId",
                table: "care_group_members",
                newName: "ix_care_group_members_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_CareGroupMembers_CareGroupId_UserId",
                table: "care_group_members",
                newName: "ix_care_group_members_care_group_id_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_care_groups",
                table: "care_groups",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_care_group_members",
                table: "care_group_members",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_care_group_members_care_groups_care_group_id",
                table: "care_group_members",
                column: "care_group_id",
                principalTable: "care_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_care_group_members_users_user_id",
                table: "care_group_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_care_group_members_care_groups_care_group_id",
                table: "care_group_members");

            migrationBuilder.DropForeignKey(
                name: "fk_care_group_members_users_user_id",
                table: "care_group_members");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_care_groups",
                table: "care_groups");

            migrationBuilder.DropPrimaryKey(
                name: "pk_care_group_members",
                table: "care_group_members");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "care_groups",
                newName: "CareGroups");

            migrationBuilder.RenameTable(
                name: "care_group_members",
                newName: "CareGroupMembers");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "Users",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "Users",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Users",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_users_email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "CareGroups",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "CareGroups",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CareGroups",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "CareGroups",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "recipient_name",
                table: "CareGroups",
                newName: "RecipientName");

            migrationBuilder.RenameColumn(
                name: "invite_code",
                table: "CareGroups",
                newName: "InviteCode");

            migrationBuilder.RenameColumn(
                name: "health_status",
                table: "CareGroups",
                newName: "HealthStatus");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "CareGroups",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CareGroups",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "CareGroups",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "CareGroupMembers",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CareGroupMembers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "CareGroupMembers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "CareGroupMembers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "joined_at",
                table: "CareGroupMembers",
                newName: "JoinedAt");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "CareGroupMembers",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CareGroupMembers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "CareGroupMembers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "care_group_id",
                table: "CareGroupMembers",
                newName: "CareGroupId");

            migrationBuilder.RenameIndex(
                name: "ix_care_group_members_user_id",
                table: "CareGroupMembers",
                newName: "IX_CareGroupMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_care_group_members_care_group_id_user_id",
                table: "CareGroupMembers",
                newName: "IX_CareGroupMembers_CareGroupId_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CareGroups",
                table: "CareGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CareGroupMembers",
                table: "CareGroupMembers",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CareGroupMembers_CareGroups_CareGroupId",
                table: "CareGroupMembers",
                column: "CareGroupId",
                principalTable: "CareGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CareGroupMembers_Users_UserId",
                table: "CareGroupMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
