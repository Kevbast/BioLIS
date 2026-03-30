using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("IntegrationEvents")]
    public class IntegrationEvent
    {
        [Key]
        [Column("EventID")]
        public int EventID { get; set; }

        [Column("EventType")]
        public string EventType { get; set; } = null!;

        [Column("PayloadJSON")]
        public string PayloadJSON { get; set; } = null!;

        [Column("IsProcessed")]
        public bool IsProcessed { get; set; } = false;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("ProcessedAt")]
        public DateTime? ProcessedAt { get; set; }
    }
}