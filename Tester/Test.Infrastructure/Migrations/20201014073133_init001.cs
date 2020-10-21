using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Test.Infrastructure.Migrations
{
    public partial class init001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(maxLength: 36, nullable: false),
                    TenantId = table.Column<Guid>(nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Creator = table.Column<Guid>(maxLength: 36, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    Desc = table.Column<string>(type: "nvarchar(200)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(maxLength: 36, nullable: false),
                    TenantId = table.Column<Guid>(nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    Creator = table.Column<Guid>(maxLength: 36, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    UserAccount = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    UserPassword = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(12)", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    RoleId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "ContactNumber", "CreateTime", "Creator", "State", "TenantId", "UpdateTime", "UserAccount", "UserName", "UserPassword", "UserType" },
                values: new object[] { new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"), null, new DateTime(2020, 10, 14, 15, 31, 33, 365, DateTimeKind.Local).AddTicks(2746), new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"), 0, new Guid("f339be29-7ce2-4876-bcca-d3abe3d16f75"), new DateTime(2020, 10, 14, 15, 31, 33, 365, DateTimeKind.Local).AddTicks(2771), "admin", "系统管理员", "21232F297A57A5A743894A0E4A801FC3", 0 });

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
