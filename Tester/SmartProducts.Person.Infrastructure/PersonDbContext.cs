using System;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SmartProducts.Person.Domain.Entities;

namespace SmartProducts.Person.Infrastructure
{
    public class PersonDbContext : ScsDbContext
    {
        private const string TablePrefix = "Person";
        public PersonDbContext(DbContextOptions<PersonDbContext> options) : base(options, TablePrefix)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
        }

        public virtual DbSet<PersonInfoEntity> PersonInfos { get; set; }
        public virtual DbSet<PersonnelQualificationEntity> PersonnelQualifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PersonInfoEntity>(entity =>
            {
                OnModelCreatingBaseEntityAndTableKey(entity);
                entity.Property(x => x.MedicalHistory).HasColumnType("bit");
                entity.Property(x => x.Name).HasColumnType("varchar(20)");
                entity.Property(x => x.Mobilephone).HasColumnType("varchar(20)");
                entity.Property(x => x.IDNo).HasColumnType("varchar(20)");
                entity.Property(x => x.WorkerType).HasColumnType("varchar(20)");
                entity.Property(x => x.ConstructionUnitId).HasColumnType("varchar(36)");
                entity.Property(x => x.WorkCard).HasColumnType("varchar(36)");
                entity.Property(x => x.QrCodeUrl).HasColumnType("varchar(200)");
                entity.Property(x => x.PortraitUrl).HasColumnType("varchar(200)");
            });

            modelBuilder.Entity<PersonnelQualificationEntity>(entity =>
            {
                OnModelCreatingBaseEntityAndTableKey(entity);
                //人员和资质处理一对多的关系
                entity.HasOne(x => x.PersonInfo)
                    .WithMany(x => x.PersonnelQualifications)
                    .OnDelete(DeleteBehavior.Cascade);//级联删除
                entity.Property(x => x.CertificateName).HasColumnType("varchar(36)");
                entity.Property(x => x.CertificateNO).HasColumnType("varchar(36)");
                entity.Property(x => x.CertificatePic).HasColumnType("varchar(200)");
                entity.Property(x => x.PersonId).HasColumnType("varchar(36)");
            });
        }
    }
}
