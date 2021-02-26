using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Power.BasicManagement.Infrastructure.Migrations
{
    public partial class init002 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SysDict",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CodeType = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    Desc = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    IsInitData = table.Column<ulong>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SysDict", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SysDict");
        }
    }
}
