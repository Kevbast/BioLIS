using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioLIS.Models
{
    [Table("OrderShareTokens")]
    public class OrderShareToken
    {
        [Key]
        [Column("TokenID")]
        public Guid TokenID { get; set; }

        [Column("OrderID")]
        public int OrderID { get; set; }

        [ForeignKey("OrderID")]
        public Order Order { get; set; } = null!;

        [Column("PinCode")]
        public string PinCode { get; set; } = null!;

        [Column("ExpiresAt")]
        public DateTime ExpiresAt { get; set; }

        [Column("DownloadsCount")]
        public int DownloadsCount { get; set; } = 0;

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}