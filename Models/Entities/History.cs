using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FPSample.Models.Entities
{
    public class History
    {
        [Key]
        public int HistoryId { get; set; }
        public int RequestId { get; set; }
        public int AdminId { get; set; }
        public int StatusId { get; set; }
        public DateTime UpdatedAt { get; set; }


        [ForeignKey("RequestId")]
        public virtual ServiceRequest ServiceRequest { get; set; }

        [ForeignKey("AdminId")]
        public virtual Admin Admin { get; set; }
    }
}