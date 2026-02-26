using BioLab.Models;
using BioLIS.Models;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Data
{
    public class LaboratorioContext:DbContext
    {
        public LaboratorioContext(DbContextOptions<LaboratorioContext> options) : base(options) { }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<SampleType> SampleTypes { get; set; }
        public DbSet<LabTest> LabTests { get; set; }
        public DbSet<ReferenceRange> ReferenceRanges { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSecurity> UsersSecurity { get; set; }
        public DbSet<UserValidation> Usersvalidations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de entidades
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(e => e.DoctorID);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LicenseNumber).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
            });

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients");
                entity.HasKey(e => e.PatientID);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(1);
                entity.Property(e => e.BirthDate).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PhotoFilename).HasMaxLength(200);
            });

            modelBuilder.Entity<SampleType>(entity =>
            {
                entity.ToTable("SampleTypes");
                entity.HasKey(e => e.SampleID);
                entity.Property(e => e.SampleName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ContainerColor).HasMaxLength(20);
            });

            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.ToTable("LabTests");
                entity.HasKey(e => e.TestID);
                entity.Property(e => e.TestName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Units).HasMaxLength(20);

                entity.HasOne(d => d.SampleType)
                      .WithMany(p => p.LabTests)
                      .HasForeignKey(d => d.SampleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReferenceRange>(entity =>
            {
                entity.ToTable("ReferenceRanges");
                entity.HasKey(e => e.RangeID);
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(1);
                entity.Property(e => e.MinVal).HasColumnType("decimal(10,2)");
                entity.Property(e => e.MaxVal).HasColumnType("decimal(10,2)");

                entity.HasOne(d => d.LabTest)
                      .WithMany(p => p.ReferenceRanges)
                      .HasForeignKey(d => d.TestID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.OrderNumber).HasMaxLength(20);
                entity.HasIndex(e => e.OrderNumber).IsUnique();

                entity.HasOne(d => d.Patient)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.PatientID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Doctor)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.DoctorID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TestResult>(entity =>
            {
                entity.ToTable("TestResults");
                entity.HasKey(e => e.ResultID);
                entity.Property(e => e.ResultValue).HasColumnType("decimal(10,2)");
                entity.Property(e => e.IsAbnormal).HasDefaultValue(false);
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(d => d.Order)
                      .WithMany(p => p.TestResults)
                      .HasForeignKey(d => d.OrderID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.LabTest)
                      .WithMany(p => p.TestResults)
                      .HasForeignKey(d => d.TestID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.PasswordText).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);

                entity.HasOne(d => d.Doctor)
                      .WithMany()
                      .HasForeignKey(d => d.DoctorID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //es una vista sin key
            modelBuilder.Entity<UserValidation>().ToView("V_UserValidation").HasNoKey();

        }

    }

}
