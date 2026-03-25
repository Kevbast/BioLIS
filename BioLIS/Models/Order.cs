using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
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
        public Patient Patient { get; set; } = null!;

        [Column("DoctorID")]
        public int DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public Doctor Doctor { get; set; } = null!;

        [Column("OrderDate")]
        public DateTime OrderDate { get; set; }

        [Column("OrderNumber")]
        public string OrderNumber { get; set; } = null!;

        [Column("Status")]
        public string Status { get; set; } = "Pendiente";

        [Column("CompletedDate")]
        public DateTime? CompletedDate { get; set; }

        [Column("ApprovedBy")]
        public int? ApprovedBy { get; set; }

        [ForeignKey("ApprovedBy")]
        public User? ApprovedByUser { get; set; }

        //-----NAVEGACIÓN INVERSA (Relaciones)----------
        // Resultados de pruebas de esta orden
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }

    /// <summary>
    /// Estados posibles de una orden
    /// </summary>
    public static class OrderStatus
    {
        public const string Pendiente = "Pendiente";
        public const string EnProceso = "EnProceso";
        public const string Completada = "Completada";
        public const string Aprobada = "Aprobada";
        public const string Entregada = "Entregada";

        public static List<string> GetAll() => new List<string> 
        { 
            Pendiente, 
            EnProceso, 
            Completada, 
            Aprobada, 
            Entregada 
        };

        public static string GetDisplayName(string status)
        {
            return status switch
            {
                "Pendiente" => "Pendiente",
                "EnProceso" => "En Proceso",
                "Completada" => "Completada",
                "Aprobada" => "Aprobada",
                "Entregada" => "Entregada",
                _ => status
            };
        }
    }
}