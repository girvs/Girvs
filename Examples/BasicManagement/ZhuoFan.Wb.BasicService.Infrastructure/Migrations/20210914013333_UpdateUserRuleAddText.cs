using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    public partial class UpdateUserRuleAddText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FieldValueText",
                table: "UserRule",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 9, 14, 9, 33, 33, 331, DateTimeKind.Local).AddTicks(3390));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldValueText",
                table: "UserRule");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 9, 4, 11, 9, 24, 805, DateTimeKind.Local).AddTicks(3740));
        }
    }
}
