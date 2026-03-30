using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("Doctors")]
    public class Doctor
    {
        [Key]
        [Column("DoctorID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Importante: Sin autoincremento
        public int DoctorID { get; set; }

        [Column("FullName")]
        public string FullName { get; set; }

        [Column("LicenseNumber")]
        public string? LicenseNumber { get; set; }

        [Column("Email")]
        public string? Email { get; set; }

        // --- NUEVOS CAMPOS ENTERPRISE ---
        [Column("PhoneNumber")]
        public string? PhoneNumber { get; set; } // Para alertas automáticas (n8n/WhatsApp)

        [Column("IsActive")]
        public bool IsActive { get; set; } = true; // Para borrado lógico (Soft Delete)

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("CreatedBy")]
        public int? CreatedBy { get; set; }

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [Column("UpdatedBy")]
        public int? UpdatedBy { get; set; }
        // --------------------------------

        //--------NAVEGACIÓN INVERSA (Relaciones)-----------
        //Órdenes creadas por este doctor
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}