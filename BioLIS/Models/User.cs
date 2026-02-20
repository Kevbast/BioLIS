using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLab.Models // Cambia a BioLIS.Models si es tu namespace
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("UserID")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Sin Identity
        public int UserID { get; set; }

        [Column("Username")]
        public string Username { get; set; } = null!;

        [Column("PasswordText")]
        public string PasswordText { get; set; } = null!;

        [Column("Role")]
        public string Role { get; set; } = null!; // 'Admin', 'Doctor', 'Recepcion'

        [Column("DoctorID")]
        public int? DoctorID { get; set; } // El '?' significa que puede ser NULL (para el Admin)

        [ForeignKey("DoctorID")]
        public Doctor? Doctor { get; set; }
    }
}