using Microsoft.EntityFrameworkCore.Migrations;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    public partial class useraddfieldtenant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "User",
                type: "varchar(36)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "User");
        }
    }
}
