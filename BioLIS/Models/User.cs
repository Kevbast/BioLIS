using BioLIS.Models;
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
        [Column("Email")]
        public string Email { get; set; } = null!;

        [Column("PhotoFilename")]
        public string PhotoFilename { get; set; } = null!;
        [Column("PasswordText")]
        public string PasswordText { get; set; } = null!;

        [Column("Role")]
        public string Role { get; set; } = null!; // 'Admin', 'Doctor', 'Recepcion'

        [Column("DoctorID")]
        public int? DoctorID { get; set; } // El '?' significa que puede ser NULL (para el Admin)

        [ForeignKey("DoctorID")]
        public Doctor? Doctor { get; set; }

        // Relación con Users_Security (1 a 1)
        public virtual UserSecurity? UserSecurity { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public bool IsAdmin => Role == "Admin";

        [NotMapped]
        public bool IsDoctor => Role == "Doctor";

        [NotMapped]
        public string DisplayName => Doctor?.FullName ?? Username;
    }

    /// <summary>
    /// Roles disponibles
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Recepcion = "Recepcion";

        public static List<string> GetAll() => new List<string> { Admin, Doctor, Recepcion };
    }

}
