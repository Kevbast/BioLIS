using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("LabTests")]
    public class LabTest
    {
        [Key]
        [Column("TestID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TestID { get; set; }

        [Column("TestName")]
        public string TestName { get; set; }

        [Column("Units")]
        public string Units { get; set; }

        // Relación con SampleType (Foreign Key)
        [Column("SampleID")]
        public int SampleID { get; set; }

        [ForeignKey("SampleID")]
        public SampleType SampleType { get; set; }
    }
}