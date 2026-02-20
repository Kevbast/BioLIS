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
    }
}