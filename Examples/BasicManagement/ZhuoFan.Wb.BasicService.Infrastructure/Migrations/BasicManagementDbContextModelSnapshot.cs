﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZhuoFan.Wb.BasicService.Infrastructure;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Migrations
{
    [DbContext(typeof(BasicManagementDbContext))]
    partial class BasicManagementDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.Property<Guid>("RolesId")
                        .HasColumnType("char(36)");

                    b.Property<Guid>("UsersId")
                        .HasColumnType("char(36)");

                    b.HasKey("RolesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("RoleUser");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.BasalPermission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<long>("AllowMask")
                        .HasColumnType("bigint");

                    b.Property<Guid>("AppliedID")
                        .HasColumnType("char(36)");

                    b.Property<Guid>("AppliedObjectID")
                        .HasColumnType("char(36)");

                    b.Property<int>("AppliedObjectType")
                        .HasColumnType("int");

                    b.Property<long>("DenyMask")
                        .HasColumnType("bigint");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<int>("ValidateObjectID")
                        .HasColumnType("int");

                    b.Property<int>("ValidateObjectType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("BasalPermission");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.Role", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Desc")
                        .HasColumnType("nvarchar(200)");

                    b.Property<ulong>("IsInitData")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(30)");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.HasKey("Id");

                    b.ToTable("Role");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.ServiceDataRule", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("DataSource")
                        .HasColumnType("varchar(500)");

                    b.Property<string>("FieldDesc")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("FieldName")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ModuleName")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("ServiceName")
                        .HasColumnType("varchar(100)");

                    b.Property<int>("UserType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ServiceDataRule");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.ServicePermission", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Permissions")
                        .HasColumnType("text");

                    b.Property<string>("ServiceId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<string>("ServiceName")
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.ToTable("ServicePermission");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<int>("AuthorizeType")
                        .HasColumnType("int");

                    b.Property<string>("ContactNumber")
                        .HasColumnType("varchar(12)");

                    b.Property<ulong>("IsInitData")
                        .HasColumnType("bit");

                    b.Property<string>("OtherId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<int>("State")
                        .HasColumnType("int");

                    b.Property<string>("TenantId")
                        .IsRequired()
                        .HasColumnType("varchar(36)");

                    b.Property<string>("UserAccount")
                        .HasColumnType("varchar(36)");

                    b.Property<string>("UserName")
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("UserPassword")
                        .HasColumnType("varchar(36)");

                    b.Property<int>("UserType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("User");

                    b.HasData(
                        new
                        {
                            Id = new Guid("58205e0e-1552-4282-bedc-a92d0afb37df"),
                            AuthorizeType = 0,
                            IsInitData = 1ul,
                            OtherId = "00000000-0000-0000-0000-000000000000",
                            State = 0,
                            TenantId = "00000000-0000-0000-0000-000000000000",
                            UserAccount = "admin",
                            UserName = "系统管理员",
                            UserPassword = "21232F297A57A5A743894A0E4A801FC3",
                            UserType = 0
                        });
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.UserRules", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("FieldName")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("FieldValue")
                        .HasColumnType("varchar(1024)");

                    b.Property<string>("ModuleName")
                        .HasColumnType("varchar(50)");

                    b.Property<string>("Operate")
                        .HasColumnType("varchar(30)");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("char(36)");

                    b.Property<int>("UserType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserRules");
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.HasOne("ZhuoFan.Wb.BasicService.Domain.Models.Role", null)
                        .WithMany()
                        .HasForeignKey("RolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ZhuoFan.Wb.BasicService.Domain.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.UserRules", b =>
                {
                    b.HasOne("ZhuoFan.Wb.BasicService.Domain.Models.User", null)
                        .WithMany("RulesList")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("ZhuoFan.Wb.BasicService.Domain.Models.User", b =>
                {
                    b.Navigation("RulesList");
                });
#pragma warning restore 612, 618
        }
    }
}