using BioLab.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("Users_Security")]
    public class UserSecurity
    {
        [Key]
        [Column("UserID")]
        [ForeignKey("User")]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Salt")]
        public string Salt { get; set; } = string.Empty;

        //LA MAYOR VENTAJA ES QUE EF DIRECTAMENTE CONVIERTE
        //VARBINARY ,BLOB A BYTE[]
        [Required]
        [Column("PasswordHash")]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        // Navegación hacia User
        public virtual User User { get; set; } = null!;

    }
}
