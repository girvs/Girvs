using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Test.Infrastructure.Migrations
{
    public partial class init002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 1, 19, 11, 8, 38, 65, DateTimeKind.Local).AddTicks(4442));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                column: "CreateTime",
                value: new DateTime(2021, 1, 15, 15, 43, 53, 768, DateTimeKind.Local).AddTicks(6792));
        }
    }
}
