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
        public Order Order { get; set; } = null!;

        [Column("TestID")]
        public int TestID { get; set; }

        [ForeignKey("TestID")]
        public LabTest LabTest { get; set; } = null!;

        [Column("ResultValue")]
        public decimal? ResultValue { get; set; }

        [Column("IsAbnormal")]
        public bool IsAbnormal { get; set; }

        [Column("Notes")]
        public string? Notes { get; set; }

        [Column("AlertLevel")]
        public string? AlertLevel { get; set; }

        [Column("EnteredBy")]
        public int? EnteredBy { get; set; }

        [Column("EnteredDate")]
        public DateTime EnteredDate { get; set; } = DateTime.Now;

        [Column("ModifiedBy")]
        public int? ModifiedBy { get; set; }

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        //-----NAVEGACIÓN (FK para auditoría)----------
        [ForeignKey("EnteredBy")]
        public User? EnteredByUser { get; set; }

        [ForeignKey("ModifiedBy")]
        public User? ModifiedByUser { get; set; }
    }

    /// <summary>
    /// Niveles de alerta para resultados
    /// </summary>
    public static class AlertLevels
    {
        public const string Normal = "NORMAL";
        public const string Anormal = "ANORMAL";
        public const string Critico = "CRITICO";
        public const string SinRango = "SIN_RANGO";

        public static List<string> GetAll() => new List<string> 
        { 
            Normal, 
            Anormal, 
            Critico, 
            SinRango 
        };

        public static string GetDisplayName(string? alertLevel)
        {
            return alertLevel switch
            {
                "NORMAL" => "Normal",
                "ANORMAL" => "Anormal",
                "CRITICO" => "Crítico",
                "SIN_RANGO" => "Sin Rango",
                _ => "Sin clasificar"
            };
        }

        public static string GetBadgeClass(string? alertLevel)
        {
            return alertLevel switch
            {
                "NORMAL" => "badge bg-success",
                "ANORMAL" => "badge bg-warning",
                "CRITICO" => "badge bg-danger",
                "SIN_RANGO" => "badge bg-secondary",
                _ => "badge bg-light"
            };
        }
    }
}