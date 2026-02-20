using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models // Cambia "BioLab" por el nombre de tu proyecto
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
        public string LicenseNumber { get; set; }

        [Column("Email")]
        public string Email { get; set; }
    }
}