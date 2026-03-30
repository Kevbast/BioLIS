using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("LabTests")]
    public class LabTest
    {
        [Key]
        [Column("TestID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TestID { get; set; }

        [Column("TestName")]
        public string TestName { get; set; } = null!;

        [Column("Units")]
        public string? Units { get; set; }

        // Relación con SampleType (Foreign Key)
        [Column("SampleID")]
        public int SampleID { get; set; }

        [ForeignKey("SampleID")]
        public SampleType SampleType { get; set; } = null!;

        // --- CAMPOS ENTERPRISE ---
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        
        // -------------------------

        //---------NAVEGACIÓN INVERSA (Relaciones)------------
        public virtual ICollection<ReferenceRange> ReferenceRanges { get; set; } = new List<ReferenceRange>();
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}