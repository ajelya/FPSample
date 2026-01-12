using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace FPSample.Models.Entities
{
    public class ServiceRequest
    {
        [Key]
        public int RequestId { get; set; }
        public int? UserId { get; set; }
        public int ServiceId { get; set; }
        public int PurposeId { get; set; }

        [Column(TypeName ="decimal(18,2)")]
        public decimal? GrossAnnualIncome { get; set; }
        public int StatusId { get; set; } = 0;
        public DateTime DateToClaim { get; set; }
        public TimeSpan TimeToClaim { get; set; }

        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Add or verify this property for the ID Photo
        public string? UploadPath { get; set; }

        // Add this to handle the file upload in the form (not saved in DB)
        [NotMapped]
        public IFormFile? ProfilePicture { get; set; }
        public virtual ICollection<History> Histories { get; set; } = new List<History>();
    }
}
