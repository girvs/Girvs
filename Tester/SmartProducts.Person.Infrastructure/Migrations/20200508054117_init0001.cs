using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SmartProducts.Person.Infrastructure.Migrations
{
    public partial class init0001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IP_PersonInfo",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(nullable: true),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    Creator = table.Column<string>(type: "varchar(36)", nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Name = table.Column<string>(type: "varchar(20)", nullable: true),
                    Sex = table.Column<int>(nullable: false),
                    PoliticOutlook = table.Column<int>(nullable: false),
                    Education = table.Column<int>(nullable: false),
                    Mobilephone = table.Column<string>(type: "varchar(20)", nullable: true),
                    IDNo = table.Column<string>(type: "varchar(20)", nullable: true),
                    WorkType = table.Column<int>(nullable: false),
                    MedicalHistory = table.Column<bool>(type: "bit", nullable: false),
                    WorkerType = table.Column<string>(type: "varchar(20)", nullable: false),
                    ConstructionUnitId = table.Column<string>(type: "varchar(36)", nullable: true),
                    CurrentInAreaId = table.Column<string>(nullable: true),
                    HeadPortraitId = table.Column<int>(nullable: false),
                    WorkCard = table.Column<string>(type: "varchar(36)", nullable: true),
                    QrCodeUrl = table.Column<string>(type: "varchar(200)", nullable: true),
                    State = table.Column<int>(nullable: false),
                    PortraitUrl = table.Column<string>(type: "varchar(200)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IP_PersonInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IP_PersonnelQualification",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(nullable: true),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    Creator = table.Column<string>(type: "varchar(36)", nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    CertificateName = table.Column<string>(type: "varchar(36)", nullable: true),
                    CertificateNO = table.Column<string>(type: "varchar(36)", nullable: true),
                    CertificateDeadline = table.Column<DateTime>(nullable: false),
                    CertificatePic = table.Column<string>(type: "varchar(200)", nullable: true),
                    PersonId = table.Column<string>(type: "varchar(36)", nullable: true),
                    PersonInfoId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IP_PersonnelQualification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IP_PersonnelQualification_IP_PersonInfo_PersonInfoId",
                        column: x => x.PersonInfoId,
                        principalTable: "IP_PersonInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IP_PersonnelQualification_PersonInfoId",
                table: "IP_PersonnelQualification",
                column: "PersonInfoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IP_PersonnelQualification");

            migrationBuilder.DropTable(
                name: "IP_PersonInfo");
        }
    }
}
