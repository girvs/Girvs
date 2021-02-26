using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Power.BasicManagement.Infrastructure.Migrations
{
    public partial class init001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BasalPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AllowMask = table.Column<long>(type: "bigint", nullable: false),
                    DenyMask = table.Column<long>(type: "bigint", nullable: false),
                    AppliedID = table.Column<Guid>(nullable: false),
                    AppliedObjectID = table.Column<Guid>(nullable: false),
                    AppliedObjectType = table.Column<int>(type: "int", nullable: false),
                    ValidateObjectID = table.Column<int>(nullable: false),
                    ValidateObjectType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasalPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(type: "nvarchar(30)", nullable: true),
                    Desc = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    IsInitData = table.Column<ulong>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserAccount = table.Column<string>(type: "varchar(36)", nullable: true),
                    UserPassword = table.Column<string>(type: "varchar(36)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    ContactNumber = table.Column<string>(type: "varchar(12)", nullable: true),
                    OtherId = table.Column<string>(type: "varchar(36)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    IsInitData = table.Column<ulong>(type: "bit", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BasalPermission");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
