using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    public partial class modifyUserRuleFieldLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FieldValue",
                table: "UserRule",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 9, 4, 11, 9, 24, 805, DateTimeKind.Local).AddTicks(3740));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FieldValue",
                table: "UserRule",
                type: "varchar(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 9, 1, 11, 25, 11, 57, DateTimeKind.Local).AddTicks(5100));
        }
    }
}
