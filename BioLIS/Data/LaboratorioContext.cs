using BioLab.Models;
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
    }
}
