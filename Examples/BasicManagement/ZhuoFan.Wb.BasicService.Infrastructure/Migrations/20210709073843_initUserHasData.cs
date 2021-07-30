using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    public partial class initUserHasData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "ContactNumber", "IsInitData", "OtherId", "State", "UserAccount", "UserName", "UserPassword", "UserType" },
                values: new object[] { new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"), null, 1ul, "00000000-0000-0000-0000-000000000000", 0, "admin", "系统管理员", "21232F297A57A5A743894A0E4A801FC3", 0 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "User",
                keyColumn: "Id",
                keyValue: new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"));
        }
    }
}
