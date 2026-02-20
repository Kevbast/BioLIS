using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        [Column("OrderID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrderID { get; set; }

        [Column("PatientID")]
        public int PatientID { get; set; }

        [ForeignKey("PatientID")]
        public Patient Patient { get; set; }

        [Column("DoctorID")]
        public int DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public Doctor Doctor { get; set; }

        [Column("OrderDate")]
        public DateTime OrderDate { get; set; }

        [Column("OrderNumber")]
        public string OrderNumber { get; set; }

        //-----NAVEGACIÓN INVERSA (Relaciones)----------
        // Resultados de pruebas de esta orden
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}