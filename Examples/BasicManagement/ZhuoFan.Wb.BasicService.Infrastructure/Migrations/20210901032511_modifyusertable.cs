using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    public partial class modifyusertable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "User",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "User",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                columns: new[] { "CreateTime", "CreatorId" },
                values: new object[] { new DateTime(2021, 9, 1, 11, 25, 11, 57, DateTimeKind.Local).AddTicks(5100), new Guid("58205e0e-1552-4282-bedc-a92d0afb37df") });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "User");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "User",
                type: "nvarchar(20)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 8, 26, 20, 17, 36, 988, DateTimeKind.Local).AddTicks(3957));
        }
    }
}
