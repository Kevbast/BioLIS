using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("Patients")]
    public class Patient
    {
        [Key]
        [Column("PatientID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PatientID { get; set; }

        // --- NUEVOS CAMPOS ENTERPRISE ---
        [Column("PublicId")]
        public Guid PublicId { get; set; } // ID público seguro para futuras APIs y descargas de PDF

        [Column("FirstName")]
        public string FirstName { get; set; }

        [Column("LastName")]
        public string LastName { get; set; }

        [Column("Gender")]
        public string Gender { get; set; } // "M" o "F"

        [Column("BirthDate")]
        public DateTime BirthDate { get; set; }

        [Column("Email")]
        public string? Email { get; set; }

        [Column("PhoneNumber")]
        public string? PhoneNumber { get; set; } // Para envío de resultados por WhatsApp (n8n)

        [Column("PhotoFilename")]
        public string? PhotoFilename { get; set; }

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
        /// Órdenes del paciente
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        //Edad calculada automáticamente
        [NotMapped]
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Year;
                if (BirthDate.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        //Nombre completo
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}