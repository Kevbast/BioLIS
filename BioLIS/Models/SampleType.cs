using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("SampleTypes")]
    public class SampleType
    {
        [Key]
        [Column("SampleID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SampleID { get; set; }

        [Column("SampleName")]
        public string SampleName { get; set; } = null!;

        [Column("ContainerColor")]
        public string? ContainerColor { get; set; }

        // --- CAMPOS ENTERPRISE ---
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;
        // -------------------------

        //---------NAVEGACIÓN INVERSA (Relaciones)------------
        public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
    }
}