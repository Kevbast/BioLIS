using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("SampleTypes")]
    public class SampleType
    {
        [Key]
        [Column("SampleID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SampleID { get; set; }

        [Column("SampleName")]
        public string SampleName { get; set; }

        [Column("ContainerColor")]
        public string ContainerColor { get; set; }

        //--------NAVEGACIÓN INVERSA (Relaciones)-----------
        // Exámenes que usan este tipo de muestra
        public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
    }
}