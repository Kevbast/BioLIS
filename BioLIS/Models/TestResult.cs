using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("TestResults")]
    public class TestResult
    {
        [Key]
        [Column("ResultID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResultID { get; set; }

        [Column("OrderID")]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public Order Order { get; set; }

        [Column("TestID")]
        public int TestID { get; set; }

        [ForeignKey("TestID")]
        public LabTest LabTest { get; set; }

        [Column("ResultValue")]
        public decimal? ResultValue { get; set; }

        [Column("IsAbnormal")]
        public bool IsAbnormal { get; set; }

        [Column("Notes")]
        public string? Notes { get; set; }
    }
}