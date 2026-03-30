using BioLIS.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
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
        public string? Email { get; set; }

        [Column("PhotoFilename")]
        public string? PhotoFilename { get; set; }

        [Column("PasswordText")]
        public string PasswordText { get; set; } = null!;

        // --- CAMBIO A ROLES (RBAC) ---
        [Column("RoleID")]
        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role? Role { get; set; }
        // -----------------------------

        [Column("DoctorID")]
        public int? DoctorID { get; set; }

        [ForeignKey("DoctorID")]
        public Doctor? Doctor { get; set; }

        // --- CAMPOS ENTERPRISE ---
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // -------------------------

        // Relación con Users_Security (1 a 1)
        public virtual UserSecurity? UserSecurity { get; set; }

        // Propiedades calculadas actualizadas para leer desde RoleID o RoleName
        [NotMapped]
        public bool IsAdmin => Role?.RoleName == "Admin" || RoleID == 1;

        [NotMapped]
        public bool IsDoctor => Role?.RoleName == "Doctor" || RoleID == 3;

        [NotMapped]
        public string DisplayName => Doctor?.FullName ?? Username;
    }

    /// <summary>
    /// Roles disponibles (Constantes para validaciones rápidas)
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Laboratorio = "Laboratorio";

        public static List<string> GetAll() => new List<string> { Admin, Doctor, Laboratorio };
    }
}