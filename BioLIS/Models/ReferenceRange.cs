using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("ReferenceRanges")]
    public class ReferenceRange
    {
        [Key]
        [Column("RangeID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RangeID { get; set; }

        [Column("TestID")]
        public int TestID { get; set; }

        [ForeignKey("TestID")]
        public LabTest LabTest { get; set; }

        [Column("Gender")]
        public string Gender { get; set; }

        [Column("MinAgeYear")]
        public int MinAgeYear { get; set; }

        [Column("MaxAgeYear")]
        public int MaxAgeYear { get; set; }

        [Column("MinVal")]
        public decimal MinVal { get; set; }

        [Column("MaxVal")]
        public decimal MaxVal { get; set; }
    }
}