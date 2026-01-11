using System.ComponentModel.DataAnnotations;

namespace FPSample.Models.Entities
{
    public class History
    {
        [Key]
        public int HistoryId { get; set; }
        public int RequestId { get; set; }
        public int AdminId { get; set; }
        public int StatusId { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
