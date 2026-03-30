using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [Column("NotificationID")]
        public int NotificationID { get; set; }

        [Column("UserID")]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; } = null!;

        [Column("Title")]
        public string Title { get; set; } = null!;

        [Column("Message")]
        public string Message { get; set; } = null!;

        [Column("IsRead")]
        public bool IsRead { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("RelatedOrderID")]
        public int? RelatedOrderID { get; set; }
    }
}