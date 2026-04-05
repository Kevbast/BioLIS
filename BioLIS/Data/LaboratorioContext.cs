using BioLIS.Models;
using Microsoft.EntityFrameworkCore;

namespace BioLIS.Data
{
    public class LaboratorioContext : DbContext
    {
        public LaboratorioContext(DbContextOptions<LaboratorioContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
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
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OrderShareToken> OrderShareTokens { get; set; } = null!;
        public DbSet<IntegrationEvent> IntegrationEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Roles
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.RoleID);
            });

            // Configuración de entidades
            modelBuilder.Entity<Doctor>(entity =>
            {
                entity.ToTable("Doctors");
                entity.HasKey(e => e.DoctorID);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LicenseNumber).HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);

                entity.HasIndex(e => e.LicenseNumber)
                      .IsUnique()
                      .HasFilter("[LicenseNumber] IS NOT NULL");
            });

            modelBuilder.Entity<Patient>(entity =>
            {
                entity.ToTable("Patients");
                entity.HasKey(e => e.PatientID);
                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()"); // GUID automático
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(1);
                entity.Property(e => e.BirthDate).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.PhotoFilename).HasMaxLength(200);

                entity.ToTable(t => t.HasCheckConstraint("CK_Patients_Gender", "[Gender] IN ('M','F')"));
            });

            modelBuilder.Entity<SampleType>(entity =>
            {
                entity.ToTable("SampleTypes");
                entity.HasKey(e => e.SampleID);
                entity.Property(e => e.SampleName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ContainerColor).HasMaxLength(20);

                entity.HasIndex(e => e.SampleName).IsUnique();
            });

            modelBuilder.Entity<LabTest>(entity =>
            {
                entity.ToTable("LabTests");
                entity.HasKey(e => e.TestID);
                entity.Property(e => e.TestName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Units).HasMaxLength(20);

                entity.HasIndex(e => new { e.TestName, e.SampleID }).IsUnique();

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

                entity.HasIndex(e => new { e.TestID, e.Gender, e.MinAgeYear, e.MaxAgeYear }).IsUnique();
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_ReferenceRanges_Gender", "[Gender] IN ('M','F','A')");
                    t.HasCheckConstraint("CK_ReferenceRanges_Age", "[MinAgeYear] >= 0 AND [MaxAgeYear] >= [MinAgeYear] AND [MaxAgeYear] <= 120");
                    t.HasCheckConstraint("CK_ReferenceRanges_Value", "[MinVal] <= [MaxVal]");
                });

                entity.HasOne(d => d.LabTest)
                      .WithMany(p => p.ReferenceRanges)
                      .HasForeignKey(d => d.TestID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.OrderID);
                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()"); // GUID Automático
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.OrderNumber).HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pendiente");
                entity.HasIndex(e => e.OrderNumber).IsUnique();

                entity.ToTable(t =>
                    t.HasCheckConstraint("CK_Orders_Status", "[Status] IN ('Pendiente','EnProceso','Completada','Aprobada','Entregada')"));

                entity.HasOne(d => d.Patient)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.PatientID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Doctor)
                      .WithMany(p => p.Orders)
                      .HasForeignKey(d => d.DoctorID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ApprovedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TestResult>(entity =>
            {
                entity.ToTable("TestResults");
                entity.HasKey(e => e.ResultID);
                entity.Property(e => e.ResultValue).HasColumnType("decimal(10,2)");
                entity.Property(e => e.IsAbnormal).HasDefaultValue(false);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.AlertLevel).HasMaxLength(20);
                entity.Property(e => e.EnteredDate).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => new { e.OrderID, e.TestID }).IsUnique();
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_TestResults_AlertLevel", "[AlertLevel] IS NULL OR [AlertLevel] IN ('NORMAL','ANORMAL','CRITICO','SIN_RANGO')");
                    t.HasCheckConstraint("CK_TestResults_ResultValue", "[ResultValue] IS NULL OR [ResultValue] >= 0");
                });

                entity.HasOne(d => d.Order)
                      .WithMany(p => p.TestResults)
                      .HasForeignKey(d => d.OrderID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.LabTest)
                      .WithMany(p => p.TestResults)
                      .HasForeignKey(d => d.TestID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.EnteredByUser)
                      .WithMany()
                      .HasForeignKey(d => d.EnteredBy)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.ModifiedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.ModifiedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasFilter("[Email] IS NOT NULL AND [Email] <> ''");
                entity.Property(e => e.PasswordText).IsRequired().HasMaxLength(100);

                // NOTA IMPORTANTE: Se elimina el CK_Users_Role porque ahora usamos RoleID como ForeignKey

                entity.HasOne(d => d.Role)
                      .WithMany()
                      .HasForeignKey(d => d.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Doctor)
                      .WithMany()
                      .HasForeignKey(d => d.DoctorID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // OrdenShareTokens
            modelBuilder.Entity<OrderShareToken>(entity =>
            {
                entity.ToTable("OrderShareTokens");
                entity.HasKey(e => e.TokenID);
                entity.Property(e => e.TokenID).HasDefaultValueSql("NEWID()"); // GUID automático
            });

            // IntegrationEvents
            modelBuilder.Entity<IntegrationEvent>(entity =>
            {
                entity.ToTable("IntegrationEvents");
                entity.HasKey(e => e.EventID);
            });

            // Vista sin key
            modelBuilder.Entity<UserValidation>().ToView("V_UserValidation").HasNoKey();
        }
    }
}