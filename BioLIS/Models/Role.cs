using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [Column("RoleID")]
        public int RoleID { get; set; }

        [Column("RoleName")]
        public string RoleName { get; set; } = null!;
    }
}