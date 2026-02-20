using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models
{
    [Table("Patients")]
    public class Patient
    {
        [Key]
        [Column("PatientID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PatientID { get; set; }

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

        [Column("PhotoFilename")]
        public string? PhotoFilename { get; set; }


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