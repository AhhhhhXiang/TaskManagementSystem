using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Data.Migrations.Migrations
{
    public partial class UpdateTaskTableAddCommentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1", "f979e43f-84be-4815-9664-414051722f80" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "f979e43f-84be-4815-9664-414051722f80");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTask",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "priorityStatus",
                table: "ProjectTask",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "51a74591-460f-4ada-b186-9db4afaadea2");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "258307fa-d5c7-498e-96b1-906099490b54");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "1a18d59c-c77d-42a0-ad9f-bde332c45aff", 0, "d6b68fcd-e096-4dd7-aead-f132cc627177", "admin@example.com", false, false, null, "ADMIN@EXAMPLE.COM", "ADMIN", "AQAAAAEAACcQAAAAEMM94sHP43W4UUi9yFC6EVb5NUX8u38avFYxE4if1DlOSaoQxQro9Hi+0PLmhjuScA==", null, false, "05f846b2-96de-4bec-bd12-6899dd71a489", false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "1", "1a18d59c-c77d-42a0-ad9f-bde332c45aff" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "1", "1a18d59c-c77d-42a0-ad9f-bde332c45aff" });

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1a18d59c-c77d-42a0-ad9f-bde332c45aff");

            migrationBuilder.DropColumn(
                name: "priorityStatus",
                table: "ProjectTask");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTask",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1",
                column: "ConcurrencyStamp",
                value: "9d1e11a6-6e12-42fa-bac5-fec9ddd2b4a1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2",
                column: "ConcurrencyStamp",
                value: "0adcdfc3-d1ee-4941-a965-a21adb5d740b");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "f979e43f-84be-4815-9664-414051722f80", 0, "c29ca96d-ae43-4133-baab-53939ec1d6c7", "admin@example.com", false, false, null, "ADMIN@EXAMPLE.COM", "ADMIN", "AQAAAAEAACcQAAAAEEGWdaBieYqyBAu90UfXBGn76WSCQQO0l4FQSj8VvzDYIxuTKSxlQssEiRt8OIhEVg==", null, false, "fd329b03-b40c-44ae-8f57-5404b48741a1", false, "admin" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "1", "f979e43f-84be-4815-9664-414051722f80" });
        }
    }
}
